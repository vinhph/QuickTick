using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Find.Framework;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Reference.Commerce.Site.Features.OSGCustomize.Pages;
using EPiServer.Reference.Commerce.Site.Features.OSGCustomize.ViewModels;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc;
using Mediachase.Commerce.Catalog;

namespace EPiServer.Reference.Commerce.Site.Features.OSGCustomize.Controllers
{
    public class IndexJobController : PageController<IndexJob>
    {
        private readonly IContentLoader _contentLoader;
        private readonly IRelationRepository _relationRepository;
        private readonly IContentRepository _repository;
        private readonly IClient _client;
        public IndexJobController(IContentLoader contentLoader, 
            IRelationRepository relationRepository,
                IContentRepository repository)
        {
            _contentLoader = contentLoader;
            //var relationRepository = ServiceLocator.Current.GetInstance<IRelationRepository>();
            _relationRepository = relationRepository;
            _repository = repository;
            _client = SearchClient.Instance;
        }
        public ActionResult Index(IndexJob currentPage)
        {
            var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            var catalogues = _repository.GetChildren<CatalogContent>(referenceConverter.GetRootLink());
            GetAllNodeOfCatalogues(catalogues);
            string returnValue = catalogues.FirstOrDefault()?.Name;
            var model = new IndexJobViewModel()
            {
                CurrentPage = currentPage,
                ReturnValue = returnValue
            };
            return View(model);
        }

        public ActionResult Search(IndexJob currentPage)
        {            
            var variations = SearchByMultyQueryAndBuildFilter();//SearchPrefix();
            string returnValue = "SL: " + variations?.Count() + " Item1: " + variations?.FirstOrDefault<FashionVariant>()?.DisplayName;
            var model = new IndexJobViewModel()
            {
                CurrentPage = currentPage,
                ReturnValue = returnValue
            };
            return View("Index", model);
        }

        private IContentResult<FashionVariant> SearchByMultyQueryAndBuildFilter()
        {
            ITypeSearch<FashionVariant> query = _client.Search<FashionVariant>().Filter(x => x.Name.Prefix("Wrangler"));

            var prefixNameFilter = _client.BuildFilter<FashionVariant>();
            IEnumerable<string> prefixNameAvaiables = new List<string>()
            {
                "W", "A", "B"
            };
            foreach (var prefixNameAvaiable in prefixNameAvaiables)
            {
                prefixNameFilter = prefixNameFilter.Or(
                    x => x.DisplayName.Prefix(prefixNameAvaiable));
            }
            query = query.Filter(prefixNameFilter);
            return query.GetContentResult();
        }

        private IContentResult<FashionVariant> SearchPrefix()
        {
            return _client.Search<FashionVariant>().Filter(x => x.Name.Prefix("Wrangler")).GetContentResult();
        }

        private void GetAllNodeOfCatalogues(IEnumerable<CatalogContent> catalogues)
        {
            foreach (var catalog in catalogues)
            {
                var nodes = _repository.GetChildren<NodeContent>(catalog?.ContentLink);
                GetAllNodeOfNodes(nodes);
                GetAllProductOfNodes(nodes);
            }

        }

        private void GetAllNodeOfNodes(IEnumerable<NodeContent> nodes)
        {
            foreach (var node in nodes)
            {
                var childNodes = _repository.GetChildren<NodeContent>(node?.ContentLink);
                GetAllNodeOfNodes(childNodes);
                GetAllProductOfNodes(nodes);
            }

        }

        private void GetAllProductOfNodes(IEnumerable<NodeContent> nodes)
        {
            foreach (var node in nodes)
            {
                var products = _repository.GetChildren<ProductContent>(node?.ContentLink);
                GetAllVariationOfProduct(products);
            }
        }

        private void GetAllVariationOfProduct(IEnumerable<ProductContent> products)
        {            
            foreach (var product in products)
            {
                var variants = product.GetVariants(_relationRepository).Select(contentLink => _contentLoader.Get<FashionVariant>(contentLink));
                if (variants?.Count() > 0)
                {
                    foreach (var variant in variants)
                    {
                        _client.Index(variant);
                    }
                }
            }
        }
    }
}