using EPiServer.Find.Api;
using Mediachase.Commerce;
using Mediachase.Search;
using Mediachase.Search.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.FindSearchProvider;

namespace OSG.Search
{
    internal class SearchDocumentImplementation : ISearchDocument
    {
        private List<ISearchField> _fields;

        public int FieldCount
        {
            get
            {
                return this._fields.Count;
            }
        }

        public ISearchField this[int index]
        {
            get
            {
                return this._fields[index];
            }
        }

        public ISearchField this[string name]
        {
            get
            {
                return this._fields.SingleOrDefault<ISearchField>((Func<ISearchField, bool>)(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)));
            }
        }

        public SearchDocumentImplementation()
        {
            this._fields = new List<ISearchField>();
        }

        public SearchDocumentImplementation(ISearchConfiguration config, string locale, SearchHit<FindDocument> hit)
        {
            this._fields = new List<ISearchField>();
            this._fields.Add((ISearchField)new SearchField("_id", (object)hit.Document.CatalogEntryId));
            foreach (string language in hit.Document.Languages)
                config.AddLocale(language);
            foreach (string currency in hit.Document.Currencies)
                config.AddCurrency((Mediachase.Commerce.Currency)currency);
            foreach (string market in hit.Document.Markets)
                config.AddMarket((MarketId)market);
            foreach (IFieldConfiguration allField in config.GetAllFields(locale))
            {
                object obj = allField.GetObjectValue(hit.Document);
                IEnumerable<string> source = obj as IEnumerable<string>;
                if (source != null)
                {
                    string[] array = source.Where<string>((Func<string, bool>)(v => v != null)).ToArray<string>();
                    obj = array.Length != 0 ? (object)array : (object)(string[])null;
                }
                if (obj != null)
                {
                    if (allField.Currency.HasValue)
                        this._fields.Add((ISearchField)new SearchField(string.Format("{0}{1}_{2}", (object)allField.Name, (object)allField.Currency.Value.CurrencyCode, (object)allField.MarketId.Value), obj));
                    else
                        this._fields.Add((ISearchField)new SearchField(allField.Name, obj));
                }
            }
        }

        public void Add(ISearchField field)
        {
            this._fields.Add(field);
        }

        public void RemoveField(string name)
        {
            this._fields.RemoveAll((Predicate<ISearchField>)(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
