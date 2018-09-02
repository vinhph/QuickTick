using EPiServer.Commerce.FindSearchProvider.LocalImplementation;
using EPiServer.Find;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Api.Querying.Queries;
using Mediachase.Commerce;
using Mediachase.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using EPiServer.Commerce.FindSearchProvider;

namespace OSG.Search
{
    public static class FindProviderExtensions
    {
        /// <summary>Gets a field descriptor for the named field.</summary>
        /// <param name="client">A find client created by the find search provider.</param>
        /// <param name="fieldName">The name of the desired field.</param>
        /// <param name="locale">The locale of the desired field. If a single-language field exists matching the name, locale is ignored. If locale is null, the field may not be multilanguage.</param>
        /// <returns>A field descriptor for the requested field.</returns>
        public static IFieldConfiguration GetField(this IClient client, string fieldName, string locale = null)
        {
            IFieldConfiguration field = client.GetConfiguration().GetField(fieldName, locale);
            if (field != null)
                return field;
            throw new Exception(locale != null ? string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Cannot find field named \"{0}\" for locale \"{1}\".", (object)fieldName, (object)locale) : string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Cannot find field named \"{0}\" for global locale.", (object)fieldName));
        }

        /// <summary>
        /// Gets a strongly typed field descriptor for the named field.
        /// </summary>
        /// <typeparam name="TField">The type of the desired field.</typeparam>
        /// <param name="client">A find client created by the find search provider.</param>
        /// <param name="fieldName">The name of the desired field.</param>
        /// <param name="locale">The locale of the desired field. If a single-language field exists matching the name, locale is ignored. If locale is null, the field may not be multilanguage.</param>
        /// <returns>A strongly typed field descriptor for the requested field.</returns>
        public static IFieldConfiguration<TField> GetField<TField>(this IClient client, string fieldName, string locale = null)
        {
            IFieldConfiguration field = client.GetField(fieldName, locale);
            IFieldConfiguration<TField> fieldConfiguration = field as IFieldConfiguration<TField>;
            if (fieldConfiguration != null)
                return fieldConfiguration;
            throw new Exception(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Field \"{0}\" is of type \"{1}\", not of requested type \"{2}\".", (object)fieldName, (object)field.Type.Name, (object)typeof(TField).Name));
        }

        /// <summary>
        /// Gets a strongly typed field descriptor for the named price field.
        /// </summary>
        /// <param name="client">A find client created by the find search provider.</param>
        /// <param name="fieldName">The name of the desired price field. Should be "listprice" or "saleprice".</param>
        /// <param name="currency">The currency.</param>
        /// <param name="marketId">The market id.</param>
        /// <returns>A field descriptor for the requested field.</returns>
        /// <exception cref="T:System.Exception">
        /// </exception>
        public static IFieldConfiguration<double?> GetPriceField(this IClient client, string fieldName, string currency, MarketId marketId)
        {
            IFieldConfiguration<double?> priceField = client.GetConfiguration().GetPriceField(fieldName, (Mediachase.Commerce.Currency)currency, marketId);
            if (priceField == null)
                throw new Exception(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Cannot find field named \"{0}\" for currency \"{1}\" and market \"{2}\".", (object)fieldName, (object)currency, (object)marketId.Value));
            IFieldConfiguration<double?> fieldConfiguration = priceField;
            if (fieldConfiguration != null)
                return fieldConfiguration;
            throw new Exception(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Field \"{0}\" is of type \"{1}\", not of requested type \"{2}\".", (object)fieldName, (object)priceField.Type.Name, (object)typeof(double?).Name));
        }

        /// <summary>
        /// Searches for the specified query in all default search fields, and in the specified locale.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="query">The query text to search for.</param>
        /// <param name="locale">The locale to search in.</param>
        /// <returns>The search, modified to include the requested query.</returns>
        public static ITypeSearch<FindDocument> ForDefaultFields(this ITypeSearch<FindDocument> search, string query, string locale)
        {
            if (!string.IsNullOrEmpty(query))
            {
                ISearchConfiguration configuration = search.Client.GetConfiguration();
                IQueriedSearch<FindDocument, QueryStringQuery> search1 = search.For<FindDocument>(query);
                Enumerable.Empty<IFieldConfiguration>();
                foreach (IFieldConfiguration fieldConfiguration1 in !string.IsNullOrWhiteSpace(locale) ? configuration.GetDefaultFields(locale) : configuration.GetDefaultFields())
                {
                    if (fieldConfiguration1.Type == typeof(string))
                    {
                        IFieldConfiguration<string> fieldConfiguration2 = (IFieldConfiguration<string>)fieldConfiguration1;
                        search1 = search1.InField<FindDocument, QueryStringQuery>(fieldConfiguration2.TypedGetValueExpression, new double?());
                    }
                    else if (fieldConfiguration1.Type == typeof(IEnumerable<string>))
                    {
                        IFieldConfiguration<IEnumerable<string>> fieldConfiguration2 = (IFieldConfiguration<IEnumerable<string>>)fieldConfiguration1;
                        search1 = search1.InField<FindDocument, QueryStringQuery>(fieldConfiguration2.TypedGetValueExpression, new double?());
                    }
                    else if (fieldConfiguration1.Type == typeof(int?))
                    {
                        IFieldConfiguration<int?> fieldConfiguration2 = (IFieldConfiguration<int?>)fieldConfiguration1;
                        search1 = search1.InField<FindDocument, QueryStringQuery>(fieldConfiguration2.TypedGetValueExpression, new double?());
                    }
                    else if (fieldConfiguration1.Type == typeof(int))
                    {
                        IFieldConfiguration<int> fieldConfiguration2 = (IFieldConfiguration<int>)fieldConfiguration1;
                        search1 = search1.InField<FindDocument, QueryStringQuery>(fieldConfiguration2.TypedGetValueExpression, new double?());
                    }
                    else if (fieldConfiguration1.Type == typeof(double?) || fieldConfiguration1.Type == typeof(float?))
                    {
                        IFieldConfiguration<double?> fieldConfiguration2 = (IFieldConfiguration<double?>)fieldConfiguration1;
                        search1 = search1.InField<FindDocument, QueryStringQuery>(fieldConfiguration2.TypedGetValueExpression, new double?());
                    }
                    else
                    {
                        if (!(fieldConfiguration1.Type == typeof(double)) && !(fieldConfiguration1.Type == typeof(float)))
                            throw new Exception(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Field of type {0} is not valid for the default search.", (object)fieldConfiguration1.Type.Name));
                        IFieldConfiguration<double> fieldConfiguration2 = (IFieldConfiguration<double>)fieldConfiguration1;
                        search1 = search1.InField<FindDocument, QueryStringQuery>(fieldConfiguration2.TypedGetValueExpression, new double?());
                    }
                }
                search = (ITypeSearch<FindDocument>)search1;
            }
            return search;
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the entries must be available for the entire interval from startDate to endDate.
        /// </summary>
        /// <remark>Preorder entries are included in results as default.</remark>
        /// <param name="search">The search to modify.</param>
        /// <param name="startDate">The beginning of the interval where the catalog entries must be available.</param>
        /// <param name="endDate">The end of the interval where the catalog entries must be available.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterDates(this ITypeSearch<FindDocument> search, DateTime startDate, DateTime endDate)
        {
            return search.FilterDates(startDate, endDate, true);
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the entries must be available for the entire interval from startDate to endDate, include Preorder entry or not.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="startDate">The beginning of the interval where the catalog entries must be available.</param>
        /// <param name="endDate">The end of the interval where the catalog entries must be available.</param>
        /// <param name="includePreorderEntry">The preorder catalog entries is available include preorder entries.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterDates(this ITypeSearch<FindDocument> search, DateTime startDate, DateTime endDate, bool includePreorderEntry)
        {
            search = search.Filter<FindDocument>((Expression<Func<FindDocument, Filter>>)(d => d.StartDate.Before(startDate) | d.StartDate.Match(startDate) | d.AllowPreorder.Match(includePreorderEntry) & (d.PreorderAvailableDate.Before(startDate) | d.PreorderAvailableDate.Match(startDate)))).Filter<FindDocument>((Expression<Func<FindDocument, Filter>>)(d => d.EndDate.After(endDate) | d.EndDate.Match(endDate)));
            return search;
        }

        /// <summary>
        /// Adds a filter to the specified search requiring the entry is available for the specific language
        /// </summary>
        /// <param name="search">The search to modify</param>
        /// <param name="language">the language availability for the entries</param>
        /// <returns></returns>
        public static ITypeSearch<FindDocument> FilterLanguages(this ITypeSearch<FindDocument> search, string language)
        {
            search = search.Filter<FindDocument>((Expression<Func<FindDocument, Filter>>)(d => d.Languages.MatchCaseInsensitive(language)));
            return search;
        }

        /// <summary>
        /// Adds a filter to the specified search requiring the entry is available for the specific languages
        /// </summary>
        /// <param name="search">The search to modify</param>
        /// <param name="languages">the languages availability for the entries</param>
        /// <returns>The search query</returns>
        public static ITypeSearch<FindDocument> FilterLanguages(this ITypeSearch<FindDocument> search, IEnumerable<string> languages)
        {
            FilterBuilder<FindDocument> filterBuilder = new FilterBuilder<FindDocument>(search.Client);
            foreach (string language1 in languages)
            {
                string language = language1;
                filterBuilder = filterBuilder.Or((Expression<Func<FindDocument, Filter>>)(d => d.Languages.MatchCaseInsensitive(language)));
            }
            return search.Filter<FindDocument>((Filter)filterBuilder);
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the entries must be in at least one of the specified catalogs.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="catalogNames">The catalog names to filter on. If the enumeration is empty, no filter is applied.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterCatalogs(this ITypeSearch<FindDocument> search, IEnumerable<string> catalogNames)
        {
            FilterBuilder<FindDocument> filterBuilder = search.Client.BuildFilter<FindDocument>();
            bool flag = false;
            foreach (string catalogName1 in catalogNames)
            {
                string catalogName = catalogName1;
                flag = true;
                filterBuilder = filterBuilder.Or((Expression<Func<FindDocument, Filter>>)(x => x.Catalogs.MatchCaseInsensitive(catalogName)));
            }
            if (flag)
                search = search.Filter<FindDocument>((Filter)filterBuilder);
            return search;
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the entries must under one of the specified catalog nodes.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="catalogNodes">The catalog nodes to filter on. If the enumeration is empty, no filter is applied.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterCatalogNodes(this ITypeSearch<FindDocument> search, IEnumerable<string> catalogNodes)
        {
            FilterBuilder<FindDocument> filterBuilder = search.Client.BuildFilter<FindDocument>();
            bool flag = false;
            foreach (string catalogNode in catalogNodes)
            {
                string catalogNodeCode = catalogNode;
                flag = true;
                filterBuilder = filterBuilder.Or((Expression<Func<FindDocument, Filter>>)(x => x.CatalogNodes.MatchCaseInsensitive(catalogNodeCode)));
            }
            if (flag)
                search = search.Filter<FindDocument>((Filter)filterBuilder);
            return search;
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the entries must match one of the specified outlines.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="outlines">The outlines to filter on. If the enumeration is empty, no filter is applied.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterOutlines(this ITypeSearch<FindDocument> search, IEnumerable<string> outlines)
        {
            FilterBuilder<FindDocument> filterBuilder = search.Client.BuildFilter<FindDocument>();
            bool flag = false;
            foreach (string outline1 in outlines)
            {
                string outline = outline1;
                flag = true;
                filterBuilder = filterBuilder.Or((Expression<Func<FindDocument, Filter>>)(x => x.Outlines.MatchCaseInsensitive(outline)));
            }
            if (flag)
                search = search.Filter<FindDocument>((Filter)filterBuilder);
            return search;
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the entries be of one of the specified meta classes.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="metaClassNames">The metaclasses to filter on. If the enumeration is empty, no filter is applied.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterMetaClasses(this ITypeSearch<FindDocument> search, IEnumerable<string> metaClassNames)
        {
            FilterBuilder<FindDocument> filterBuilder = search.Client.BuildFilter<FindDocument>();
            bool flag = false;
            foreach (string metaClassName1 in metaClassNames)
            {
                string metaClassName = metaClassName1;
                flag = true;
                filterBuilder = filterBuilder.Or((Expression<Func<FindDocument, Filter>>)(x => x.MetaClassName.MatchCaseInsensitive(metaClassName)));
            }
            if (flag)
                search = search.Filter<FindDocument>((Filter)filterBuilder);
            return search;
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the entries be of one of the specified entry types (such as product, variation, etc).
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="entryTypes">The entry types to filter on. If the enumeration is empty, no filter is applied.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterCatalogEntryTypes(this ITypeSearch<FindDocument> search, IEnumerable<string> entryTypes)
        {
            FilterBuilder<FindDocument> filterBuilder = search.Client.BuildFilter<FindDocument>();
            bool flag = false;
            foreach (string entryType1 in entryTypes)
            {
                string entryType = entryType1;
                flag = true;
                filterBuilder = filterBuilder.Or((Expression<Func<FindDocument, Filter>>)(x => x.CatalogEntryType.MatchCaseInsensitive(entryType)));
            }
            if (flag)
                search = search.Filter<FindDocument>((Filter)filterBuilder);
            return search;
        }

        /// <summary>Filters inactive catalog entries.</summary>
        /// <param name="search">The search to modify.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterInactiveCatalogEntries(this ITypeSearch<FindDocument> search)
        {
            FilterBuilder<FindDocument> filterBuilder = search.Client.BuildFilter<FindDocument>();
            filterBuilder.And((Expression<Func<FindDocument, Filter>>)(x => x.IsActive.Match(true)));
            search = search.Filter<FindDocument>((Filter)filterBuilder);
            return search;
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the entries be valid in the specified market.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="marketId">The market id to require entries are valid for.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterCatalogEntryMarket(this ITypeSearch<FindDocument> search, MarketId marketId)
        {
            IFieldConfiguration<IEnumerable<string>> field = search.Client.GetField<IEnumerable<string>>("_ExcludedCatalogEntryMarkets", (string)null);
            string value = marketId.Value;
            Expression<Func<FindDocument, Filter>> filterExpression = EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.ComposeFilter<IEnumerable<string>>(field.TypedGetValueExpression, (Expression<Func<IEnumerable<string>, Filter>>)(x => x.Match(value)));
            return search.Filter<FindDocument>(filterExpression);
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the specified field is in the specified range.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="fieldName">The name of the field to filter on.</param>
        /// <param name="locale">The locale of the field to filter on. If the field is not multilanguage, locale is ignored. If locale is null, the field may not be multilanguage.</param>
        /// <param name="from">The value for the lower end of the range, represented as a string in the invariant culture.</param>
        /// <param name="to">The value for the upper end of the range, represented as a string in the invariant culture.</param>
        /// <param name="includeFrom">If true, the lower endpoint is included; otherwise, it is excluded.</param>
        /// <param name="includeTo">If true, the upper endpoint is included; otherwise, it is excluded.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterFieldRange(this ITypeSearch<FindDocument> search, string fieldName, string locale, string from, string to, bool includeFrom, bool includeTo)
        {
            IFieldConfiguration field = search.Client.GetField(fieldName, locale);
            return search.FilterFieldRange(field, from, to, includeFrom, includeTo);
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the specified field is in the specified range.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="fieldName">The name of the field to filter on.</param>
        /// <param name="locale">The locale of the field to filter on. If the field is not multilanguage, locale is ignored. If locale is null, the field may not be multilanguage.</param>
        /// <param name="from">The value for the lower end of the range, represented as a string in the invariant culture.</param>
        /// <param name="to">The value for the upper end of the range, represented as a string in the invariant culture.</param>
        /// <param name="includeFrom">If true, the lower endpoint is included; otherwise, it is excluded.</param>
        /// <param name="includeTo">If true, the upper endpoint is included; otherwise, it is excluded.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterFieldRange<TField>(this ITypeSearch<FindDocument> search, string fieldName, string locale, string from, string to, bool includeFrom, bool includeTo)
        {
            IFieldConfiguration<TField> field = search.Client.GetField<TField>(fieldName, locale);
            return search.FilterFieldRange((IFieldConfiguration)field, from, to, includeFrom, includeTo);
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the specified field is in the specified range.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="fieldName">The name of the field to filter on.</param>
        /// <param name="from">The value for the lower end of the range, represented as a string in the invariant culture.</param>
        /// <param name="to">The value for the upper end of the range, represented as a string in the invariant culture.</param>
        /// <param name="includeFrom">If true, the lower endpoint is included; otherwise, it is excluded.</param>
        /// <param name="includeTo">If true, the upper endpoint is included; otherwise, it is excluded.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterFieldRange(this ITypeSearch<FindDocument> search, string fieldName, string from, string to, bool includeFrom, bool includeTo)
        {
            return search.FilterFieldRange(fieldName, (string)null, from, to, includeFrom, includeTo);
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the specified price field is in the specified range.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="fieldName">The name of the field to filter on.  This should be "listprice" or "saleprice".</param>
        /// <param name="currency">The currency of the price to filter on.</param>
        /// <param name="marketId">The market id.</param>
        /// <param name="from">The value for the lower end of the range.</param>
        /// <param name="to">The value for the upper end of the range.</param>
        /// <param name="includeFrom">If true, the lower endpoint is included; otherwise, it is excluded.</param>
        /// <param name="includeTo">If true, the upper endpoint is included; otherwise, it is excluded.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterPriceFieldRange(this ITypeSearch<FindDocument> search, string fieldName, string currency, MarketId marketId, double? from, double? to, bool includeFrom, bool includeTo)
        {
            IFieldConfiguration<double?> priceField = search.Client.GetPriceField(fieldName, currency, marketId);
            return search.FilterFieldRange((IFieldConfiguration)priceField, from.HasValue ? from.Value.ToString((IFormatProvider)CultureInfo.InvariantCulture) : string.Empty, to.HasValue ? to.Value.ToString((IFormatProvider)CultureInfo.InvariantCulture) : string.Empty, includeFrom, includeTo);
        }

        /// <summary>
        /// Adds a filter to the specified search requiring that the specified field is in the specified range.
        /// </summary>
        /// <param name="search">The search to modify.</param>
        /// <param name="field">The field to filter on.</param>
        /// <param name="from">The value for the lower end of the range, represented as a string in the invariant culture.</param>
        /// <param name="to">The value for the upper end of the range, represented as a string in the invariant culture.</param>
        /// <param name="includeFrom">If true, the lower endpoint is included; otherwise, it is excluded.</param>
        /// <param name="includeTo">If true, the upper endpoint is included; otherwise, it is excluded.</param>
        /// <returns>The search, modified to include the requested filter.</returns>
        public static ITypeSearch<FindDocument> FilterFieldRange(this ITypeSearch<FindDocument> search, IFieldConfiguration field, string from, string to, bool includeFrom, bool includeTo)
        {
            Expression<Func<FindDocument, Filter>> rangeFilter = UntypedFilterBuilder.GetRangeFilter(field, from, to, includeFrom, includeTo);
            return search.Filter<FindDocument>(rangeFilter);
        }

        /// <summary>
        /// Adds the active filters from an ISearchCriteria object to an existing search.
        /// </summary>
        public static ITypeSearch<FindDocument> AddActiveFilters(this ITypeSearch<FindDocument> search, ISearchCriteria criteria)
        {
            string[] activeFilterFields = criteria.ActiveFilterFields;
            ISearchFilterValue[] activeFilterValues = criteria.ActiveFilterValues;
            if (activeFilterFields.Length != activeFilterValues.Length)
                throw new InvalidOperationException("Active filter fields and values collections must have the same length.");
            for (int index = 0; index < activeFilterFields.Length; ++index)
                search = search.AddActiveFilter(activeFilterFields[index], activeFilterValues[index], criteria.Locale, criteria.MarketId);
            return search;
        }

        /// <summary>
        /// Adds a single active filter from an ISearchCriteria object to an existing search.
        /// </summary>
        public static ITypeSearch<FindDocument> AddActiveFilter(this ITypeSearch<FindDocument> search, string fieldName, ISearchFilterValue filterValue, string locale, MarketId marketId)
        {
            ISearchConfiguration configuration = search.Client.GetConfiguration();
            SimpleValue simpleValue;
            if ((simpleValue = filterValue as SimpleValue) != null)
            {
                IFieldConfiguration field;
                if (configuration.TryGetField(fieldName, locale, out field))
                {
                    Expression<Func<FindDocument, Filter>> matchFilter = UntypedFilterBuilder.GetMatchFilter(field, simpleValue.value);
                    search = search.Filter<FindDocument>(matchFilter);
                }
            }
            else
            {
                RangeValue rangeValue;
                if ((rangeValue = filterValue as RangeValue) != null)
                {
                    IFieldConfiguration field;
                    if (configuration.TryGetField(fieldName, locale, out field))
                        search = search.FilterFieldRange(field, rangeValue.lowerbound, rangeValue.upperbound, rangeValue.lowerboundincluded, rangeValue.upperboundincluded);
                }
                else
                {
                    PriceRangeValue priceRangeValue;
                    if ((priceRangeValue = filterValue as PriceRangeValue) == null)
                        throw new NotSupportedException(string.Format("Unrecognized search filter value type {0}.", (object)filterValue.GetType().Name));
                    IFieldConfiguration<double?> field;
                    if (configuration.TryGetPriceField(fieldName, new Mediachase.Commerce.Currency(priceRangeValue.currency.ToUpperInvariant()), marketId, out field))
                        search = search.FilterFieldRange((IFieldConfiguration)field, priceRangeValue.lowerbound, priceRangeValue.upperbound, priceRangeValue.lowerboundincluded, priceRangeValue.upperboundincluded);
                }
            }
            return search;
        }

        /// <summary>
        /// Adds the faceting criteria from an ISearchCriteria object to an existing search.
        /// </summary>
        public static ITypeSearch<FindDocument> AddFacets(this ITypeSearch<FindDocument> search, ISearchCriteria criteria)
        {
            foreach (KeyValuePair<string, Expression<Func<FindDocument, Filter>>> facet in search.GetFacets(criteria))
                search = search.FilterFacet<FindDocument>(facet.Key, facet.Value);
            return search;
        }

        internal static IEnumerable<KeyValuePair<string, Expression<Func<FindDocument, Filter>>>> GetFacets(this ITypeSearch<FindDocument> search, ISearchCriteria criteria)
        {
            SearchFilter[] searchFilterArray = criteria.Filters;
            for (int index = 0; index < searchFilterArray.Length; ++index)
            {
                SearchFilter filter = searchFilterArray[index];
                foreach (KeyValuePair<string, Expression<Func<FindDocument, Filter>>> facet in search.GetFacets(filter, criteria.Locale))
                    yield return facet;
                foreach (KeyValuePair<string, Expression<Func<FindDocument, Filter>>> priceFacet in search.GetPriceFacets(filter, criteria.Currency, criteria.MarketId))
                    yield return priceFacet;
                filter = (SearchFilter)null;
            }
            searchFilterArray = (SearchFilter[])null;
        }

        /// <summary>
        /// Adds faceting requests to the search for the specified filter and locale.
        /// </summary>
        public static ITypeSearch<FindDocument> AddFacet(this ITypeSearch<FindDocument> search, SearchFilter filter, string locale)
        {
            foreach (KeyValuePair<string, Expression<Func<FindDocument, Filter>>> facet in search.GetFacets(filter, locale))
                search = search.FilterFacet<FindDocument>(facet.Key, facet.Value);
            return search;
        }

        internal static IEnumerable<KeyValuePair<string, Expression<Func<FindDocument, Filter>>>> GetFacets(this ITypeSearch<FindDocument> search, SearchFilter filter, string locale)
        {
            ISearchConfiguration config = search.Client.GetConfiguration();
            IFieldConfiguration field;
            int index;
            if (FindProviderExtensions.IsSimpleFacet(filter) && config.TryGetField(filter.field, locale, out field))
            {
                SimpleValue[] simpleValueArray = filter.Values.SimpleValue;
                for (index = 0; index < simpleValueArray.Length; ++index)
                {
                    SimpleValue simpleValue = simpleValueArray[index];
                    yield return new KeyValuePair<string, Expression<Func<FindDocument, Filter>>>(FindProviderExtensions.EncodeFacetName(field.Name, simpleValue.key), UntypedFilterBuilder.GetMatchFilter(field, simpleValue.value));
                }
                simpleValueArray = (SimpleValue[])null;
            }
            if (FindProviderExtensions.IsRangeFacet(filter) && config.TryGetField(filter.field, locale, out field))
            {
                RangeValue[] rangeValueArray = filter.Values.RangeValue;
                for (index = 0; index < rangeValueArray.Length; ++index)
                {
                    RangeValue rangeValue = rangeValueArray[index];
                    yield return new KeyValuePair<string, Expression<Func<FindDocument, Filter>>>(FindProviderExtensions.EncodeFacetName(field.Name, rangeValue.key), UntypedFilterBuilder.GetRangeFilter(field, rangeValue.lowerbound, rangeValue.upperbound, rangeValue.lowerboundincluded, rangeValue.upperboundincluded));
                }
                rangeValueArray = (RangeValue[])null;
            }
        }

        /// <summary>
        /// Adds faceting requests to the search for the specified pricing.
        /// </summary>
        public static ITypeSearch<FindDocument> AddPriceFacet(this ITypeSearch<FindDocument> search, SearchFilter filter, Mediachase.Commerce.Currency currency, MarketId marketId)
        {
            foreach (KeyValuePair<string, Expression<Func<FindDocument, Filter>>> priceFacet in search.GetPriceFacets(filter, currency, marketId))
                search = search.FilterFacet<FindDocument>(priceFacet.Key, priceFacet.Value);
            return search;
        }

        internal static IEnumerable<KeyValuePair<string, Expression<Func<FindDocument, Filter>>>> GetPriceFacets(this ITypeSearch<FindDocument> search, SearchFilter filter, Mediachase.Commerce.Currency currency, MarketId marketId)
        {
            IFieldConfiguration<double?> field;
            if (FindProviderExtensions.IsPriceRangeFacet(filter) && search.Client.GetConfiguration().TryGetPriceField(filter.field, currency, marketId, out field))
            {
                foreach (PriceRangeValue priceRangeValue in ((IEnumerable<PriceRangeValue>)filter.Values.PriceRangeValue).Where<PriceRangeValue>((Func<PriceRangeValue, bool>)(prv => string.Equals(prv.currency, (string)currency, StringComparison.OrdinalIgnoreCase))))
                    yield return new KeyValuePair<string, Expression<Func<FindDocument, Filter>>>(FindProviderExtensions.EncodeFacetName(field.Name, priceRangeValue.key), UntypedFilterBuilder.GetRangeFilter((IFieldConfiguration)field, priceRangeValue.lowerbound, priceRangeValue.upperbound, priceRangeValue.lowerboundincluded, priceRangeValue.upperboundincluded));
            }
        }

        /// <summary>
        /// Adds the ordering requests from an ISearchCriteria object to an existing search.
        /// </summary>
        public static ITypeSearch<FindDocument> OrderBy(this ITypeSearch<FindDocument> search, ISearchCriteria criteria)
        {
            ISearchConfiguration configuration = search.Client.GetConfiguration();
            SearchSortField[] searchSortFieldArray = criteria.Sort == null ? new SearchSortField[0] : criteria.Sort.GetSort();
            bool flag = true;
            foreach (SearchSortField searchSortField in searchSortFieldArray)
            {
                IFieldConfiguration field1;
                if (configuration.TryGetAnyField(searchSortField.FieldName, criteria.Locale, criteria.Currency, criteria.MarketId, out field1) && field1 != null)
                {
                    if (field1.Type == typeof(string))
                    {
                        IFieldConfiguration<string> field2 = (IFieldConfiguration<string>)field1;
                        search = flag ? search.OrderByField<string>(field2, searchSortField.IsDescending) : search.ThenByField<string>(field2, searchSortField.IsDescending);
                    }
                    else if (field1.Type == typeof(bool?))
                    {
                        IFieldConfiguration<bool?> field2 = (IFieldConfiguration<bool?>)field1;
                        search = flag ? search.OrderByField<bool?>(field2, searchSortField.IsDescending) : search.ThenByField<bool?>(field2, searchSortField.IsDescending);
                    }
                    else if (field1.Type == typeof(int?))
                    {
                        IFieldConfiguration<int?> field2 = (IFieldConfiguration<int?>)field1;
                        search = flag ? search.OrderByField<int?>(field2, searchSortField.IsDescending) : search.ThenByField<int?>(field2, searchSortField.IsDescending);
                    }
                    else if (field1.Type == typeof(int))
                    {
                        IFieldConfiguration<int> field2 = (IFieldConfiguration<int>)field1;
                        search = flag ? search.OrderByField<int>(field2, searchSortField.IsDescending) : search.ThenByField<int>(field2, searchSortField.IsDescending);
                    }
                    else if (field1.Type == typeof(long?))
                    {
                        IFieldConfiguration<long?> field2 = (IFieldConfiguration<long?>)field1;
                        search = flag ? search.OrderByField<long?>(field2, searchSortField.IsDescending) : search.ThenByField<long?>(field2, searchSortField.IsDescending);
                    }
                    else if (field1.Type == typeof(double?))
                    {
                        IFieldConfiguration<double?> field2 = (IFieldConfiguration<double?>)field1;
                        search = flag ? search.OrderByField<double?>(field2, searchSortField.IsDescending) : search.ThenByField<double?>(field2, searchSortField.IsDescending);
                    }
                    else if (field1.Type == typeof(DateTime?))
                    {
                        IFieldConfiguration<DateTime?> field2 = (IFieldConfiguration<DateTime?>)field1;
                        search = flag ? search.OrderByField<DateTime?>(field2, searchSortField.IsDescending) : search.ThenByField<DateTime?>(field2, searchSortField.IsDescending);
                    }
                    else
                    {
                        if (!(field1.Type == typeof(DateTime)))
                            throw new InvalidOperationException(string.Format("Cannot sort on a field of type {0}.", (object)field1.Type.Name));
                        IFieldConfiguration<DateTime> field2 = (IFieldConfiguration<DateTime>)field1;
                        search = flag ? search.OrderByField<DateTime>(field2, searchSortField.IsDescending) : search.ThenByField<DateTime>(field2, searchSortField.IsDescending);
                    }
                    flag = false;
                }
            }
            return search;
        }

        /// <summary>
        /// Adds an ascending ordering request for the specified field to an existing search.
        /// </summary>
        public static ITypeSearch<FindDocument> OrderByField<TField>(this ITypeSearch<FindDocument> search, IFieldConfiguration<TField> field, bool isDescending = false)
        {
            if (!isDescending)
                return search.OrderBy<FindDocument, TField>(field.TypedGetValueExpression);
            return search.OrderByDescending<FindDocument, TField>(field.TypedGetValueExpression);
        }

        /// <summary>
        /// Adds a descending ordering request for the specified field to an existing search.
        /// </summary>
        public static ITypeSearch<FindDocument> OrderByFieldDescending<TField>(this ITypeSearch<FindDocument> search, IFieldConfiguration<TField> field)
        {
            return search.OrderByField<TField>(field, true);
        }

        /// <summary>
        /// Adds a code-ordered ascending ordering request for the specified field to an existing search.
        /// </summary>
        public static ITypeSearch<FindDocument> ThenByField<TField>(this ITypeSearch<FindDocument> search, IFieldConfiguration<TField> field, bool isDescending = false)
        {
            if (!isDescending)
                return search.ThenBy<FindDocument, TField>(field.TypedGetValueExpression);
            return search.ThenByDescending<FindDocument, TField>(field.TypedGetValueExpression);
        }

        /// <summary>
        /// Adds a code-ordered descending ordering request for the specified field to an existing search.
        /// </summary>
        public static ITypeSearch<FindDocument> ThenByFieldDescending<TField>(this ITypeSearch<FindDocument> search, IFieldConfiguration<TField> field)
        {
            return search.ThenByField<TField>(field, true);
        }

        /// <summary>Gets the configuration.</summary>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        /// <exception cref="T:System.NotSupportedException">Find Provider for Commerce extensions will only work on searches against the provider-created client.</exception>
        public static ISearchConfiguration GetConfiguration(this IClient client)
        {
            ProviderClient providerClient = client as ProviderClient;
            if (providerClient == null)
                throw new NotSupportedException("Find Provider for Commerce extensions will only work on searches against the provider-created client.");
            return providerClient.ProviderConfiguration;
        }

        internal static string EncodeFacetName(string fieldName, string facetKey)
        {
            return string.Format("{0} {1}", (object)fieldName, (object)facetKey);
        }

        private static bool IsSimpleFacet(SearchFilter filter)
        {
            if (filter.Values.SimpleValue != null)
                return (uint)filter.Values.SimpleValue.Length > 0U;
            return false;
        }

        private static bool IsRangeFacet(SearchFilter filter)
        {
            if (filter.Values.RangeValue != null)
                return (uint)filter.Values.RangeValue.Length > 0U;
            return false;
        }

        private static bool IsPriceRangeFacet(SearchFilter filter)
        {
            if (filter.Values.PriceRangeValue != null)
                return (uint)filter.Values.PriceRangeValue.Length > 0U;
            return false;
        }
    }
}
