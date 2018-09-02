using EPiServer.Find.Api;
using EPiServer.Find.Api.Ids;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.ClientConventions;
using EPiServer.Find.Connection;
using EPiServer.Find.Helpers;
using EPiServer.Find.Helpers.Text;
using EPiServer.Find.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using EPiServer.Find;

namespace OSG.Search
{
    public class Client : IClient
    {
        private ICommands commands;
        private Settings settings;

        public IClientConventions Conventions { get; set; }

        public string ServiceUrl
        {
            get
            {
                if (this.commands == null)
                    return string.Empty;
                return this.commands.ServerUrl;
            }
        }

        public string DefaultIndex { get; private set; }

        internal Dictionary<Type, object> Clients { get; private set; }

        public Client(string serviceUrl, string defaultIndex)
          : this(serviceUrl, defaultIndex, new int?())
        {
        }

        public Client(string serviceUrl, string defaultIndex, int? defaultRequestTimeout)
          : this(serviceUrl, defaultIndex, false, defaultRequestTimeout)
        {
        }

        internal Client(string serviceUrl, string defaultIndex, bool bypassLicenseRestriction, int? defaultRequestTimeout)
          : this((ICommands)new Commands(serviceUrl, (IJsonRequestFactory)new JsonRequestFactory(defaultRequestTimeout), new Func<JsonSerializer>(Serializer.CreateDefault), (ICache)new RuntimeCacheAdapter()), defaultIndex, bypassLicenseRestriction)
        {
        }

        public Client(ICommands commands, string defaultIndex)
          : this(commands, defaultIndex, false)
        {
        }

        internal Client(ICommands commands, string defaultIndex, bool bypassLicenseRestriction)
        {
            commands.ValidateNotNullArgument(nameof(commands));
            commands.ServerUrl.ValidateNotNullArgument("commands.ServerUrl");
            defaultIndex.ValidateNotNullArgument(nameof(defaultIndex));
            this.commands = commands;
            commands.Cache.Add("UnifiedWeightCache.Dependency", new StaticCachePolicy(), (object)Guid.NewGuid());
            this.Conventions = (IClientConventions)new DefaultConventions((IClient)this);
            this.DefaultIndex = defaultIndex;
            this.Clients = new Dictionary<Type, object>();
        }

        public static IClient CreateFromConfig()
        {
            return Client.CreateFromConfig(false);
        }

        internal static IClient CreateFromConfig(bool bypassLicenseRestriction)
        {
            EPiServer.Find.Configuration configuration = EPiServer.Find.Configuration.GetConfiguration();
            if (configuration.ServiceUrl.IsNullOrWhiteSpace())
                throw new ConfigurationErrorsException("The serviceUrl cannot be empty");
            if (configuration.DefaultIndex.IsNullOrWhiteSpace())
                throw new ConfigurationErrorsException("The defaultIndex cannot be empty");
            if (!configuration.ServiceUrl.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) && !configuration.ServiceUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                throw new ConfigurationErrorsException("The serviceUrl must start with http:// or https://.");
            return (IClient)new Client((ICommands)new Commands(configuration.ServiceUrl, (IJsonRequestFactory)new JsonRequestFactory(configuration.DefaultRequestTimeout), new Func<JsonSerializer>(Serializer.CreateDefault), (ICache)new RuntimeCacheAdapter()), configuration.DefaultIndex, bypassLicenseRestriction);
        }

        public Action<JsonSerializer> CustomizeSerializer
        {
            get
            {
                return new Action<JsonSerializer>(this.PrepareSerializerUsingConventions);
            }
        }

        public BulkResult Index(IEnumerable objectsToIndex, bool deleteLanguageRoutingDuplicatesOnIndex)
        {
            objectsToIndex.ValidateNotNullOrEmptyArgument(nameof(objectsToIndex));
            List<BulkAction> bulkActionList = new List<BulkAction>();
            foreach (object obj in objectsToIndex)
            {
                string id = this.Conventions.IdConvention.GetId(obj);
                if (deleteLanguageRoutingDuplicatesOnIndex)
                {
                    BulkDeleteAction bulkDeleteAction1 = new BulkDeleteAction(this.DefaultIndex, this.GetTypeName(obj), id);
                    bulkDeleteAction1.ActionAndMeta.LanguageRouting = (LanguageRouting)null;
                    BulkDeleteAction bulkDeleteAction2 = bulkDeleteAction1;
                    bulkActionList.Add((BulkAction)bulkDeleteAction2);
                }
                BulkIndexAction bulkIndexAction = new BulkIndexAction((IndexName)this.DefaultIndex, (TypeName)this.GetTypeName(obj), (DocumentId)id, obj);
                bulkIndexAction.ActionAndMeta.TimeToLive = this.Conventions.TimeToLiveConvention.GetTimeToLive(obj);
                if (this.Conventions.LanguageRoutingConvention.HasLanguageRouting(obj))
                {
                    LanguageRouting languageRouting = this.Conventions.LanguageRoutingConvention.GetLanguageRouting(obj);
                    if (languageRouting == null)
                        throw new ArgumentException(string.Format("Language missing for the object with id: {0}.", (object)id));
                    bulkIndexAction.ActionAndMeta.LanguageRouting = this.GetSupportedLanguageRoutingOrDefault(languageRouting);
                }
                bulkActionList.Add((BulkAction)bulkIndexAction);
            }
            BulkCommand bulkCommand = this.commands.Bulk((IEnumerable<BulkAction>)bulkActionList);
            this.PrepareSerializerUsingConventions(bulkCommand.CommandContext.Serializer);
            return bulkCommand.Execute();
        }

        public BulkResult Index(IEnumerable objectsToIndex)
        {
            return this.Index(objectsToIndex, false);
        }

        public IndexResult Index(object objectToIndex)
        {
            return this.Index(objectToIndex, (Action<IndexCommand>)null);
        }

        public IndexResult Index(object objectToIndex, Action<IndexCommand> commandAction)
        {
            objectToIndex.ValidateNotNullArgument(nameof(objectToIndex));
            IndexCommand indexCommand = this.commands.Index(this.DefaultIndex, this.GetTypeName(objectToIndex), objectToIndex);
            indexCommand.Id = (DocumentId)this.Conventions.IdConvention.GetId(objectToIndex);
            indexCommand.TimeToLive = this.Conventions.TimeToLiveConvention.GetTimeToLive(objectToIndex);
            if (this.Conventions.LanguageRoutingConvention.HasLanguageRouting(objectToIndex))
            {
                LanguageRouting languageRouting = this.Conventions.LanguageRoutingConvention.GetLanguageRouting(objectToIndex);
                if (languageRouting == null)
                    throw new ArgumentException(string.Format("Language missing for the object with id: {0}.", (object)indexCommand.Id));
                indexCommand.LanguageRouting = this.GetSupportedLanguageRoutingOrDefault(languageRouting);
            }
            if (commandAction.IsNotNull())
                commandAction(indexCommand);
            this.PrepareSerializerUsingConventions(indexCommand.CommandContext.Serializer);
            return indexCommand.Execute();
        }

        private string GetTypeName(object objectToIndex)
        {
            return this.GetTypeName(objectToIndex.GetType());
        }

        private string GetTypeName(Type type)
        {
            return this.Conventions.TypeNameConvention.GetTypeName(type);
        }

        private string GetTypeName<T>()
        {
            return this.GetTypeName(typeof(T));
        }

        public TSource Get<TSource>(DocumentId id)
        {
            return this.Get<TSource>(id, (LanguageRouting)null, (Action<GetCommand<TSource>>)null);
        }

        public TSource Get<TSource>(DocumentId id, LanguageRouting languageRouting)
        {
            return this.Get<TSource>(id, languageRouting, (Action<GetCommand<TSource>>)null);
        }

        public TSource Get<TSource>(DocumentId id, Action<GetCommand<TSource>> commandAction)
        {
            return this.GetWithMeta<TSource>(id, commandAction).Source;
        }

        public TSource Get<TSource>(DocumentId id, LanguageRouting languageRouting, Action<GetCommand<TSource>> commandAction)
        {
            return this.GetWithMeta<TSource>(id, commandAction).Source;
        }

        public GetResult<TSource> GetWithMeta<TSource>(DocumentId id, Action<GetCommand<TSource>> commandAction)
        {
            return this.GetWithMeta<TSource>(id, (LanguageRouting)null, commandAction);
        }

        public GetResult<TSource> GetWithMeta<TSource>(DocumentId id, LanguageRouting languageRouting, Action<GetCommand<TSource>> commandAction)
        {
            GetCommand<TSource> getCommand = this.commands.Get<TSource>(this.DefaultIndex, this.GetTypeName<TSource>(), (string)id);
            if (languageRouting != null)
                getCommand.LanguageRouting = this.GetSupportedLanguageRoutingOrDefault(languageRouting);
            this.PrepareSerializerUsingConventions(getCommand.CommandContext.Serializer);
            if (!typeof(TSource).IsAssignableFrom(typeof(TSource)))
                getCommand.Fields = (IList<string>)this.GetFields<TSource>(getCommand.CommandContext);
            if (commandAction.IsNotNull())
                commandAction(getCommand);
            return getCommand.Execute();
        }

        private List<string> GetFields<TResult>(ICommandContext commandContext)
        {
            return this.GetFields(typeof(TResult), commandContext);
        }

        private List<string> GetFields(Type type, ICommandContext commandContext)
        {
            List<string> stringList = (List<string>)null;
            JsonObjectContract jsonObjectContract = commandContext.Serializer.ContractResolver.ResolveContract(type) as JsonObjectContract;
            if (jsonObjectContract.IsNotNull())
                stringList = jsonObjectContract.Properties.Select<JsonProperty, string>((Func<JsonProperty, string>)(x => x.PropertyName)).ToList<string>();
            return stringList;
        }

        public DeleteResult Delete<T>(DocumentId id)
        {
            return this.Delete<T>(id, (Action<DeleteCommand>)null);
        }

        public DeleteResult Delete<T>(DocumentId id, LanguageRouting languageRouting)
        {
            return this.Delete<T>(id, languageRouting, (Action<DeleteCommand>)null);
        }

        public DeleteResult Delete<T>(DocumentId id, Action<DeleteCommand> commandAction)
        {
            return this.Delete(typeof(T), id, commandAction);
        }

        public DeleteResult Delete<T>(DocumentId id, LanguageRouting languageRouting, Action<DeleteCommand> commandAction)
        {
            return this.Delete(typeof(T), id, languageRouting, commandAction);
        }

        public DeleteResult Delete(Type type, DocumentId id, Action<DeleteCommand> commandAction)
        {
            return this.Delete(type, id, (LanguageRouting)null, commandAction);
        }

        public DeleteResult Delete(Type type, DocumentId id, LanguageRouting languageRouting, Action<DeleteCommand> commandAction)
        {
            DeleteCommand deleteCommand = this.commands.Delete(this.DefaultIndex, this.GetTypeName(type), (string)id);
            if (languageRouting.IsNotNull() && languageRouting.IsValid())
                deleteCommand.LanguageRouting = this.GetSupportedLanguageRoutingOrDefault(languageRouting);
            if (commandAction.IsNotNull())
                commandAction(deleteCommand);
            return deleteCommand.Execute();
        }

        public DeleteByQueryResult DeleteByQuery(IQuery query, Action<DeleteByQueryCommand> commandAction)
        {
            DeleteByQueryCommand deleteByQueryCommand = this.commands.DeleteByQuery();
            deleteByQueryCommand.Query = query;
            this.PrepareSerializerUsingConventions(deleteByQueryCommand.CommandContext.Serializer);
            deleteByQueryCommand.Indexes.Add((IndexName)this.DefaultIndex);
            if (commandAction.IsNotNull())
                commandAction(deleteByQueryCommand);
            return deleteByQueryCommand.Execute();
        }

        public ITypeUpdate<TSource> Update<TSource>(DocumentId id)
        {
            //return (ITypeUpdate<TSource>)new Update<TSource>(this, (Action<IUpdateContext>)(context => context.SourceTypes.Add(typeof(TSource))), id);
            return null;
        }

        public IndexResult Update<TSource>(DocumentId id, UpdateRequestBody requestBody)
        {
            return this.Update<TSource>(id, requestBody, (LanguageRouting)null, (Action<UpdateCommand>)null);
        }

        public IndexResult Update<TSource>(DocumentId id, UpdateRequestBody requestBody, LanguageRouting languageRouting)
        {
            return this.Update<TSource>(id, requestBody, languageRouting, (Action<UpdateCommand>)null);
        }

        public IndexResult Update<TSource>(DocumentId id, UpdateRequestBody requestBody, Action<UpdateCommand> commandAction)
        {
            return this.Update<TSource>(id, requestBody, (LanguageRouting)null, commandAction);
        }

        public IndexResult Update<TSource>(DocumentId id, UpdateRequestBody requestBody, LanguageRouting languageRouting, Action<UpdateCommand> commandAction)
        {
            UpdateCommand<TSource> updateCommand = this.commands.Update<TSource>((IndexName)this.DefaultIndex, (TypeName)this.GetTypeName<TSource>(), id);
            if (languageRouting.IsNotNull() && languageRouting.IsValid())
                updateCommand.LanguageRouting = this.GetSupportedLanguageRoutingOrDefault(languageRouting);
            updateCommand.Body = requestBody.Clone();
            this.PrepareSerializerUsingConventions(updateCommand.CommandContext.Serializer);
            return updateCommand.Execute();
        }

        public ITypeSearch<TSource> Search<TSource>()
        {
            return (ITypeSearch<TSource>)new Search<TSource, IQuery>((IClient)this, (Action<ISearchContext>)(context =>
            {
                context.SourceTypes.Add(typeof(TSource));
                context.CommandAction = (Action<SearchCommand>)(command => this.Conventions.SearchTypeFilter((ITypeFilterable)command, (IEnumerable<Type>)context.SourceTypes));
            }));
        }

        public ITypeSearch<TSource> Search<TSource>(Language language)
        {
            return (ITypeSearch<TSource>)new Search<TSource, IQuery>((IClient)this, (Action<ISearchContext>)(context =>
            {
                context.SourceTypes.Add(typeof(TSource));
                context.Analyzer = language;
                if (!context.HasContentLanguage)
                    context.ContentLanguage = language;
                context.CommandAction = (Action<SearchCommand>)(command => this.Conventions.SearchTypeFilter((ITypeFilterable)command, (IEnumerable<Type>)context.SourceTypes));
            }));
        }

        public SearchResults<TResult> Search<TResult>(SearchRequestBody requestBody, Action<SearchCommand> commandAction)
        {
            SearchCommand<TResult> searchCommand = this.commands.Search<TResult>();
            searchCommand.Body = requestBody.Clone();
            this.PrepareSerializerUsingConventions(searchCommand.CommandContext.Serializer);
            searchCommand.Indexes.Add((IndexName)this.DefaultIndex);
            if (commandAction.IsNotNull())
                commandAction((SearchCommand)searchCommand);
            return new SearchResults<TResult>(searchCommand.Execute());
        }

        public SearchResults<TResult> SearchForType<TResult>(SearchRequestBody requestBody, Action<SearchCommand> commandAction)
        {
            SearchCommand<TResult> searchCommand = this.commands.Search<TResult>();
            searchCommand.Body = requestBody.Clone();
            this.Conventions.SearchTypeFilter((ITypeFilterable)searchCommand, (IEnumerable<Type>)new Type[1]
            {
        typeof (TResult)
            });
            this.PrepareSerializerUsingConventions(searchCommand.CommandContext.Serializer);
            searchCommand.Indexes.Add((IndexName)this.DefaultIndex);
            if (commandAction.IsNotNull())
                commandAction((SearchCommand)searchCommand);
            return new SearchResults<TResult>(searchCommand.Execute());
        }

        private void PrepareSerializerUsingConventions(JsonSerializer serializer)
        {
            serializer.ContractResolver = (IContractResolver)this.Conventions.ContractResolver;
            Action<JsonSerializer> customizeSerializer = this.Conventions.CustomizeSerializer;
            if (!customizeSerializer.IsNotNull())
                return;
            customizeSerializer(serializer);
        }

        public IEnumerable<GetResult<TSource>> Get<TSource>(IEnumerable<DocumentId> ids)
        {
            Type type = typeof(TSource);
            //if (DefaultLanguageRoutingConvention.TypeHasLanguageRouting(type))
            //    throw new ArgumentException(string.Format("The type {0} must not have LanguageRouting attribute when invoking Get without languagerouting.", (object)type.FullName));
            if (this.Conventions.LanguageRoutingConvention.HasLanguageRouting(Activator.CreateInstance(type)))
                throw new ArgumentException(string.Format("The type {0} must not have LanguageRouting set by convention when invoking Get without languagerouting.", (object)type.FullName));
            return this.Get<TSource>(ids.Select<DocumentId, Tuple<DocumentId, LanguageRouting>>((Func<DocumentId, Tuple<DocumentId, LanguageRouting>>)(i => new Tuple<DocumentId, LanguageRouting>(i, (LanguageRouting)null))));
        }

        public IEnumerable<GetResult<TSource>> Get<TSource>(IEnumerable<Tuple<DocumentId, LanguageRouting>> ids)
        {
            ids = (IEnumerable<Tuple<DocumentId, LanguageRouting>>)ids.ToList<Tuple<DocumentId, LanguageRouting>>();
            ids.ValidateNotNullOrEmptyArgument(nameof(ids));
            List<IdAndType> idAndTypeList = new List<IdAndType>();
            foreach (Tuple<DocumentId, LanguageRouting> id in ids)
            {
                LanguageRouting routingOrDefault = id.Item2;
                if (routingOrDefault != null && routingOrDefault.IsValid())
                    routingOrDefault = this.GetSupportedLanguageRoutingOrDefault(routingOrDefault);
                idAndTypeList.Add(new IdAndType(id.Item1, (TypeName)this.Conventions.TypeNameConvention.GetTypeName(typeof(TSource)), routingOrDefault, this.DefaultIndex));
            }
            MultiGetCommand<TSource> multiGetCommand = this.commands.MultiGet<TSource>((IndexName)this.DefaultIndex, (IEnumerable<IdAndType>)idAndTypeList);
            this.PrepareSerializerUsingConventions(multiGetCommand.CommandContext.Serializer);
            return multiGetCommand.Execute().Results;
        }

        public string GetServiceUrlWithDefaultIndex()
        {
            string serviceUrl = this.ServiceUrl;
            if (!serviceUrl.EndsWith("/"))
                serviceUrl += "/";
            return serviceUrl + this.DefaultIndex;
        }

        public Settings Settings
        {
            get
            {
                if (this.settings.IsNull())
                {
                    try
                    {
                        this.settings = this.GetSettings().Settings;
                    }
                    catch
                    {
                        return new Settings()
                        {
                            Admin = false,
                            Stats = false,
                            Languages = new Languages()
                        };
                    }
                }
                return this.settings;
            }
        }

        public SettingsResult GetSettings()
        {
            return this.commands.New<SettingsCommand>((Func<CommandContext, SettingsCommand>)(context => new SettingsCommand((ICommandContext)context, (IndexName)this.DefaultIndex))).Execute();
        }

        public T NewCommand<T>(Func<CommandContext, T> createMethod) where T : Command
        {
            return this.commands.New<T>(createMethod);
        }

        public IMultiSearch<TSource> MultiSearch<TSource>()
        {
            return (IMultiSearch<TSource>)new MultiSearch<TSource>((IClient)this);
        }

        public IEnumerable<SearchResults<TResult>> MultiSearch<TResult>(IEnumerable<Tuple<SearchRequestBody, Action<SearchCommand>>> searchRequests, Action<MultiSearchCommand> commandAction)
        {
            searchRequests.ValidateNotNullOrEmptyArgument(nameof(searchRequests));
            MultiSearchCommand<TResult> multiSearchCommand = this.commands.MultiSearch<TResult>();
            foreach (Tuple<SearchRequestBody, Action<SearchCommand>> searchRequest in searchRequests)
            {
                SearchCommand<TResult> searchCommand = this.commands.Search<TResult>();
                searchCommand.Body = searchRequest.Item1.Clone();
                this.PrepareSerializerUsingConventions(searchCommand.CommandContext.Serializer);
                searchCommand.Indexes.Add((IndexName)this.DefaultIndex);
                if (searchRequest.Item2.IsNotNull())
                    searchRequest.Item2((SearchCommand)searchCommand);
                multiSearchCommand.SearchCommands.Add((SearchCommand)searchCommand);
            }
            if (commandAction.IsNotNull())
                commandAction((MultiSearchCommand)multiSearchCommand);
            foreach (SearchResult<TResult> searchResult in multiSearchCommand.Execute())
                yield return new SearchResults<TResult>(searchResult);
        }

        private LanguageRouting GetSupportedLanguageRoutingOrDefault(LanguageRouting languageRouting)
        {
            if (languageRouting == null)
                return (LanguageRouting)null;
            if ((!this.Settings.Languages.Any<Language>((Func<Language, bool>)(l => l.FieldSuffix.Equals(languageRouting.FieldSuffix))) ? 0 : (languageRouting.IsValid() ? 1 : 0)) == 0)
                return new LanguageRouting(Language.None.FieldSuffix);
            return languageRouting;
        }
    }
}
