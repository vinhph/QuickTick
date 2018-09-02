using EPiServer.Commerce.FindSearchProvider;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Catalog.Objects;
using Mediachase.Commerce.Extensions;
using Mediachase.Commerce.InventoryService;
using Mediachase.Commerce.Pricing;
using Mediachase.MetaDataPlus;
using Mediachase.MetaDataPlus.Configurator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OSG.Search
{
    internal class FindDocumentBuilder
    {
        private static string FormatStructureElement(string value)
        {
            return value.ToLowerInvariant().Replace('/', '_');
        }

        public FindDocument Document { get; private set; }

        private static ICatalogSystem CatalogSystem
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ICatalogSystem>();
            }
        }

        public FindDocumentBuilder(CatalogEntryDto.CatalogEntryRow entryRow, ISearchConfiguration configuration, IEnumerable<string> languages)
        {
            this.Document = new FindDocument();
            this.Document.CatalogEntryId = entryRow.CatalogEntryId;
            this.Document.CatalogEntryCode = entryRow.Code.ToLowerInvariant();
            this.Document.CatalogEntryType = entryRow.ClassTypeId.ToLowerInvariant();
            this.Document.MetaClassName = MetaClass.Load(CatalogContext.MetaDataContext, entryRow.MetaClassId).FriendlyName;
            this.Document.Name = entryRow.Name.ToLowerInvariant();
            this.Document.StartDate = entryRow.StartDate;
            this.Document.EndDate = entryRow.EndDate;
            this.Document.Languages = languages.Where<string>((Func<string, bool>)(l => entryRow.IsEntryPublished(l))).ToList<string>();
            this.Document.IsActive = entryRow.IsActive;
            this.BuildPreorderFields(this.Document, entryRow.Code);
            this.BuildCatalogStructure(this.Document, entryRow);
            this.BuildPrices(this.Document, entryRow, configuration);
            this.BuildMetaFields(this.Document, entryRow, configuration);
        }

        private void BuildCatalogStructure(FindDocument document, CatalogEntryDto.CatalogEntryRow entryRow)
        {
            Dictionary<int, FindDocumentBuilder.CatalogInfo> catalogs = new Dictionary<int, FindDocumentBuilder.CatalogInfo>();
            Dictionary<int, FindDocumentBuilder.CatalogNodeInfo> catalogNodes = new Dictionary<int, FindDocumentBuilder.CatalogNodeInfo>();
            List<FindDocumentBuilder.NodeEntryRelationInfo> list = FindDocumentBuilder.CatalogSystem.GetCatalogRelationDto(entryRow.CatalogEntryId).NodeEntryRelation.Select<CatalogRelationDto.NodeEntryRelationRow, FindDocumentBuilder.NodeEntryRelationInfo>((Func<CatalogRelationDto.NodeEntryRelationRow, FindDocumentBuilder.NodeEntryRelationInfo>)(r => new FindDocumentBuilder.NodeEntryRelationInfo(r, catalogs, catalogNodes))).ToList<FindDocumentBuilder.NodeEntryRelationInfo>();
            FindDocumentBuilder.CatalogInfo.GetCatalog(entryRow.CatalogId, catalogs);
            document.Catalogs = catalogs.Select<KeyValuePair<int, FindDocumentBuilder.CatalogInfo>, string>((Func<KeyValuePair<int, FindDocumentBuilder.CatalogInfo>, string>)(kv => kv.Value.CatalogName)).Distinct<string>().OrderBy<string, string>((Func<string, string>)(s => s)).ToList<string>();
            document.CatalogNodes = catalogNodes.Select<KeyValuePair<int, FindDocumentBuilder.CatalogNodeInfo>, string>((Func<KeyValuePair<int, FindDocumentBuilder.CatalogNodeInfo>, string>)(kv => kv.Value.CatalogNodeCode)).Distinct<string>().OrderBy<string, string>((Func<string, string>)(s => s)).ToList<string>();
            document.Outlines = list.SelectMany<FindDocumentBuilder.NodeEntryRelationInfo, string>((Func<FindDocumentBuilder.NodeEntryRelationInfo, IEnumerable<string>>)(r => this.BuildOutlines(r))).Distinct<string>().OrderBy<string, string>((Func<string, string>)(s => s)).ToList<string>();
        }

        private IEnumerable<string> BuildOutlines(FindDocumentBuilder.NodeEntryRelationInfo relation)
        {
            HashSet<string> stringSet = new HashSet<string>();
            List<string> stringList = new List<string>();
            stringSet.Add(relation.Catalog.CatalogName);
            for (FindDocumentBuilder.CatalogNodeInfo catalogNodeInfo = relation.Node; catalogNodeInfo != null; catalogNodeInfo = catalogNodeInfo.ParentCatalogNode)
            {
                stringSet.Add(catalogNodeInfo.Catalog.CatalogName);
                stringList.Add(catalogNodeInfo.CatalogNodeCode);
            }
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = stringList.Count - 1; index >= 0; --index)
                stringBuilder.Append('/').Append(stringList[index]);
            string nodePath = stringBuilder.ToString();
            foreach (string str in stringSet)
                yield return str + nodePath;
        }

        private void BuildPreorderFields(FindDocument document, string entryCode)
        {
            DateTime safeBeginningOfTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IEnumerable<InventoryRecord> source = ServiceLocator.Current.GetInstance<IInventoryService>().QueryByEntry((IEnumerable<string>)new string[1]
            {
        entryCode
            }).Where<InventoryRecord>((Func<InventoryRecord, bool>)(i => i.PreorderAvailableUtc > safeBeginningOfTime));
            bool flag = source.Any<InventoryRecord>();
            document.AllowPreorder = flag;
            if (!flag)
                return;
            document.PreorderAvailableDate = source.Select<InventoryRecord, DateTime>((Func<InventoryRecord, DateTime>)(i => i.PreorderAvailableUtc)).Min<DateTime>();
        }

        private void BuildPrices(FindDocument document, CatalogEntryDto.CatalogEntryRow entryRow, ISearchConfiguration configuration)
        {
            IPriceService instance = ServiceLocator.Current.GetInstance<IPriceService>();
            CatalogKey catalogKey = new CatalogKey(entryRow);
            HashSet<Mediachase.Commerce.Currency> source1 = new HashSet<Mediachase.Commerce.Currency>();
            HashSet<MarketId> source2 = new HashSet<MarketId>();
            IFieldConfiguration<double?> field = null;
            foreach (IPriceValue catalogEntryPrice in instance.GetCatalogEntryPrices(catalogKey))
            {
                HashSet<Mediachase.Commerce.Currency> currencySet = source1;
                Money unitPrice = catalogEntryPrice.UnitPrice;
                Mediachase.Commerce.Currency currency1 = unitPrice.Currency;
                if (currencySet.Add(currency1))
                {
                    ISearchConfiguration searchConfiguration = configuration;
                    unitPrice = catalogEntryPrice.UnitPrice;
                    Mediachase.Commerce.Currency currency2 = unitPrice.Currency;
                    searchConfiguration.AddCurrency(currency2);
                }
                if (source2.Add(catalogEntryPrice.MarketId))
                    configuration.AddMarket(catalogEntryPrice.MarketId);
                ISearchConfiguration searchConfiguration1 = configuration;
                string name = "saleprice";
                unitPrice = catalogEntryPrice.UnitPrice;
                Mediachase.Commerce.Currency currencyCode = (Mediachase.Commerce.Currency)unitPrice.Currency.CurrencyCode;
                MarketId marketId = (MarketId)catalogEntryPrice.MarketId.Value;
                ref IFieldConfiguration<double?> local = ref field;
                if (searchConfiguration1.TryGetPriceField(name, currencyCode, marketId, out local))
                {
                    unitPrice = catalogEntryPrice.UnitPrice;
                    double amount = (double)unitPrice.Amount;
                    double? nullable = field.GetValue(document);
                    if (!nullable.HasValue || nullable.Value > amount)
                        field.SetValue(document, new double?(amount));
                }
            }
            DateTime currentDateTime = FrameworkContext.Current.CurrentDateTime;
            PriceFilter filter = new PriceFilter();
            filter.Currencies = (IEnumerable<Mediachase.Commerce.Currency>)source1;
            filter.CustomerPricing = (IEnumerable<CustomerPricing>)new List<CustomerPricing>()
      {
        CustomerPricing.AllCustomers
      };
            filter.Quantity = new Decimal?(Decimal.Zero);
            foreach (MarketId marketId in source2)
            {
                foreach (IPriceValue price in instance.GetPrices(marketId, currentDateTime, catalogKey, filter))
                {
                    if (configuration.TryGetPriceField("listprice", (Mediachase.Commerce.Currency)price.UnitPrice.Currency.CurrencyCode, marketId, out field))
                        field.SetValue(document, new double?((double)price.UnitPrice.Amount));
                }
            }
            document.Currencies = source1.Select<Mediachase.Commerce.Currency, string>((Func<Mediachase.Commerce.Currency, string>)(c => c.CurrencyCode)).ToList<string>();
            document.Markets = source2.Select<MarketId, string>((Func<MarketId, string>)(m => m.Value)).ToList<string>();
        }

        private void BuildMetaFields(FindDocument document, CatalogEntryDto.CatalogEntryRow entryRow, ISearchConfiguration configuration)
        {
            Dictionary<string, Hashtable> allMetaFieldValues = this.GetAllMetaFieldValues(entryRow, (IEnumerable<string>)this.Document.Languages);
            foreach (IFieldConfiguration dynamicField in configuration.GetDynamicFields((IEnumerable<string>)this.Document.Languages))
            {
                object metaFieldValue = this.GetMetaFieldValue(allMetaFieldValues, dynamicField);
                if (metaFieldValue != null)
                {
                    object obj;
                    if (dynamicField.Type == typeof(IEnumerable<string>))
                    {
                        string[] strArray;
                        if ((strArray = metaFieldValue as string[]) != null)
                        {
                            List<string> list = ((IEnumerable<string>)strArray).Where<string>((Func<string, bool>)(s => s != null)).ToList<string>();
                            obj = list.Count > 0 ? (object)list : (object)(List<string>)null;
                        }
                        else
                        {
                            MetaStringDictionary stringDictionary;
                            if ((stringDictionary = metaFieldValue as MetaStringDictionary) != null)
                            {
                                List<string> list = stringDictionary.Keys.Cast<string>().Where<string>((Func<string, bool>)(s => s != null)).ToList<string>();
                                obj = list.Count > 0 ? (object)list : (object)(List<string>)null;
                            }
                            else
                            {
                                MetaDictionaryItem[] metaDictionaryItemArray;
                                if ((metaDictionaryItemArray = metaFieldValue as MetaDictionaryItem[]) == null)
                                    throw new Exception(string.Format("Unexpected data type {0} for string collection.", (object)metaFieldValue.GetType().Name));
                                List<string> list = ((IEnumerable<MetaDictionaryItem>)metaDictionaryItemArray).Select<MetaDictionaryItem, string>((Func<MetaDictionaryItem, string>)(item => item.Value)).ToList<string>();
                                obj = list.Count > 0 ? (object)list : (object)(List<string>)null;
                            }
                        }
                    }
                    else if (dynamicField.Type == typeof(string))
                    {
                        MetaDictionaryItem metaDictionaryItem = metaFieldValue as MetaDictionaryItem;
                        obj = metaDictionaryItem == null ? (object)metaFieldValue.ToString() : (object)metaDictionaryItem.Value;
                    }
                    else if (dynamicField.Type == typeof(bool?))
                    {
                        bool? nullable = metaFieldValue as bool?;
                        obj = (object)(bool)(nullable.HasValue ? (nullable.GetValueOrDefault() ? true : false) : (bool.Parse(metaFieldValue.ToString()) ? true : false));
                    }
                    else if (dynamicField.Type == typeof(int?))
                        obj = (object)(metaFieldValue as int? ?? int.Parse(metaFieldValue.ToString()));
                    else if (dynamicField.Type == typeof(long?))
                        obj = (object)(metaFieldValue as long? ?? long.Parse(metaFieldValue.ToString()));
                    else if (dynamicField.Type == typeof(double?))
                    {
                        obj = (object)(metaFieldValue as double? ?? double.Parse(metaFieldValue.ToString(), (IFormatProvider)CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        if (!(dynamicField.Type == typeof(DateTime?)))
                            throw new Exception(string.Format("Cannot populate field of type {0}.", (object)dynamicField.Type.Name));
                        obj = (object)(metaFieldValue as DateTime? ?? DateTime.Parse(metaFieldValue.ToString()));
                    }
                    if (obj != null)
                        dynamicField.SetObjectValue(document, obj);
                }
            }
        }

        private Dictionary<string, Hashtable> GetAllMetaFieldValues(CatalogEntryDto.CatalogEntryRow entryRow, IEnumerable<string> languages)
        {
            Dictionary<string, Hashtable> dictionary = new Dictionary<string, Hashtable>();
            MetaDataContext context = CatalogContext.MetaDataContext.Clone();
            context.UseCurrentThreadCulture = false;
            MetaClass metaClass = MetaClass.Load(context, entryRow.MetaClassId);
            ItemAttributes metaAttributes = new ItemAttributes();
            foreach (string language in languages)
            {
                context.Language = language;
                Hashtable metaFieldValues = ObjectHelper.GetMetaFieldValues((DataRow)entryRow, metaClass, ref metaAttributes, context);
                dictionary.Add(language, metaFieldValues);
            }
            return dictionary;
        }

        private object GetMetaFieldValue(Dictionary<string, Hashtable> metaFieldValues, IFieldConfiguration field)
        {
            Hashtable hashtable = (Hashtable)null;
            if (field.Locale == null || !metaFieldValues.TryGetValue(field.Locale, out hashtable))
                hashtable = metaFieldValues.Values.FirstOrDefault<Hashtable>();
            return hashtable?[(object)field.Name];
        }

        private class CatalogInfo
        {
            public int CatalogId { get; private set; }

            public string CatalogName { get; private set; }

            public static FindDocumentBuilder.CatalogInfo GetCatalog(int catalogId, Dictionary<int, FindDocumentBuilder.CatalogInfo> loadedCatalogs)
            {
                FindDocumentBuilder.CatalogInfo catalogInfo;
                if (!loadedCatalogs.TryGetValue(catalogId, out catalogInfo))
                {
                    CatalogDto.CatalogRow catalogRow = FindDocumentBuilder.CatalogSystem.GetCatalogDto(catalogId).Catalog.Single<CatalogDto.CatalogRow>();
                    catalogInfo = new FindDocumentBuilder.CatalogInfo()
                    {
                        CatalogId = catalogId,
                        CatalogName = FindDocumentBuilder.FormatStructureElement(catalogRow.Name)
                    };
                    loadedCatalogs.Add(catalogId, catalogInfo);
                }
                return catalogInfo;
            }
        }

        private class CatalogNodeInfo
        {
            public int CatalogNodeId { get; private set; }

            public string CatalogNodeCode { get; private set; }

            public FindDocumentBuilder.CatalogInfo Catalog { get; private set; }

            public FindDocumentBuilder.CatalogNodeInfo ParentCatalogNode { get; private set; }

            public static FindDocumentBuilder.CatalogNodeInfo GetCatalogNode(int catalogNodeId, Dictionary<int, FindDocumentBuilder.CatalogInfo> catalogs, Dictionary<int, FindDocumentBuilder.CatalogNodeInfo> catalogNodes)
            {
                FindDocumentBuilder.CatalogNodeInfo catalogNodeInfo;
                if (!catalogNodes.TryGetValue(catalogNodeId, out catalogNodeInfo))
                {
                    CatalogNodeDto.CatalogNodeRow catalogNodeRow = FindDocumentBuilder.CatalogSystem.GetCatalogNodeDto(catalogNodeId).CatalogNode.Single<CatalogNodeDto.CatalogNodeRow>();
                    catalogNodeInfo = new FindDocumentBuilder.CatalogNodeInfo()
                    {
                        CatalogNodeId = catalogNodeId,
                        CatalogNodeCode = FindDocumentBuilder.FormatStructureElement(catalogNodeRow.Code),
                        Catalog = FindDocumentBuilder.CatalogInfo.GetCatalog(catalogNodeRow.CatalogId, catalogs),
                        ParentCatalogNode = (FindDocumentBuilder.CatalogNodeInfo)null
                    };
                    catalogNodes.Add(catalogNodeId, catalogNodeInfo);
                    if (catalogNodeRow.ParentNodeId > 0)
                        catalogNodeInfo.ParentCatalogNode = FindDocumentBuilder.CatalogNodeInfo.GetCatalogNode(catalogNodeRow.ParentNodeId, catalogs, catalogNodes);
                }
                return catalogNodeInfo;
            }
        }

        private class NodeEntryRelationInfo
        {
            public FindDocumentBuilder.CatalogInfo Catalog { get; private set; }

            public FindDocumentBuilder.CatalogNodeInfo Node { get; private set; }

            public NodeEntryRelationInfo(CatalogRelationDto.NodeEntryRelationRow relationRow, Dictionary<int, FindDocumentBuilder.CatalogInfo> catalogs, Dictionary<int, FindDocumentBuilder.CatalogNodeInfo> catalogNodes)
            {
                this.Catalog = FindDocumentBuilder.CatalogInfo.GetCatalog(relationRow.CatalogId, catalogs);
                this.Node = FindDocumentBuilder.CatalogNodeInfo.GetCatalogNode(relationRow.CatalogNodeId, catalogs, catalogNodes);
            }
        }
    }
}
