using Mediachase.Search;
using System;
using System.Configuration.Provider;
using System.Web.Configuration;

namespace EPiServer.Reference.Commerce.Site.Features.Search
{
	/// <summary>
	/// This class provides/load the search indexing provider, exposes methods to Search, Remove, Index using the defaultProvider
	/// </summary>
	internal class OSGSearchService
	{
		private static SearchProvider _provider;

		/// <summary>
		/// Gets the provider.
		/// </summary>
		/// <value>The provider.</value>
		public static SearchProvider Provider
		{
			get
			{
				SearchProvider arg_14_0;
				if ((arg_14_0 = _provider) == null)
				{
					arg_14_0 = (_provider = InitializeConfiguredProvider());
				}
				return arg_14_0;
			}
			set
			{
				_provider = value;
			}
		}


	    private static SearchProvider InitializeConfiguredProvider()
		{
			SearchProviderElement searchProviders = SearchConfiguration.Instance.SearchProviders;
			if (searchProviders.Providers == null)
			{
				throw new ProviderException("Failed to initialize search provider section");
			}
			SearchProvider expr_43 = (SearchProvider)ProvidersHelper.InstantiateProvider(searchProviders.Providers[searchProviders.DefaultProvider], typeof(SearchProvider));
			if (expr_43 == null)
			{
				throw new ProviderException("Unable to load default SearchProvider");
			}
			return expr_43;
		}

		/// <summary>
		/// Searches the specified criteria.
		/// </summary>
		/// <param name="applicationName">Name of the application.</param>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		public static ISearchResults Search(string applicationName, ISearchCriteria criteria)
		{
			return Provider.Search(applicationName, criteria);
		}

		/// <summary>
		/// Removes the specified scope.
		/// </summary>
		/// <param name="applicationName">Name of the application.</param>
		/// <param name="scope">The scope.</param>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static int Remove(string applicationName, string scope, string key, string value)
		{
			return Provider.Remove(applicationName, scope, key, value);
		}

		/// <summary>
		/// Removes all.
		/// </summary>
		/// <param name="applicationName">Name of the application.</param>
		/// <param name="scope">The scope.</param>
		public static void RemoveAll(string applicationName, string scope)
		{
			Provider.RemoveAll(applicationName, scope);
		}

		/// <summary>
		/// Commits this instance.
		/// </summary>
		/// <param name="applicationName">Name of the application.</param>
		public static void Commit(string applicationName)
		{
			Provider.Commit(applicationName);
		}

		/// <summary>
		/// Indexes the specified scope.
		/// </summary>
		/// <param name="applicationName">Name of the application.</param>
		/// <param name="scope">The scope. We should give Indexer name as scope.</param>
		/// <param name="document">The document.</param>
		/// <remarks>applicationName and scope will be used to create Folder to store indexing files</remarks>
		public static void Index(string applicationName, string scope, ISearchDocument document)
		{
			Provider.Index(applicationName, scope, document);
		}

		/// <summary>
		/// Closes the specified scope.
		/// </summary>
		/// <param name="applicationName">Name of the application.</param>
		/// <param name="scope">The scope.</param>
		public static void Close(string applicationName, string scope)
		{
			Provider.Close(applicationName, scope);
		}
	}
}
