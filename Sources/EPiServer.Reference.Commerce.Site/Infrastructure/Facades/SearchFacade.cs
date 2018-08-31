using System;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Website.Search;
using Mediachase.Search;
using Mediachase.Search.Extensions;
using OSG.Utilities;
using StringCollection = System.Collections.Specialized.StringCollection;
using AppContext = Mediachase.Commerce.Core.AppContext;

namespace EPiServer.Reference.Commerce.Site.Infrastructure.Facades
{
    [ServiceConfiguration(typeof(SearchFacade), Lifecycle = ServiceInstanceScope.Singleton)]
    public class SearchFacade
    {
        public enum SearchProviderType
        {
            Lucene,
            FindSearch,
            OSGSearch,
            Unknown
        }

        private OFindManage _searchManager;
        private SearchProviderType _searchProviderType;
        private bool _initialized;

        public virtual ISearchResults Search(CatalogEntrySearchCriteria criteria)
        {
            Initialize();
            return _searchManager.Search(criteria);
        }

        public virtual SearchProviderType GetSearchProvider()
        {
            Initialize();
            return _searchProviderType;
        }

        public virtual SearchFilter[] SearchFilters => SearchFilterHelper.Current.SearchConfig.SearchFilters;

        public virtual StringCollection GetOutlinesForNode(string code)
        {
            return SearchFilterHelper.GetOutlinesForNode(code);
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _searchManager = new OFindManage(AppContext.Current.ApplicationName);
            _searchProviderType = LoadSearchProvider();
            _initialized = true;
        }

        private SearchProviderType LoadSearchProvider()
        {
            var element = SearchConfiguration.Instance.SearchProviders;
            if (element.Providers == null ||
                String.IsNullOrEmpty(element.DefaultProvider) ||
                String.IsNullOrEmpty(element.Providers[element.DefaultProvider].Type))
            {
                return SearchProviderType.Unknown;
            }

            var providerType = Type.GetType(element.Providers[element.DefaultProvider].Type);
            var luceneType = Type.GetType("Mediachase.Search.Providers.Lucene.LuceneSearchProvider, Mediachase.Search.LuceneSearchProvider");
            var ePiFindType = Type.GetType("EPiServer.Commerce.FindSearchProvider.FindSearchProvider, EPiServer.Commerce.FindSearchProvider");
            var osgFindType = Type.GetType("OSG.Search.OSGSearchProvider, OSG.Search");
            if (providerType == null || luceneType == null)
            {
                return SearchProviderType.Unknown;
            }
            if (ePiFindType != null && (providerType == ePiFindType || providerType.IsSubclassOf(ePiFindType)))
            {
                return SearchProviderType.FindSearch;
            }
            if (osgFindType != null && (providerType == osgFindType || providerType.IsSubclassOf(osgFindType)))
            {
                return SearchProviderType.OSGSearch;
            }
            if (providerType == luceneType || providerType.IsSubclassOf(luceneType))
            {
                return SearchProviderType.Lucene;
            }

            return SearchProviderType.Unknown;
        }
    }
}