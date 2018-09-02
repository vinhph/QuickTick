using EPiServer.Commerce.FindSearchProvider.LocalImplementation;
using EPiServer.Find;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Search;
using Mediachase.Search.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using EPiServer.Commerce.FindSearchProvider;

namespace OSG.Search
{
    public class FindSearchQueryBuilder : ISearchQueryBuilder
    {
        private static readonly Dictionary<string, Language> _providerLanguages = Language.GetAll().ToDictionary<Language, string>((Func<Language, string>)(l => l.FieldSuffix), (IEqualityComparer<string>)FindSearchQueryBuilder.LanguageComparer.Instance);

        /// <summary>Gets the search.</summary>
        /// <value>The search.</value>
        public ISearch<FindDocument> Search { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.FindSearchProvider.FindSearchQueryBuilder" /> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="criteria">The criteria.</param>
        /// <exception cref="T:System.InvalidOperationException">The Find search query builder only works with clients created by the provider.</exception>
        [Obsolete("Use the constructor with QueryLanguageOption instead. Will remain at least until April 2017.")]
        public FindSearchQueryBuilder(IClient client, ISearchCriteria criteria)
          : this(client, criteria, ServiceLocator.Current.GetInstance<FindSearchProviderConventions>().DefaultQueryCultureOption())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.FindSearchProvider.FindSearchQueryBuilder" /> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="languageOption">The language option.</param>
        /// <exception cref="T:System.InvalidOperationException">The Find search query builder only works with clients created by the provider.</exception>
        public FindSearchQueryBuilder(IClient client, ISearchCriteria criteria, QueryCultureOption languageOption)
        {
            ProviderClient providerClient = client as ProviderClient;
            if (providerClient == null)
                throw new InvalidOperationException("The Find search query builder only works with clients created by the provider.");
            ISearchConfiguration providerConfiguration = providerClient.ProviderConfiguration;
            Language language = (Language)null;
            ITypeSearch<FindDocument> search1 = criteria.IgnoreFilterOnLanguage || !FindSearchQueryBuilder._providerLanguages.TryGetValue(criteria.Locale, out language) ? providerClient.Search<FindDocument>() : providerClient.Search<FindDocument>(language);
            CatalogEntrySearchCriteria criteria1 = criteria as CatalogEntrySearchCriteria;
            if (criteria1 != null)
            {
                ITypeSearch<FindDocument> search2;
                if (criteria.IgnoreFilterOnLanguage)
                {
                    search2 = search1.ForDefaultFields(criteria1.SearchPhrase, string.Empty);
                }
                else
                {
                    ITypeSearch<FindDocument> search3 = search1.ForDefaultFields(criteria1.SearchPhrase, criteria.Locale);
                    HashSet<string> source = new HashSet<string>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
                    if (language != null && !string.IsNullOrEmpty(language.FieldSuffix) && languageOption.HasFlag((Enum)QueryCultureOption.Neutral))
                        source.Add(language.FieldSuffix);
                    if (!string.IsNullOrEmpty(criteria.Locale) && languageOption.HasFlag((Enum)QueryCultureOption.Specific))
                        source.Add(criteria.Locale);
                    if (!source.Any<string>())
                        source.Add(ContentLanguage.PreferredCulture.Name);
                    search2 = search3.FilterLanguages((IEnumerable<string>)source);
                }
                if (DateTime.MinValue < criteria1.StartDate && criteria1.EndDate < DateTime.MaxValue)
                    search2 = search2.FilterDates(criteria1.StartDate, criteria1.EndDate, criteria1.IncludePreorderEntry);
                search1 = search2.FilterCatalogs(this.ToStringEnumerable(criteria1.CatalogNames)).FilterCatalogNodes(this.GetNodes(criteria1)).FilterOutlines(this.GetOutlines(criteria1)).FilterMetaClasses(this.ToStringEnumerable(criteria1.SearchIndex)).FilterCatalogEntryTypes(this.ToStringEnumerable(criteria1.ClassTypes));
                if (!criteria1.IncludeInactive)
                    search1 = search1.FilterInactiveCatalogEntries();
                if (criteria1.MarketId != MarketId.Empty)
                    search1 = search1.FilterCatalogEntryMarket(criteria1.MarketId);
            }
            ITypeSearch<FindDocument> search4 = search1.AddActiveFilters(criteria).AddFacets(criteria).OrderBy(criteria);
            if (criteria.StartingRecord > 0)
                search4 = search4.Skip<FindDocument>(criteria.StartingRecord);
            if (criteria.RecordsToRetrieve > 0)
                search4 = search4.Take<FindDocument>(criteria.RecordsToRetrieve);
            this.Search = (ISearch<FindDocument>)search4;
        }

        /// <summary>Builds the query fromt he provided search criteria.</summary>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        /// <exception cref="T:System.NotSupportedException">Please provide search query description to the constructor.</exception>
        public object BuildQuery(ISearchCriteria criteria)
        {
            throw new NotSupportedException("Please provide search query description to the constructor.");
        }

        private IEnumerable<string> ToStringEnumerable(StringCollection collection)
        {
            if (collection == null)
                return Enumerable.Empty<string>();
            return collection.Cast<string>().Where<string>((Func<string, bool>)(s =>
            {
                if (!string.IsNullOrEmpty(s))
                    return s.Trim().Length > 0;
                return false;
            }));
        }

        private IEnumerable<string> GetOutlines(CatalogEntrySearchCriteria criteria)
        {
            return this.ToStringEnumerable(criteria.Outlines).Where<string>((Func<string, bool>)(s => !s.EndsWith("*")));
        }

        private IEnumerable<string> GetNodes(CatalogEntrySearchCriteria criteria)
        {
            HashSet<string> stringSet = new HashSet<string>(this.ToStringEnumerable(criteria.CatalogNodes), (IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
            foreach (string outline in criteria.Outlines)
            {
                if (outline.EndsWith("*"))
                {
                    int startIndex = outline.LastIndexOf('/') + 1;
                    string str = outline.Substring(startIndex, outline.Length - 1 - startIndex);
                    stringSet.Add(str);
                }
            }
            return (IEnumerable<string>)stringSet;
        }

        private class LanguageComparer : IEqualityComparer<string>
        {
            public static readonly FindSearchQueryBuilder.LanguageComparer Instance = new FindSearchQueryBuilder.LanguageComparer();

            private string ReduceToLanguage(string locale)
            {
                int length = locale.IndexOf('-');
                if (length >= 0)
                    return locale.Substring(0, length);
                return locale;
            }

            public bool Equals(string x, string y)
            {
                return string.Equals(this.ReduceToLanguage(x), this.ReduceToLanguage(y), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return this.ReduceToLanguage(obj).ToLowerInvariant().GetHashCode();
            }
        }
    }
}
