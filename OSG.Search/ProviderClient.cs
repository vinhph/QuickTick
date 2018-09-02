using EPiServer.Commerce.FindSearchProvider;
using EPiServer.Find;

namespace OSG.Search
{
    public class ProviderClient : Client
    {
        /// <summary>This property is intended for internal use only.</summary>
        public virtual ISearchConfiguration ProviderConfiguration { get; private set; }

        /// <summary>For internal use only.</summary>
        /// <param name="configuration"></param>
        public ProviderClient(ISearchConfiguration configuration)
            : base(configuration.ServiceUrl, configuration.DefaultIndex, true, new int?())
        {
            this.ProviderConfiguration = configuration;
        }
    }
}
