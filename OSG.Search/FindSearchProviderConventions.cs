using System;
using EPiServer.Commerce.FindSearchProvider;
using EPiServer.ServiceLocation;

namespace OSG.Search
{
    [ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    public class FindSearchProviderConventions
    {
        /// <summary>The default query language option.</summary>
        public Func<QueryCultureOption> DefaultQueryCultureOption = (Func<QueryCultureOption>)(() => QueryCultureOption.SpecificAndNeutral);
    }
}
