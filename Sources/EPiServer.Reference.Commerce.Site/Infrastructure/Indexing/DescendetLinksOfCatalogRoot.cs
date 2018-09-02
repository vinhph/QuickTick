using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Find.Cms;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;

namespace EPiServer.Reference.Commerce.Site.Infrastructure.Indexing
{
    [ServiceConfiguration(typeof(IReindexInformation))]
    public class DescendetLinksOfCatalogRoot // : IReindexInformation
    {
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;
        private readonly ILanguageBranchRepository _languageBranchRepository;

        public DescendetLinksOfCatalogRoot(ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            ILanguageBranchRepository languageBranchRepository)
        {
            _contentLoader = contentLoader;
            _referenceConverter = referenceConverter;
            _languageBranchRepository = languageBranchRepository;
        }

        /// <summary>
        /// Returns all descendents of the <see cref="Root"/>.
        /// </summary>
        public IEnumerable<ReindexTarget> ReindexTargets
        {
            get
            {
                var catalogs = _contentLoader.GetChildren<CatalogContent>(Root);
                foreach (var catalogContent in catalogs)
                {
                    var reindexTarget = new ReindexTarget()
                    {
                        ContentLinks = _contentLoader.GetDescendents(catalogContent.ContentLink)
                    };

                    var languages = catalogContent.ExistingLanguages.ToList();
                    if (!languages.Select(x => x.Name).Contains(catalogContent.DefaultLanguage))
                    {
                        languages.Add(CultureInfo.GetCultureInfo(catalogContent.DefaultLanguage));
                    }

                    reindexTarget.Languages = languages;
                    yield return reindexTarget;
                }
            }
        }

        /// <summary>
        /// Gets the reference of the catalog root.
        /// </summary>
        public ContentReference Root
        {
            get { return _referenceConverter.GetRootLink(); }
        }
    }
}