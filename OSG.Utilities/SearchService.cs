using System.Configuration.Provider;
using System.Web.Configuration;
using Mediachase.Search;

namespace OSG.Utilities
{
    internal class SearchService
    {
        private static SearchProvider _provider;

        /// <summary>Gets the provider.</summary>
        /// <value>The provider.</value>
        public static SearchProvider Provider
        {
            get
            {
                return _provider ?? (_provider = InitializeConfiguredProvider());
            }
            set
            {
                _provider = value;
            }
        }

        private static SearchProvider InitializeConfiguredProvider()
        {
            SearchProviderElement searchProviders = SearchConfiguration.Instance.SearchProviders;
            SearchProvider searchProvider = (SearchProvider)ProvidersHelper.InstantiateProvider(searchProviders.Providers[searchProviders.DefaultProvider], typeof(SearchProvider));
            if (searchProvider != null)
                return searchProvider;
            throw new ProviderException("Unable to load default SearchProvider");
        }

        /// <summary>Searches the specified criteria.</summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public static ISearchResults Search(string applicationName, ISearchCriteria criteria)
        {
            ISearchResults results = Provider.Search(applicationName, new Mediachase.Search.Extensions.CatalogEntrySearchCriteria());
            return results;
        }

        /// <summary>Removes the specified scope.</summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static int Remove(string applicationName, string scope, string key, string value)
        {
            return Provider.Remove(applicationName, scope, key, value);
        }

        /// <summary>Removes all.</summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="scope">The scope.</param>
        public static void RemoveAll(string applicationName, string scope)
        {
            Provider.RemoveAll(applicationName, scope);
        }

        /// <summary>Commits this instance.</summary>
        /// <param name="applicationName">Name of the application.</param>
        public static void Commit(string applicationName)
        {
            Provider.Commit(applicationName);
        }

        /// <summary>Indexes the specified scope.</summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="scope">The scope. We should give Indexer name as scope.</param>
        /// <param name="document">The document.</param>
        /// <remarks>applicationName and scope will be used to create Folder to store indexing files</remarks>
        public static void Index(string applicationName, string scope, ISearchDocument document)
        {
            Provider.Index(applicationName, scope, document);
        }

        /// <summary>Closes the specified scope.</summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="scope">The scope.</param>
        public static void Close(string applicationName, string scope)
        {
            Provider.Close(applicationName, scope);
        }
    }
}
