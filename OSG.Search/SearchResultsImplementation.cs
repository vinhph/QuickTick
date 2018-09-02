using EPiServer.Find;
using EPiServer.Find.Api;
using EPiServer.Find.Api.Facets;
using Mediachase.Search;
using Mediachase.Search.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.FindSearchProvider;

namespace OSG.Search
{
    internal class SearchResultsImplementation : SearchResults
    {
        private int _totalCount;
        private int[] _keyFieldValues;

        public override int TotalCount
        {
            get
            {
                return this._totalCount;
            }
        }

        public override T[] GetFieldValue<T>(int index, string fieldName)
        {
            try
            {
                return this.Documents[index][fieldName].Values.OfType<T>().ToArray<T>();
            }
            catch (NullReferenceException ex)
            {
                return (T[])null;
            }
        }

        public override T[] GetKeyFieldValues<T>()
        {
            return this._keyFieldValues.OfType<T>().ToArray<T>();
        }

        public SearchResultsImplementation(ISearchConfiguration config, SearchResults<FindDocument> results, ISearchCriteria criteria)
          : base((ISearchDocuments)new SearchDocumentsImplementation(config, criteria.Locale, results), criteria)
        {
            this._totalCount = results.TotalMatching;
            this.Suggestions = new string[0];
            this._keyFieldValues = results.Hits.Select<SearchHit<FindDocument>, int>((Func<SearchHit<FindDocument>, int>)(h => h.Document.CatalogEntryId)).ToArray<int>();
            this.FacetGroups = (ISearchFacetGroup[])this.BuildFacetResults(results, criteria).ToArray<FacetGroup>();
        }

        private IEnumerable<FacetGroup> BuildFacetResults(SearchResults<FindDocument> results, ISearchCriteria criteria)
        {
            List<FacetGroup> facetGroupList = new List<FacetGroup>();
            if (results.Facets != null && results.Facets.Any<EPiServer.Find.Api.Facets.Facet>())
            {
                Dictionary<string, FilterFacet> dictionary = results.Facets.OfType<FilterFacet>().ToDictionary<FilterFacet, string, FilterFacet>((Func<FilterFacet, string>)(f => f.Name), (Func<FilterFacet, FilterFacet>)(f => f), (IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
                foreach (SearchFilter filter in criteria.Filters)
                {
                    string facetDescription = this.GetFacetDescription(filter.Descriptions, criteria.Locale);
                    FacetGroup facetGroup = new FacetGroup(filter.field, facetDescription);
                    if (filter.Values.SimpleValue != null)
                    {
                        foreach (SimpleValue simpleValue in filter.Values.SimpleValue)
                            this.AddFacetResult(facetGroup, dictionary, filter.field, simpleValue.key, simpleValue.Descriptions, criteria.Locale);
                    }
                    if (filter.Values.RangeValue != null)
                    {
                        foreach (RangeValue rangeValue in filter.Values.RangeValue)
                            this.AddFacetResult(facetGroup, dictionary, filter.field, rangeValue.key, rangeValue.Descriptions, criteria.Locale);
                    }
                    if (filter.Values.PriceRangeValue != null)
                    {
                        foreach (PriceRangeValue priceRangeValue in ((IEnumerable<PriceRangeValue>)filter.Values.PriceRangeValue).Where<PriceRangeValue>((Func<PriceRangeValue, bool>)(prv => string.Equals(prv.currency, (string)criteria.Currency, StringComparison.OrdinalIgnoreCase))))
                            this.AddFacetResult(facetGroup, dictionary, filter.field, priceRangeValue.key, priceRangeValue.Descriptions, criteria.Locale);
                    }
                    if (facetGroup.Facets.Count > 0)
                        facetGroupList.Add(facetGroup);
                }
            }
            return (IEnumerable<FacetGroup>)facetGroupList;
        }

        private void AddFacetResult(FacetGroup facetGroup, Dictionary<string, FilterFacet> facetsByName, string field, string key, Descriptions descriptions, string locale)
        {
            string key1 = FindProviderExtensions.EncodeFacetName(field, key);
            FilterFacet filterFacet;
            if (!facetsByName.TryGetValue(key1, out filterFacet) || filterFacet.Count <= 0)
                return;
            string facetDescription = this.GetFacetDescription(descriptions, locale);
            Mediachase.Search.Extensions.Facet facet = new Mediachase.Search.Extensions.Facet((ISearchFacetGroup)facetGroup, key, facetDescription, filterFacet.Count);
            facetGroup.Facets.Add((ISearchFacet)facet);
        }

        private string GetFacetDescription(Descriptions descriptions, string locale)
        {
            return (((IEnumerable<Description>)descriptions.Description).SingleOrDefault<Description>((Func<Description, bool>)(d => string.Equals(d.locale, locale, StringComparison.OrdinalIgnoreCase))) ?? ((IEnumerable<Description>)descriptions.Description).SingleOrDefault<Description>((Func<Description, bool>)(d => string.Equals(d.locale, descriptions.defaultLocale, StringComparison.OrdinalIgnoreCase))))?.Value;
        }
    }
}
