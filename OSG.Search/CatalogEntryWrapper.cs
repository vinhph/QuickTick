using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Search;

namespace OSG.Search
{
    internal class CatalogEntryWrapper : ISearchDocument
    {
        private List<string> _languages;

        public CatalogEntryDto.CatalogEntryRow EntryRow { get; private set; }

        public Mediachase.Commerce.Currency DefaultCurrency { get; private set; }

        public IEnumerable<string> Languages
        {
            get
            {
                return (IEnumerable<string>)this._languages;
            }
        }

        public CatalogEntryWrapper(CatalogEntryDto.CatalogEntryRow entryRow, Mediachase.Commerce.Currency defaultCurrency, IEnumerable<string> languages)
        {
            this.EntryRow = entryRow;
            this.DefaultCurrency = defaultCurrency;
            this._languages = languages.Select<string, string>((Func<string, string>)(l => l.ToLowerInvariant())).Distinct<string>().ToList<string>();
        }

        public int FieldCount
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public ISearchField this[int index]
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public ISearchField this[string name]
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public void Add(ISearchField field)
        {
            throw new NotSupportedException();
        }

        public void RemoveField(string name)
        {
            throw new NotSupportedException();
        }
    }
}
