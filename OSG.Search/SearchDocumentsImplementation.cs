using EPiServer.Find;
using EPiServer.Find.Api;
using Mediachase.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.FindSearchProvider;

namespace OSG.Search
{
    internal class SearchDocumentsImplementation : ISearchDocuments, IList<ISearchDocument>, ICollection<ISearchDocument>, IEnumerable<ISearchDocument>, IEnumerable
    {
        private List<ISearchDocument> _documents;

        public int TotalCount { get; set; }

        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ISearchDocument this[int index]
        {
            get
            {
                return this._documents[index];
            }
            set
            {
                this._documents[index] = value;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public SearchDocumentsImplementation(ISearchConfiguration config, string locale, SearchResults<FindDocument> results)
        {
            this.TotalCount = results.TotalMatching;
            this._documents = results.Hits.Select<SearchHit<FindDocument>, ISearchDocument>((Func<SearchHit<FindDocument>, ISearchDocument>)(hit => (ISearchDocument)new SearchDocumentImplementation(config, locale, hit))).ToList<ISearchDocument>();
        }

        public int IndexOf(ISearchDocument item)
        {
            return this._documents.IndexOf(item);
        }

        public void Insert(int index, ISearchDocument item)
        {
            this._documents.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this._documents.RemoveAt(index);
        }

        public void Add(ISearchDocument item)
        {
            this._documents.Add(item);
        }

        public void Clear()
        {
            this._documents.Clear();
        }

        public bool Contains(ISearchDocument item)
        {
            return this._documents.Contains(item);
        }

        public void CopyTo(ISearchDocument[] array, int arrayIndex)
        {
            this._documents.CopyTo(array, arrayIndex);
        }

        public bool Remove(ISearchDocument item)
        {
            return this._documents.Remove(item);
        }

        public IEnumerator<ISearchDocument> GetEnumerator()
        {
            return (IEnumerator<ISearchDocument>)this._documents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this._documents.GetEnumerator();
        }
    }
}
