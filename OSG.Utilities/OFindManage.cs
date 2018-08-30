using System;
using Mediachase.Search;

namespace OSG.Utilities
{
    public class OFindManage
    {
        private readonly string _applicationName;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Mediachase.Search.SearchManager" /> class.
        /// </summary>
        /// <param name="applicationName">The name of the application associated with this search manager.</param>
        public OFindManage(string applicationName)
        {
            _applicationName = applicationName;
        }

        /// <summary>Searches for the specified criteria.</summary>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public ISearchResults Search(ISearchCriteria criteria)
        {
            try
            {
                return SearchService.Search(_applicationName, criteria);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /*
        /// <summary>
        /// Updates the index with either an incremental or full build.
        /// </summary>
        /// <param name="rebuild">If true, rebuilds the entire index; otherwise, incrementally updates the index.</param>
        public void BuildIndex(bool rebuild)
        {
            foreach (ISearchIndexBuilder searchIndexBuilder in this.CreateSearchIndexBuilders())
            {
                try
                {
                    if (rebuild)
                        this._logger.Information(string.Format("Starting new index build using \"{0}\" indexer.", (object)searchIndexBuilder.Indexer.IndexerName));
                    else
                        this._logger.Information(string.Format("Starting incremental index build using \"{0}\" indexer.", (object)searchIndexBuilder.Indexer.IndexerName));
                    searchIndexBuilder.BuildIndex(rebuild);
                    searchIndexBuilder.Indexer.SaveBuild(Status.Completed, searchIndexBuilder.Indexer.BuildDate);
                    this._logger.Information(string.Format("Successfully finished index build using \"{0}\" indexer.", (object)searchIndexBuilder.Indexer.IndexerName));
                }
                catch (Exception ex)
                {
                    IndexBuildException indexBuildException = new IndexBuildException(string.Format("Build Failed using \"{0}\" indexer. \"{1}\"", (object)searchIndexBuilder.Indexer.IndexerName, (object)ex.Message), ex);
                    this._logger.Information(indexBuildException.Message, (Exception)indexBuildException);
                    throw indexBuildException;
                }
            }
        }

        /// <summary>Updates the index for the specified items.</summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        public bool UpdateIndex(IEnumerable<int> itemIds)
        {
            bool flag1 = true;
            foreach (ISearchIndexBuilder searchIndexBuilder in this.CreateSearchIndexBuilders())
            {
                bool flag2 = searchIndexBuilder.UpdateIndex(itemIds);
                flag1 &= flag2;
            }
            return flag1;
        }

        protected virtual void OnSearchMessage(object source, SearchEventArgs args)
        {
            // ISSUE: reference to a compiler-generated field
            if (this.SearchMessage == null)
                return;
            // ISSUE: reference to a compiler-generated field
            this.SearchMessage(source, args);
        }

        protected virtual void OnSearchIndexMessage(object source, SearchIndexEventArgs args)
        {
            this._logger.Debug(string.Format("\"{0}\" - {1}%.", (object)args.Message, (object)Convert.ToInt32(args.CompletedPercentage).ToString()));
            // ISSUE: reference to a compiler-generated field
            if (this.SearchIndexMessage == null)
                return;
            // ISSUE: reference to a compiler-generated field
            this.SearchIndexMessage(source, args);
        }

        /// <summary>
        /// Returns an array containing the configured index builders.
        /// </summary>
        /// <remarks>
        /// The <see cref="T:Mediachase.Search.IndexBuilder" /> class does not actually build indexes.  It exposes some search functionality,
        /// and manages the index status file.  These classes should not be used directly.
        /// </remarks>
        public IndexBuilder[] GetIndexBuilders()
        {
            return this.CreateSearchIndexBuilders().Select<ISearchIndexBuilder, IndexBuilder>((Func<ISearchIndexBuilder, IndexBuilder>)(ib => ib.Indexer)).ToArray<IndexBuilder>();
        }

        private IEnumerable<ISearchIndexBuilder> CreateSearchIndexBuilders()
        {
            // ISSUE: reference to a compiler-generated field
            int num = this.\u003C\u003E1__state;
            SearchManager searchManager = this;
            List<IndexerDefinition> indexerDefinitions;
            string basePath;
            int i;
            switch (num)
            {
                case 0:
                    // ISSUE: reference to a compiler-generated field
                    this.\u003C\u003E1__state = -1;
                    indexerDefinitions = SearchConfiguration.Instance.Indexers.Cast<IndexerDefinition>().ToList<IndexerDefinition>();
                    basePath = SearchConfiguration.Instance.Indexers.BasePath;
                    double completionPercentScale = 1.0 / (double)indexerDefinitions.Count;
                    i = 0;
                    break;
                case 1:
                    // ISSUE: reference to a compiler-generated field
                    this.\u003C\u003E1__state = -1;
                    ++i;
                    break;
                default:
                    return false;
            }
            if (i >= indexerDefinitions.Count)
                return false;
            IndexerDefinition indexerDefinition = indexerDefinitions[i];
            searchManager._logger.Debug(string.Format("Getting the type \"{0}\".", (object)indexerDefinition.ClassName));
            Type type = Type.GetType(indexerDefinition.ClassName);
            searchManager._logger.Debug(string.Format("Creating instance of \"{0}\".", (object)type.Name));
            ISearchIndexBuilder instance1 = (ISearchIndexBuilder)Activator.CreateInstance(type);
            instance1.Manager = searchManager;
            double completionPercentBase = (double)i / completionPercentScale;
            instance1.SearchIndexMessage += (SearchIndexHandler)((s, e) => this.OnSearchIndexMessage(s, new SearchIndexEventArgs(e.Message, completionPercentBase + completionPercentScale * this.ClampPercent(e.CompletedPercentage))));
            string applicationPath = searchManager.GetApplicationPath(basePath, indexerDefinition.Name);
            IndexBuilder instance2 = ServiceLocator.Current.GetInstance<IndexBuilder>();
            instance2.ApplicationName = searchManager._applicationName;
            instance2.DirectoryPath = applicationPath;
            instance2.IndexerName = indexerDefinition.Name;
            instance1.Indexer = instance2;
            // ISSUE: reference to a compiler-generated field
            this.\u003C\u003E2__current = instance1;
            // ISSUE: reference to a compiler-generated field
            this.\u003C\u003E1__state = 1;
            return true;
        }

        private double ClampPercent(double value)
        {
            if (value < 0.0)
                return 0.0;
            if (value >= 100.0)
                return 100.0;
            return value;
        }

        /// <summary>Gets a path to store build index files in.</summary>
        /// <param name="basePath">The configured base path for index files.</param>
        /// <param name="indexerName">The configured name of the current indexer.</param>
        private string GetApplicationPath(string basePath, string indexerName)
        {
            return SearchCommon.GetSearchPath(basePath) + "\\" + this._applicationName + "\\" + indexerName;
        }
        */
    }
}
