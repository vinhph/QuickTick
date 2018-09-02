using EPiServer.Commerce.FindSearchProvider.Dictionary2Find;
using EPiServer.Commerce.FindSearchProvider.LocalImplementation;
using EPiServer.Find;
using EPiServer.Find.Api;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Json;
using EPiServer.ServiceLocation;
using Mediachase.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.FindSearchProvider;
using EPiServer.Find.Cms;
using Mediachase.Search.Extensions;


namespace OSG.Search
{
    /// <summary>A search provider implementation for EPiServer Find.</summary>
    public class FindSearchProvider : SearchProvider
    {
        private const int MaxOperationsPerRequest = 50;
        private object _lock;
        private Dictionary<int, FindDocument> _pendingOperations;
        private readonly EPiServer.Commerce.FindSearchProvider.FindSearchProviderConventions _findSearchProviderConventions;

        /// <summary>Gets the client.</summary>
        /// <value>The client.</value>
        public IClient Client { get; private set; }

        /// <summary>Gets the configuration.</summary>
        /// <value>The configuration.</value>
        public ISearchConfiguration Configuration
        {
            get
            {
                return (this.Client as ProviderClient)?.ProviderConfiguration;
            }
        }

        /// <summary>
        /// Gets the class type of the query builder. This class will be used to dynamically convert SearchCriteria to the query
        /// that Search Provider can understand.
        /// </summary>
        /// <value>The type of the query builder.</value>
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
        public FindSearchProvider()
          : this(ServiceLocator.Current.GetInstance<EPiServer.Commerce.FindSearchProvider.FindSearchProviderConventions>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.FindSearchProvider.FindSearchProvider" /> class.
        /// <param name="findSearchProviderConventions">The find search provide conventions.</param>
        /// </summary>
        public FindSearchProvider(EPiServer.Commerce.FindSearchProvider.FindSearchProviderConventions findSearchProviderConventions)
        {
            this._findSearchProviderConventions = findSearchProviderConventions;
            this._lock = new object();
            this._pendingOperations = new Dictionary<int, FindDocument>(50);
        }

        /// <summary>Initializes the provider.</summary>
        /// <param name="name">The friendly name of the provider.</param>
        /// <param name="config">A collection of the name/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
        /// <exception cref="T:System.ArgumentNullException">config</exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(name))
                name = typeof(EPiServer.Commerce.FindSearchProvider.FindSearchProvider).Name;
            base.Initialize(name, config);
            this.Client = this.CreateClient((ISearchConfiguration)new ProviderConfiguration(config));
        }

        /// <summary>Searches the datasource using the specified criteria.</summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public override ISearchResults Search(string applicationName, ISearchCriteria criteria)
        {
            //var res = new FindSearchQueryBuilder(
            //    Client,
            //    criteria,
            //    _findSearchProviderConventions.DefaultQueryCultureOption()).Search.GetResult<FindDocument>();

            //var res = Client.Search<VariationContent>().GetContentResult();
            //return Client.Search<FindDocument>().GetResult();
            //return (ISearchResults)new SearchResultsImplementation(
            //    Client.GetConfiguration(),
            //    res,
            //    criteria);
            return (ISearchResults)new SearchResultsImplementation(
                Client.GetConfiguration(),
                new FindSearchQueryBuilder(
                    Client,
                    criteria,
                    _findSearchProviderConventions.DefaultQueryCultureOption()).Search.GetResult<FindDocument>(),
                criteria);
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
                throw new ArgumentNullException(nameof(document));
            CatalogEntryWrapper catalogEntryWrapper = document as CatalogEntryWrapper;
            if (catalogEntryWrapper == null)
                throw new NotSupportedException("The EPiServer Find search provider requires the use of the FindSearchIndexBuilder.  Please update your search configuration.");
            ISearchConfiguration providerConfiguration = (this.Client as ProviderClient).ProviderConfiguration;
            FindDocument document1 = new FindDocumentBuilder(catalogEntryWrapper.EntryRow, providerConfiguration, catalogEntryWrapper.Languages).Document;
            this.AddOperation(document1.CatalogEntryId, document1);
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
                throw new NotSupportedException(string.Format("The EPiServer Find search provider will only remove documents by key value. Field \"{0}\" not accepted.", (object)key));
            this.AddOperation(int.Parse(value), (FindDocument)null);
            return 0;
        }

        /// <summary>Removes all documents in the specified scope.</summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="scope">The scope.</param>
        public override void RemoveAll(string applicationName, string scope)
        {
            this.TakePendingOperations();
            //this.Client.Delete<FindDocument>((Expression<Func<FindDocument, Filter>>)(x => x.CatalogEntryId.Match(0) | ~x.CatalogEntryId.Match(0)));
        }

        /// <summary>Closes the specified provider.</summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="scope">The scope.</param>
        public override void Close(string applicationName, string scope)
        {
            this.Commit(applicationName);
        }

        /// <summary>Commits changes made to this instance.</summary>
        /// <param name="applicationName">Name of the application.</param>
        public override void Commit(string applicationName)
        {
            this.SendOperations(this.TakePendingOperations());
        }

        private void AddOperation(int catalogEntryId, FindDocument document)
        {
            Dictionary<int, FindDocument> operations = (Dictionary<int, FindDocument>)null;
            lock (this._lock)
            {
                this._pendingOperations[catalogEntryId] = document;
                if (this._pendingOperations.Count >= 50)
                    operations = this.TakePendingOperations();
            }
            if (operations == null)
                return;
            this.SendOperations(operations);
        }

        private void SendOperations(Dictionary<int, FindDocument> operations)
        {
            ProviderClient client = (ProviderClient)this.Client;
            List<int> intList = new List<int>(operations.Count);
            List<FindDocument> findDocumentList = new List<FindDocument>(operations.Count);
            foreach (KeyValuePair<int, FindDocument> operation in operations)
            {
                if (operation.Value == null)
                    intList.Add(operation.Key);
                else
                    findDocumentList.Add(operation.Value);
            }
            if (intList.Count > 0)
            {
                FilterBuilder<FindDocument> filter = client.BuildFilter<FindDocument>();
                foreach (int num in intList)
                {
                    int documentId = num;
                    filter = filter.Or((Expression<Func<FindDocument, Filter>>)(doc => doc.CatalogEntryId.Match(documentId)));
                }
                client.Delete<FindDocument>((Expression<Func<FindDocument, Filter>>)(_ => (Filter)filter));
            }
            if (findDocumentList.Count <= 0)
                return;
            BulkResultItem bulkResultItem = client.Index((IEnumerable)findDocumentList).Items.FirstOrDefault<BulkResultItem>((Func<BulkResultItem, bool>)(r => !r.Ok));
            if (bulkResultItem != null)
                throw new Exception(string.Format("Indexing error: {0}", (object)bulkResultItem.Error));
        }

        private Dictionary<int, FindDocument> TakePendingOperations()
        {
            Dictionary<int, FindDocument> pendingOperations;
            lock (this._lock)
            {
                pendingOperations = this._pendingOperations;
                if (pendingOperations != null)
                    this._pendingOperations = new Dictionary<int, FindDocument>(50);
            }
            return pendingOperations;
        }

        private IClient CreateClient(ISearchConfiguration config)
        {
            ProviderClient providerClient = new ProviderClient(config);
            providerClient.Conventions.ContractResolver.ContractInterceptors.Add((IInterceptContract)new IncludeTypeNameInDictionaryKeyFieldNameConvention());
            return (IClient)providerClient;
        }
    }
}
