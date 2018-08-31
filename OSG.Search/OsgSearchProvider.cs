using EPiServer.Commerce.FindSearchProvider.Dictionary2Find;
using EPiServer.Commerce.FindSearchProvider.LocalImplementation;
using EPiServer.Find;
using EPiServer.Find.Api;
using EPiServer.Find.Api.Querying;
using EPiServer.ServiceLocation;
using Mediachase.Search;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using EPiServer.Commerce.FindSearchProvider;

namespace OSG.Search
{
    public class OsgSearchProvider : SearchProvider
    {
        [CompilerGenerated]
        [Serializable]
        private sealed class <>c
		{
			public static readonly FindSearchProvider.<>c<>9 = new FindSearchProvider.<>c();

        public static Func<BulkResultItem, bool> <>9__22_0;

		internal bool <SendOperations>b__22_0(BulkResultItem r)
        {
            return !r.Ok;
        }
    }

    private const int MaxOperationsPerRequest = 50;

    private object _lock;

    private Dictionary<int, FindDocument> _pendingOperations;

    private readonly FindSearchProviderConventions _findSearchProviderConventions;

    /// <summary>
    /// Gets the client.
    /// </summary>
    /// <value>
    /// The client.
    /// </value>
    public IClient Client
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    /// <value>
    /// The configuration.
    /// </value>
    public ISearchConfiguration Configuration
    {
        get
        {
            ProviderClient providerClient = this.Client as ProviderClient;
            if (providerClient != null)
            {
                return providerClient.ProviderConfiguration;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets the class type of the query builder. This class will be used to dynamically convert SearchCriteria to the query
    /// that Search Provider can understand.
    /// </summary>
    /// <value>
    /// The type of the query builder.
    /// </value>
    /// <example>
    /// // the following type will build query for the SOLR server
    /// "Mediachase.Search.Providers.Solr.SolrSearchQueryBuilder, Mediachase.Search.SolrSearchProvider"
    /// </example>
    public override string QueryBuilderType
    {
        get
        {
            return typeof(FindSearchQueryBuilder).AssemblyQualifiedName;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.FindSearchProvider.FindSearchProvider" /> class.
    /// </summary>
    [Obsolete("Use the constructor with QueryLanguageOption instead. Will remain at least until April 2017.")]
    public FindSearchProvider() : this(ServiceLocator.Current.GetInstance<FindSearchProviderConventions>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.FindSearchProvider.FindSearchProvider" /> class.
    /// <param name="findSearchProviderConventions">The find search provide conventions.</param>
    /// </summary>
    public FindSearchProvider(FindSearchProviderConventions findSearchProviderConventions)
    {
        this._findSearchProviderConventions = findSearchProviderConventions;
        this._lock = new object();
        this._pendingOperations = new Dictionary<int, FindDocument>(50);
    }

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="name">The friendly name of the provider.</param>
    /// <param name="config">A collection of the name/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
    /// <exception cref="T:System.ArgumentNullException">config</exception>
    public override void Initialize(string name, NameValueCollection config)
    {
        if (config == null)
        {
            throw new ArgumentNullException("config");
        }
        if (string.IsNullOrEmpty(name))
        {
            name = typeof(FindSearchProvider).Name;
        }
        base.Initialize(name, config);
        ProviderConfiguration config2 = new ProviderConfiguration(config);
        this.Client = this.CreateClient(config2);
    }

    /// <summary>
    /// Searches the datasource using the specified criteria.
    /// </summary>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="criteria">The criteria.</param>
    /// <returns></returns>
    public override ISearchResults Search(string applicationName, ISearchCriteria criteria)
    {
        SearchResults<FindDocument> result = new FindSearchQueryBuilder(this.Client, criteria, this._findSearchProviderConventions.DefaultQueryCultureOption()).Search.GetResult<FindDocument>();
        return new SearchResultsImplementation(this.Client.GetConfiguration(), result, criteria);
    }

    /// <summary>
    /// Adds the document to the index. Depending on the provider, the document will be commited only after commit is called.
    /// </summary>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="document">The document.</param>
    /// <exception cref="T:System.ArgumentNullException">document</exception>
    /// <exception cref="T:System.NotSupportedException">The EPiServer Find search provider requires the use of the FindSearchIndexBuilder.  Please update your search configuration.</exception>
    public override void Index(string applicationName, string scope, ISearchDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
        CatalogEntryWrapper catalogEntryWrapper = document as CatalogEntryWrapper;
        if (catalogEntryWrapper == null)
        {
            throw new NotSupportedException("The EPiServer Find search provider requires the use of the FindSearchIndexBuilder.  Please update your search configuration.");
        }
        ISearchConfiguration providerConfiguration = (this.Client as ProviderClient).ProviderConfiguration;
        FindDocument document2 = new FindDocumentBuilder(catalogEntryWrapper.EntryRow, providerConfiguration, catalogEntryWrapper.Languages).Document;
        this.AddOperation(document2.CatalogEntryId, document2);
    }

    /// <summary>
    /// Removes the document by specifying scope (core in SOLR), key (a field that can be used to lookup for a document) and
    /// value of the key.
    /// </summary>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    /// <exception cref="T:System.NotSupportedException"></exception>
    public override int Remove(string applicationName, string scope, string key, string value)
    {
        if (key != "_id")
        {
            throw new NotSupportedException(string.Format("The EPiServer Find search provider will only remove documents by key value. Field \"{0}\" not accepted.", key));
        }
        this.AddOperation(int.Parse(value), null);
        return 0;
    }

    /// <summary>
    /// Removes all documents in the specified scope.
    /// </summary>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="scope">The scope.</param>
    public override void RemoveAll(string applicationName, string scope)
    {
        this.TakePendingOperations();
        this.Client.Delete((FindDocument x) => x.CatalogEntryId.Match(0) | !x.CatalogEntryId.Match(0));
    }

    /// <summary>
    /// Closes the specified provider.
    /// </summary>
    /// <param name="applicationName">Name of the application.</param>
    /// <param name="scope">The scope.</param>
    public override void Close(string applicationName, string scope)
    {
        this.Commit(applicationName);
    }

    /// <summary>
    /// Commits changes made to this instance.
    /// </summary>
    /// <param name="applicationName">Name of the application.</param>
    public override void Commit(string applicationName)
    {
        Dictionary<int, FindDocument> operations = this.TakePendingOperations();
        this.SendOperations(operations);
    }

    private void AddOperation(int catalogEntryId, FindDocument document)
    {
        Dictionary<int, FindDocument> dictionary = null;
        object @lock = this._lock;
        lock (@lock)
        {
            this._pendingOperations[catalogEntryId] = document;
            if (this._pendingOperations.Count >= 50)
            {
                dictionary = this.TakePendingOperations();
            }
        }
        if (dictionary != null)
        {
            this.SendOperations(dictionary);
        }
    }

    private void SendOperations(Dictionary<int, FindDocument> operations)
    {
        ProviderClient providerClient = (ProviderClient)this.Client;
        List<int> list = new List<int>(operations.Count);
        List<FindDocument> list2 = new List<FindDocument>(operations.Count);
        foreach (KeyValuePair<int, FindDocument> current in operations)
        {
            if (current.Value == null)
            {
                list.Add(current.Key);
            }
            else
            {
                list2.Add(current.Value);
            }
        }
        if (list.Count > 0)
        {
            FilterBuilder<FindDocument> filter = providerClient.BuildFilter<FindDocument>();
            using (List<int>.Enumerator enumerator2 = list.GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    int documentId = enumerator2.Current;
                    filter = filter.Or((FindDocument doc) => doc.CatalogEntryId.Match(documentId));
                }
            }
            providerClient.Delete((FindDocument _) => (Filter)filter);
        }
        if (list2.Count > 0)
        {
            IEnumerable<BulkResultItem> arg_206_0 = providerClient.Index(list2).Items;
            Func<BulkResultItem, bool> arg_206_1;
            if ((arg_206_1 = FindSearchProvider.<> c.<> 9__22_0) == null)
            {
                arg_206_1 = (FindSearchProvider.<> c.<> 9__22_0 = new Func<BulkResultItem, bool>(FindSearchProvider.<> c.<> 9.< SendOperations > b__22_0));
            }
            BulkResultItem bulkResultItem = arg_206_0.FirstOrDefault(arg_206_1);
            if (bulkResultItem != null)
            {
                throw new Exception(string.Format("Indexing error: {0}", bulkResultItem.Error));
            }
        }
    }

    private Dictionary<int, FindDocument> TakePendingOperations()
    {
        object @lock = this._lock;
        Dictionary<int, FindDocument> pendingOperations;
        lock (@lock)
        {
            pendingOperations = this._pendingOperations;
            if (pendingOperations != null)
            {
                this._pendingOperations = new Dictionary<int, FindDocument>(50);
            }
        }
        return pendingOperations;
    }

    private IClient CreateClient(ISearchConfiguration config)
    {
        return new ProviderClient(config)
        {
            Conventions =
                {
                    ContractResolver =
                    {
                        ContractInterceptors =
                        {
                            new IncludeTypeNameInDictionaryKeyFieldNameConvention()
                        }
                    }
                }
        };
    }
}
}
