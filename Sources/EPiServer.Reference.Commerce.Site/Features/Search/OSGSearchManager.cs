using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;

namespace EPiServer.Reference.Commerce.Site.Features.Search
{
	public class OSGSearchManager
	{
		[CompilerGenerated]
		[Serializable]
		private sealed class OSGSearchManagerSealed
        {
			public static readonly OSGSearchManagerSealed _oSGSearchManagerSealed = new OSGSearchManagerSealed();			
		}

		[NonSerialized]
		private readonly ILogger _logger;

		private readonly string _applicationName;

		[method: CompilerGenerated]
		[CompilerGenerated]
		public event SearchMessageHandler SearchMessage;

		[method: CompilerGenerated]
		[CompilerGenerated]
		public event SearchIndexHandler SearchIndexMessage;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Mediachase.Search.SearchManager" /> class.
		/// </summary>
		/// <param name="applicationName">The name of the application associated with this search manager.</param>
		public OSGSearchManager(string applicationName)
		{
			this._logger = LogManager.GetLogger(base.GetType());
			this._applicationName = applicationName;
			HttpContext arg_23_0 = HttpContext.Current;
		}

		/// <summary>
		/// Searches for the specified criteria.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		public ISearchResults Search(ISearchCriteria criteria)
		{
			if (SearchConfiguration.Instance.Indexers.Count == 0)
			{
				string message = string.Format("No Search Indexers defined in the configuration file.", Array.Empty<object>());
				this._logger.Error(message);
				throw new IndexNotFoundException(message);
			}
			ISearchResults result;
			try
			{
				result = OSGSearchService.Search(this._applicationName, criteria);
			}
			catch (Exception exception)
			{
				this._logger.Error("Search failed.", exception);
				throw;
			}
			return result;
		}

		protected virtual void OnSearchMessage(object source, SearchEventArgs args)
		{
			if (this.SearchMessage != null)
			{
				this.SearchMessage(source, args);
			}
		}

		protected virtual void OnSearchIndexMessage(object source, SearchIndexEventArgs args)
		{
			this._logger.Debug(string.Format("\"{0}\" - {1}%.", args.Message, Convert.ToInt32(args.CompletedPercentage).ToString()));
			if (this.SearchIndexMessage != null)
			{
				this.SearchIndexMessage(source, args);
			}
		}	

		private double ClampPercent(double value)
		{
			if (value < 0.0)
			{
				return 0.0;
			}
			if (value >= 100.0)
			{
				return 100.0;
			}
			return value;
		}

		/// <summary>
		/// Gets a path to store build index files in.
		/// </summary>
		/// <param name="basePath">The configured base path for index files.</param>
		/// <param name="indexerName">The configured name of the current indexer.</param>
		private string GetApplicationPath(string basePath, string indexerName)
		{
			return string.Concat(new string[]
			{
				SearchCommon.GetSearchPath(basePath),
				"\\",
				this._applicationName,
				"\\",
				indexerName
			});
		}
	}
}
