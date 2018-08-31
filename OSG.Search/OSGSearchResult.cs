using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Search;

namespace OSG.Search
{
    public class OSGSearchResult : SearchResults
    {
        public OSGSearchResult(string initData)
        {
            PopulateDocuments(initData);
        }

        private void PopulateDocuments(string initData)
        {
            var searchDocuments = new SearchDocuments();
            foreach (var t in initData)
            {
                var searchDocument = new SearchDocument();
                var fieldTitle = new SearchField("Title", $"Tieu de {t}");
                searchDocument.Add(fieldTitle);
                var fieldContent = new SearchField("Content", $"Noi dung {t}");
                searchDocument.Add(fieldContent);
                searchDocuments.Add((ISearchDocument)searchDocument);
            }

            searchDocuments.TotalCount = initData.Length;
            this.Documents = (ISearchDocuments)searchDocuments;
        }
    }
}
