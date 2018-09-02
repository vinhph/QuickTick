using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EPiServer.Commerce.FindSearchProvider;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.MetaDataPlus.Configurator;

namespace OSG.Search
{
    internal class ProviderConfiguration : ISearchConfiguration
    {
        private object _knownConfigurationLock = new object();
        private static readonly Dictionary<MetaDataType, Type> _documentFieldTypes = new Dictionary<MetaDataType, Type>()
    {
      {
        MetaDataType.Bit,
        typeof (bool?)
      },
      {
        MetaDataType.Boolean,
        typeof (bool?)
      },
      {
        MetaDataType.TinyInt,
        typeof (int?)
      },
      {
        MetaDataType.SmallInt,
        typeof (int?)
      },
      {
        MetaDataType.Int,
        typeof (int?)
      },
      {
        MetaDataType.Integer,
        typeof (int?)
      },
      {
        MetaDataType.BigInt,
        typeof (long?)
      },
      {
        MetaDataType.Date,
        typeof (DateTime?)
      },
      {
        MetaDataType.SmallDateTime,
        typeof (DateTime?)
      },
      {
        MetaDataType.DateTime,
        typeof (DateTime?)
      },
      {
        MetaDataType.Decimal,
        typeof (double?)
      },
      {
        MetaDataType.Money,
        typeof (double?)
      },
      {
        MetaDataType.Float,
        typeof (double?)
      },
      {
        MetaDataType.Numeric,
        typeof (double?)
      },
      {
        MetaDataType.Real,
        typeof (double?)
      },
      {
        MetaDataType.SmallMoney,
        typeof (double?)
      },
      {
        MetaDataType.Char,
        typeof (string)
      },
      {
        MetaDataType.NChar,
        typeof (string)
      },
      {
        MetaDataType.NText,
        typeof (string)
      },
      {
        MetaDataType.NVarChar,
        typeof (string)
      },
      {
        MetaDataType.UniqueIdentifier,
        typeof (string)
      },
      {
        MetaDataType.Text,
        typeof (string)
      },
      {
        MetaDataType.VarChar,
        typeof (string)
      },
      {
        MetaDataType.Sysname,
        typeof (string)
      },
      {
        MetaDataType.ShortString,
        typeof (string)
      },
      {
        MetaDataType.LongString,
        typeof (string)
      },
      {
        MetaDataType.URL,
        typeof (string)
      },
      {
        MetaDataType.Email,
        typeof (string)
      },
      {
        MetaDataType.LongHtmlString,
        typeof (string)
      },
      {
        MetaDataType.DictionarySingleValue,
        typeof (string)
      },
      {
        MetaDataType.EnumSingleValue,
        typeof (string)
      },
      {
        MetaDataType.DictionaryMultiValue,
        typeof (IEnumerable<string>)
      },
      {
        MetaDataType.EnumMultiValue,
        typeof (IEnumerable<string>)
      },
      {
        MetaDataType.StringDictionary,
        typeof (IEnumerable<string>)
      }
    };
        private static readonly Regex CommaSeparatedListExpression = new Regex("^(\\s*,\\s*)*([-A-Za-z]+(\\s*,\\s*)+)*([-A-Za-z]+(\\s*,\\s*)*)?\\s*$", RegexOptions.Compiled);
        private static readonly Regex CommaSeparatedListEntryExpression = new Regex("[-A-Za-z]+", RegexOptions.Compiled);
        private static readonly Dictionary<string, string> _BackwardsCompatibleNames = new Dictionary<string, string>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        internal const string ListPriceName = "listprice";
        internal const string SalePriceName = "saleprice";
        private HashSet<string> _knownLocales;
        private HashSet<Mediachase.Commerce.Currency> _knownCurrencies;
        private HashSet<MarketId> _knownMarkets;
        private List<ProviderConfiguration.MultilanguageFieldGenerator> _multilanguageFieldGenerators;
        private List<IFieldConfiguration> _fields;
        private List<IFieldConfiguration> _backwardsCompatibleFields;
        private Dictionary<string, List<IFieldConfiguration>> _fieldMap;

        static ProviderConfiguration()
        {
            ProviderConfiguration._BackwardsCompatibleNames.Add("_catalog", "catalogs");
            ProviderConfiguration._BackwardsCompatibleNames.Add("_classtype", "catalogentrytype");
            ProviderConfiguration._BackwardsCompatibleNames.Add("_lang", "languages");
            ProviderConfiguration._BackwardsCompatibleNames.Add("_metaclass", "metaclassname");
            ProviderConfiguration._BackwardsCompatibleNames.Add("_outline", "outlines");
            ProviderConfiguration._BackwardsCompatibleNames.Add("code", "catalogentrycode");
        }

        public string ServiceUrl { get; private set; }

        public string DefaultIndex { get; private set; }

        public ProviderConfiguration(NameValueCollection config)
        {
            this.ServiceUrl = this.GetSettingWithFallback("episerver:FindServiceUrl", "serviceUrl", config);
            this.DefaultIndex = this.GetSettingWithFallback("episerver:FindDefaultIndex", "defaultIndex", config);
            if (config.Count > 0)
                throw new ProviderException(string.Format("Unrecognized attribute \"{0}\" in configuration", (object)config.GetKey(0)));
            this._knownLocales = new HashSet<string>();
            this._knownCurrencies = new HashSet<Mediachase.Commerce.Currency>();
            this._knownMarkets = new HashSet<MarketId>();
            this.BuildFieldConfiguration();
            this._backwardsCompatibleFields = new List<IFieldConfiguration>(ProviderConfiguration._BackwardsCompatibleNames.Count);
            foreach (KeyValuePair<string, string> backwardsCompatibleName in ProviderConfiguration._BackwardsCompatibleNames)
            {
                KeyValuePair<string, string> compatibleNamePair = backwardsCompatibleName;
                ProviderFieldConfiguration fieldConfiguration = (ProviderFieldConfiguration)this._fields.SingleOrDefault<IFieldConfiguration>((Func<IFieldConfiguration, bool>)(f =>
                {
                    if (string.Equals(f.Name, compatibleNamePair.Value) && f.Locale == null)
                        return !f.Currency.HasValue;
                    return false;
                }));
                if (fieldConfiguration != null && !this._fields.Any<IFieldConfiguration>((Func<IFieldConfiguration, bool>)(f => string.Equals(f.Name, compatibleNamePair.Key, StringComparison.OrdinalIgnoreCase))))
                    this._backwardsCompatibleFields.Add((IFieldConfiguration)fieldConfiguration.CloneRenamed(compatibleNamePair.Key));
            }
            this._backwardsCompatibleFields.AddRange(this._fields.Where<IFieldConfiguration>((Func<IFieldConfiguration, bool>)(f =>
            {
                if (f.Name == "saleprice" || f.Name == "listprice")
                    return f.Currency.HasValue;
                return false;
            })).Cast<ProviderFieldConfiguration>().Select<ProviderFieldConfiguration, IFieldConfiguration>((Func<ProviderFieldConfiguration, IFieldConfiguration>)(f => (IFieldConfiguration)f.CloneRenamed(string.Format("{0}{1}_{2}", (object)f.Name, (object)f.Currency.Value.CurrencyCode, (object)f.MarketId.Value.Value)))));
            this._fieldMap = this._fields.Concat<IFieldConfiguration>((IEnumerable<IFieldConfiguration>)this._backwardsCompatibleFields).GroupBy<IFieldConfiguration, string>((Func<IFieldConfiguration, string>)(f => f.Name.ToLowerInvariant())).ToDictionary<IGrouping<string, IFieldConfiguration>, string, List<IFieldConfiguration>>((Func<IGrouping<string, IFieldConfiguration>, string>)(g => g.Key), (Func<IGrouping<string, IFieldConfiguration>, List<IFieldConfiguration>>)(g => g.ToList<IFieldConfiguration>()), (IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        }

        public IFieldConfiguration GetField(string name, string locale)
        {
            IFieldConfiguration field;
            if (!this.TryGetField(name, locale, out field))
                throw new Exception(locale == null ? string.Format("Field \"{0}\" not found.", (object)name) : string.Format("Field \"{0}\" for locale \"{1}\" not found.", (object)name, (object)locale));
            return field;
        }

        public IFieldConfiguration<double?> GetPriceField(string name, Mediachase.Commerce.Currency currency, MarketId marketId)
        {
            IFieldConfiguration<double?> field;
            if (!this.TryGetPriceField(name, currency, marketId, out field))
                throw new Exception(string.Format("Price field \"{0}\" for currency \"{1}\" and market \"{2}\" not found.", (object)name, (object)currency.CurrencyCode, (object)marketId.Value));
            return field;
        }

        public IFieldConfiguration GetAnyField(string name, string locale, Mediachase.Commerce.Currency currency, MarketId marketId)
        {
            IFieldConfiguration field;
            if (!this.TryGetAnyField(name, locale, currency, marketId, out field))
                throw new Exception(string.Format("Field matching values name \"{0}\", locale \"{1}\", currency \"{2}\", and market \"{3}\" not found.", new object[4]
                {
          (object) name,
          (object) locale,
          (object) currency.CurrencyCode,
          (object) marketId.Value
                }));
            return field;
        }

        public bool TryGetField(string name, string locale, out IFieldConfiguration field)
        {
            field = this.TryGetFieldWorker(name, locale, new Mediachase.Commerce.Currency?(), new MarketId?());
            return field != null;
        }

        public bool TryGetPriceField(string name, Mediachase.Commerce.Currency currency, MarketId marketId, out IFieldConfiguration<double?> field)
        {
            IFieldConfiguration fieldWorker = this.TryGetFieldWorker(name, (string)null, new Mediachase.Commerce.Currency?(currency), new MarketId?(marketId));
            field = fieldWorker as IFieldConfiguration<double?>;
            return field != null;
        }

        public bool TryGetAnyField(string name, string locale, Mediachase.Commerce.Currency currency, MarketId marketId, out IFieldConfiguration field)
        {
            field = this.TryGetFieldWorker(name, locale, new Mediachase.Commerce.Currency?(currency), new MarketId?(marketId));
            return field != null;
        }

        private IFieldConfiguration TryGetFieldWorker(string name, string locale, Mediachase.Commerce.Currency? currency, MarketId? marketId)
        {
            if (locale != null)
                this.AddLocale(locale);
            if (currency.HasValue)
                this.AddCurrency(currency.Value);
            if (marketId.HasValue)
                this.AddMarket(marketId.Value);
            List<IFieldConfiguration> fieldConfigurationList;
            if (!this._fieldMap.TryGetValue(name, out fieldConfigurationList))
                return (IFieldConfiguration)null;
            IFieldConfiguration fieldConfiguration1 = (IFieldConfiguration)null;
            IFieldConfiguration fieldConfiguration2 = (IFieldConfiguration)null;
            IFieldConfiguration fieldConfiguration3 = (IFieldConfiguration)null;
            foreach (IFieldConfiguration fieldConfiguration4 in fieldConfigurationList)
            {
                if (fieldConfiguration4.Locale == null && !fieldConfiguration4.Currency.HasValue)
                {
                    if (fieldConfiguration1 != null)
                        throw new Exception(string.Format("Multiple single-language fields found for \"{0}\".", (object)name));
                    fieldConfiguration1 = fieldConfiguration4;
                }
                else if (fieldConfiguration4.Locale != null)
                {
                    if (string.Equals(locale, fieldConfiguration4.Locale, StringComparison.OrdinalIgnoreCase))
                    {
                        if (fieldConfiguration2 != null)
                            throw new Exception(string.Format("Multiple multi-language fields found for \"{0}\", \"{1}\".", (object)name, (object)locale));
                        fieldConfiguration2 = fieldConfiguration4;
                    }
                }
                else if (fieldConfiguration4.Currency.HasValue && currency.HasValue && marketId.HasValue)
                {
                    Mediachase.Commerce.Currency? currency1 = fieldConfiguration4.Currency;
                    Mediachase.Commerce.Currency currency2 = currency.Value;
                    if ((currency1.HasValue ? (currency1.HasValue ? (currency1.GetValueOrDefault() == currency2 ? 1 : 0) : 1) : 0) != 0)
                    {
                        MarketId? marketId1 = fieldConfiguration4.MarketId;
                        MarketId marketId2 = marketId.Value;
                        if ((marketId1.HasValue ? (marketId1.HasValue ? (marketId1.GetValueOrDefault() == marketId2 ? 1 : 0) : 1) : 0) != 0)
                        {
                            if (fieldConfiguration3 != null)
                                throw new Exception(string.Format("Multiple price fields found for \"{0}\", \"{1}\", \"{2}\".", (object)name, (object)currency.Value.CurrencyCode, (object)marketId.Value.Value));
                            fieldConfiguration3 = fieldConfiguration4;
                        }
                    }
                }
            }
            IFieldConfiguration fieldConfiguration5 = fieldConfiguration2 ?? fieldConfiguration1;
            if (fieldConfiguration5 != null && fieldConfiguration3 != null)
                throw new Exception(string.Format("Multiple kinds of fields found for \"{0}\", \"{1}\", \"{2}\", \"{3}\".", new object[4]
                {
          (object) name,
          (object) (locale ?? string.Empty),
          marketId.HasValue ? (object) marketId.Value.Value : (object) string.Empty,
          currency.HasValue ? (object) currency.Value.CurrencyCode : (object) string.Empty
                }));
            return fieldConfiguration5 ?? fieldConfiguration3;
        }

        public IEnumerable<IFieldConfiguration> GetDefaultFields(string locale)
        {
            locale = locale.ToLowerInvariant();
            this.AddLocale(locale);
            this._fields.Select<IFieldConfiguration, string>((Func<IFieldConfiguration, string>)(f => string.Format("{0} {1}", (object)f.Name, (object)f.Locale))).ToList<string>();
            this._fields.Where<IFieldConfiguration>((Func<IFieldConfiguration, bool>)(f => f.IsIncludedInDefaultSearch)).Select<IFieldConfiguration, string>((Func<IFieldConfiguration, string>)(f => string.Format("{0} {1}", (object)f.Name, (object)f.Locale))).ToList<string>();
            return this._fields.Where<IFieldConfiguration>((Func<IFieldConfiguration, bool>)(f =>
            {
                if (!f.IsIncludedInDefaultSearch)
                    return false;
                if (f.Locale != null)
                    return string.Equals(f.Locale, locale, StringComparison.Ordinal);
                return true;
            }));
        }

        public IEnumerable<IFieldConfiguration> GetDefaultFields()
        {
            List<IFieldConfiguration> fieldConfigurationList = new List<IFieldConfiguration>();
            foreach (string knownLocale in this._knownLocales)
            {
                string locale = knownLocale;
                fieldConfigurationList.AddRange(this._fields.Where<IFieldConfiguration>((Func<IFieldConfiguration, bool>)(f =>
                {
                    if (!f.IsIncludedInDefaultSearch)
                        return false;
                    if (f.Locale != null)
                        return string.Equals(f.Locale, locale, StringComparison.Ordinal);
                    return true;
                })));
            }
            return (IEnumerable<IFieldConfiguration>)fieldConfigurationList;
        }

        public IEnumerable<IFieldConfiguration> GetDynamicFields(IEnumerable<string> locales)
        {
            HashSet<string> localeSet = new HashSet<string>(locales.Select<string, string>((Func<string, string>)(l => l.ToLowerInvariant())), (IEqualityComparer<string>)StringComparer.Ordinal);
            foreach (string locale in localeSet)
                this.AddLocale(locale);
            return this._fields.Where<IFieldConfiguration>((Func<IFieldConfiguration, bool>)(f =>
            {
                if (!f.IsDynamicField)
                    return false;
                if (f.Locale != null)
                    return localeSet.Contains(f.Locale);
                return true;
            }));
        }

        public IEnumerable<IFieldConfiguration> GetAllFields(string locale)
        {
            locale = locale.ToLowerInvariant();
            this.AddLocale(locale);
            return this._fields.Concat<IFieldConfiguration>((IEnumerable<IFieldConfiguration>)this._backwardsCompatibleFields).Where<IFieldConfiguration>((Func<IFieldConfiguration, bool>)(f =>
            {
                if (f.Locale != null)
                    return string.Equals(f.Locale, locale, StringComparison.Ordinal);
                return true;
            }));
        }

        public void AddLocale(string locale)
        {
            locale = locale.ToLowerInvariant();
            if (this._knownLocales.Contains(locale))
                return;
            lock (this._knownConfigurationLock)
            {
                if (this._knownLocales.Contains(locale))
                    return;
                this._knownLocales.Add(locale);
                foreach (IFieldConfiguration field in this._multilanguageFieldGenerators.Select<ProviderConfiguration.MultilanguageFieldGenerator, IFieldConfiguration>((Func<ProviderConfiguration.MultilanguageFieldGenerator, IFieldConfiguration>)(f => f.Generate(locale))))
                    this.AddField(field);
            }
        }

        public void AddCurrency(Mediachase.Commerce.Currency currency)
        {
            if (this._knownCurrencies.Contains(currency))
                return;
            lock (this._knownConfigurationLock)
            {
                if (this._knownCurrencies.Contains(currency))
                    return;
                this._knownCurrencies.Add(currency);
                foreach (MarketId knownMarket in this._knownMarkets)
                {
                    this.AddField((IFieldConfiguration)ProviderConfiguration.CreateListPriceField(currency, knownMarket));
                    this.AddField((IFieldConfiguration)ProviderConfiguration.CreateSalePriceField(currency, knownMarket));
                }
            }
        }

        public void AddMarket(MarketId marketId)
        {
            if (this._knownMarkets.Contains(marketId))
                return;
            lock (this._knownConfigurationLock)
            {
                if (this._knownMarkets.Contains(marketId))
                    return;
                this._knownMarkets.Add(marketId);
                foreach (Mediachase.Commerce.Currency knownCurrency in this._knownCurrencies)
                {
                    this.AddField((IFieldConfiguration)ProviderConfiguration.CreateListPriceField(knownCurrency, marketId));
                    this.AddField((IFieldConfiguration)ProviderConfiguration.CreateSalePriceField(knownCurrency, marketId));
                }
            }
        }

        private void AddField(IFieldConfiguration field)
        {
            this._fields.Add(field);
            List<IFieldConfiguration> fieldConfigurationList;
            if (!this._fieldMap.TryGetValue(field.Name, out fieldConfigurationList))
            {
                fieldConfigurationList = new List<IFieldConfiguration>();
                this._fieldMap.Add(field.Name, fieldConfigurationList);
            }
            fieldConfigurationList.Add(field);
        }

        private void BuildFieldConfiguration()
        {
            this._multilanguageFieldGenerators = new List<ProviderConfiguration.MultilanguageFieldGenerator>();
            this._fields = new List<IFieldConfiguration>();
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<int>("catalogentryid", (Expression<Func<FindDocument, int>>)(d => d.CatalogEntryId)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<DateTime>("startdate", (Expression<Func<FindDocument, DateTime>>)(d => d.StartDate)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<DateTime>("enddate", (Expression<Func<FindDocument, DateTime>>)(d => d.EndDate)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<string>("name", (Expression<Func<FindDocument, string>>)(d => d.Name), 1 != 0));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<string>("catalogentrycode", (Expression<Func<FindDocument, string>>)(d => d.CatalogEntryCode), 1 != 0));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<string>("metaclassname", (Expression<Func<FindDocument, string>>)(d => d.MetaClassName)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<string>("catalogentrytype", (Expression<Func<FindDocument, string>>)(d => d.CatalogEntryType)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<IEnumerable<string>>("languages", (Expression<Func<FindDocument, IEnumerable<string>>>)(d => d.Languages)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<IEnumerable<string>>("currencies", (Expression<Func<FindDocument, IEnumerable<string>>>)(d => d.Currencies)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<IEnumerable<string>>("markets", (Expression<Func<FindDocument, IEnumerable<string>>>)(d => d.Markets)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<IEnumerable<string>>("catalogs", (Expression<Func<FindDocument, IEnumerable<string>>>)(d => d.Catalogs)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<IEnumerable<string>>("outlines", (Expression<Func<FindDocument, IEnumerable<string>>>)(d => d.Outlines)));
            this._fields.Add((IFieldConfiguration)new ProviderFieldConfiguration<IEnumerable<string>>("catalognodes", (Expression<Func<FindDocument, IEnumerable<string>>>)(d => d.CatalogNodes)));
            foreach (MetaField entryUserMetaField in ProviderConfiguration.GetCatalogEntryUserMetaFields())
            {
                Type type;
                ProviderConfiguration._documentFieldTypes.TryGetValue(entryUserMetaField.DataType, out type);
                bool safeAllowSearch = entryUserMetaField.SafeAllowSearch;
                bool flag1 = string.Equals(entryUserMetaField.Attributes["includeindefaultsearch"], "true", StringComparison.OrdinalIgnoreCase);
                bool flag2 = string.Equals(entryUserMetaField.Attributes["indexsortable"], "true", StringComparison.OrdinalIgnoreCase);
                bool flag3 = string.Equals(entryUserMetaField.Attributes["indexstored"], "true", StringComparison.OrdinalIgnoreCase);
                if (type != (Type)null && safeAllowSearch | flag1 | flag2 | flag3)
                {
                    bool isIncludedInDefaultSearch = flag1 && (type == typeof(string) || type == typeof(int?) || type == typeof(IEnumerable<string>));
                    if (entryUserMetaField.MultiLanguageValue)
                        this._multilanguageFieldGenerators.Add(new ProviderConfiguration.MultilanguageFieldGenerator(entryUserMetaField.Name, type, isIncludedInDefaultSearch));
                    else
                        this._fields.Add(ProviderConfiguration.CreateUserField(entryUserMetaField.Name, (string)null, isIncludedInDefaultSearch, type));
                }
            }
        }

        private static IEnumerable<MetaField> GetCatalogEntryUserMetaFields()
        {
            List<MetaClass> source = new List<MetaClass>();
            source.Add(MetaClass.Load(CatalogContext.MetaDataContext, "CatalogEntry"));
            for (int index = 0; index < source.Count; ++index)
                source.AddRange(source[index].ChildClasses.Cast<MetaClass>());
            return (IEnumerable<MetaField>)source.SelectMany<MetaClass, MetaField>((Func<MetaClass, IEnumerable<MetaField>>)(mc => mc.GetUserMetaFields())).GroupBy<MetaField, int>((Func<MetaField, int>)(mf => mf.Id)).Select<IGrouping<int, MetaField>, MetaField>((Func<IGrouping<int, MetaField>, MetaField>)(mfg => mfg.First<MetaField>())).OrderBy<MetaField, string>((Func<MetaField, string>)(mf => mf.Name)).ToList<MetaField>();
        }

        private static IFieldConfiguration<double?> CreateListPriceField(Mediachase.Commerce.Currency currency, MarketId marketId)
        {
            string fieldKey = string.Format("{0}_{1}", (object)currency.CurrencyCode, (object)marketId.Value);
            return (IFieldConfiguration<double?>)new ProviderFieldConfiguration<double?>("listprice", (string)null, new Mediachase.Commerce.Currency?(currency), new MarketId?(marketId), 0 != 0, 0 != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<double?>((Expression<Func<FindDocument, double?>>)(d => d.ListPrices[fieldKey])), (Func<FindDocument, double?>)(d =>
            {
                double? nullable;
                if (!d.ListPrices.TryGetValue(fieldKey, out nullable))
                    return new double?();
                return nullable;
            }), (Action<FindDocument, double?>)((d, a) => d.ListPrices[fieldKey] = a));
        }

        private static IFieldConfiguration<double?> CreateSalePriceField(Mediachase.Commerce.Currency currency, MarketId marketId)
        {
            string fieldKey = string.Format("{0}_{1}", (object)currency.CurrencyCode, (object)marketId.Value);
            return (IFieldConfiguration<double?>)new ProviderFieldConfiguration<double?>("saleprice", (string)null, new Mediachase.Commerce.Currency?(currency), new MarketId?(marketId), 0 != 0, 0 != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<double?>((Expression<Func<FindDocument, double?>>)(d => d.SalePrices[fieldKey])), (Func<FindDocument, double?>)(d =>
            {
                double? nullable;
                if (!d.SalePrices.TryGetValue(fieldKey, out nullable))
                    return new double?();
                return nullable;
            }), (Action<FindDocument, double?>)((d, a) => d.SalePrices[fieldKey] = a));
        }

        private static IFieldConfiguration CreateUserField(string name, string locale, bool isIncludedInDefaultSearch, Type fieldType)
        {
            string fieldKey = string.Format("{0}_{1}", (object)name, locale == null ? (object)"nolang" : (object)locale).ToLowerInvariant();
            if (fieldType == typeof(bool?))
                return (IFieldConfiguration)new ProviderFieldConfiguration<bool?>(name, locale, new Mediachase.Commerce.Currency?(), new MarketId?(), 1 != 0, (isIncludedInDefaultSearch ? 1 : 0) != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<bool?>((Expression<Func<FindDocument, bool?>>)(d => d.BoolFields[fieldKey])), (Func<FindDocument, bool?>)(d =>
                {
                    bool? nullable;
                    if (!d.BoolFields.TryGetValue(fieldKey, out nullable))
                        return new bool?();
                    return nullable;
                }), (Action<FindDocument, bool?>)((d, v) => d.BoolFields[fieldKey] = v));
            if (fieldType == typeof(int?))
                return (IFieldConfiguration)new ProviderFieldConfiguration<int?>(name, locale, new Mediachase.Commerce.Currency?(), new MarketId?(), 1 != 0, (isIncludedInDefaultSearch ? 1 : 0) != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<int?>((Expression<Func<FindDocument, int?>>)(d => d.IntFields[fieldKey])), (Func<FindDocument, int?>)(d =>
                {
                    int? nullable;
                    if (!d.IntFields.TryGetValue(fieldKey, out nullable))
                        return new int?();
                    return nullable;
                }), (Action<FindDocument, int?>)((d, v) => d.IntFields[fieldKey] = v));
            if (fieldType == typeof(long?))
                return (IFieldConfiguration)new ProviderFieldConfiguration<long?>(name, locale, new Mediachase.Commerce.Currency?(), new MarketId?(), 1 != 0, (isIncludedInDefaultSearch ? 1 : 0) != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<long?>((Expression<Func<FindDocument, long?>>)(d => d.LongFields[fieldKey])), (Func<FindDocument, long?>)(d =>
                {
                    long? nullable;
                    if (!d.LongFields.TryGetValue(fieldKey, out nullable))
                        return new long?();
                    return nullable;
                }), (Action<FindDocument, long?>)((d, v) => d.LongFields[fieldKey] = v));
            if (fieldType == typeof(double?) || fieldType == typeof(float?))
                return (IFieldConfiguration)new ProviderFieldConfiguration<double?>(name, locale, new Mediachase.Commerce.Currency?(), new MarketId?(), 1 != 0, (isIncludedInDefaultSearch ? 1 : 0) != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<double?>((Expression<Func<FindDocument, double?>>)(d => d.DoubleFields[fieldKey])), (Func<FindDocument, double?>)(d =>
                {
                    double? nullable;
                    if (!d.DoubleFields.TryGetValue(fieldKey, out nullable))
                        return new double?();
                    return nullable;
                }), (Action<FindDocument, double?>)((d, v) => d.DoubleFields[fieldKey] = v));
            if (fieldType == typeof(DateTime?))
                return (IFieldConfiguration)new ProviderFieldConfiguration<DateTime?>(name, locale, new Mediachase.Commerce.Currency?(), new MarketId?(), 1 != 0, (isIncludedInDefaultSearch ? 1 : 0) != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<DateTime?>((Expression<Func<FindDocument, DateTime?>>)(d => d.DateTimeFields[fieldKey])), (Func<FindDocument, DateTime?>)(d =>
                {
                    DateTime? nullable;
                    if (!d.DateTimeFields.TryGetValue(fieldKey, out nullable))
                        return new DateTime?();
                    return nullable;
                }), (Action<FindDocument, DateTime?>)((d, v) => d.DateTimeFields[fieldKey] = v));
            if (fieldType == typeof(string))
                return (IFieldConfiguration)new ProviderFieldConfiguration<string>(name, locale, new Mediachase.Commerce.Currency?(), new MarketId?(), 1 != 0, (isIncludedInDefaultSearch ? 1 : 0) != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<string>((Expression<Func<FindDocument, string>>)(d => d.StringFields[fieldKey])), (Func<FindDocument, string>)(d =>
                {
                    string str;
                    if (!d.StringFields.TryGetValue(fieldKey, out str))
                        return (string)null;
                    return str;
                }), (Action<FindDocument, string>)((d, v) => d.StringFields[fieldKey] = v));
            if (!(fieldType == typeof(IEnumerable<string>)))
                throw new InvalidOperationException(string.Format("Cannot create dynamic field of type {0}.", (object)fieldType.Name));
            return (IFieldConfiguration)new ProviderFieldConfiguration<IEnumerable<string>>(name, locale, new Mediachase.Commerce.Currency?(), new MarketId?(), 1 != 0, (isIncludedInDefaultSearch ? 1 : 0) != 0, EPiServer.Commerce.FindSearchProvider.LocalImplementation.Expressions.CloseFieldAccessParameter<IEnumerable<string>>((Expression<Func<FindDocument, IEnumerable<string>>>)(d => d.StringCollectionFields[fieldKey])), (Func<FindDocument, IEnumerable<string>>)(d =>
            {
                List<string> stringList;
                if (!d.StringCollectionFields.TryGetValue(fieldKey, out stringList))
                    return (IEnumerable<string>)null;
                return (IEnumerable<string>)stringList;
            }), (Action<FindDocument, IEnumerable<string>>)((d, v) => d.StringCollectionFields[fieldKey] = v.ToList<string>()));
        }

        private string GetSettingWithFallback(string key, string fallbackKey, NameValueCollection fallbackConfiguration)
        {
            string appSetting = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(appSetting))
                return this.TakeConfigurationValue(fallbackConfiguration, fallbackKey, true);
            fallbackConfiguration.Remove(fallbackKey);
            return appSetting;
        }

        private string TakeConfigurationValue(NameValueCollection config, string key, bool isRequired)
        {
            string str = config[key];
            if (string.IsNullOrEmpty(str))
            {
                if (isRequired)
                    throw new ProviderException(string.Format("{0} is a required attribute for FindSearchProvider.", (object)key));
                str = (string)null;
            }
            config.Remove(key);
            return str;
        }

        private class MultilanguageFieldGenerator
        {
            public string Name { get; private set; }

            public Type Type { get; private set; }

            public bool IsIncludedInDefaultSearch { get; private set; }

            public MultilanguageFieldGenerator(string name, Type type, bool isIncludedInDefaultSearch)
            {
                this.Name = name;
                this.Type = type;
                this.IsIncludedInDefaultSearch = isIncludedInDefaultSearch;
            }

            public IFieldConfiguration Generate(string locale)
            {
                return ProviderConfiguration.CreateUserField(this.Name, locale, this.IsIncludedInDefaultSearch, this.Type);
            }
        }
    }
}
