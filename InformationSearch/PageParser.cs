using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Document = InformationSearch.Models.Document;

namespace InformationSearch
{
    public class PageParser
    {
        private readonly string _baseUrl;
        private readonly int _pagesCount;

        public PageParser(string baseUrl, int pagesCount)
        {
            _baseUrl = baseUrl;
            _pagesCount = pagesCount;
        }

        public IEnumerable<string> GetLinks()
        {
            var hashSet = new HashSet<string>(_pagesCount) { _baseUrl };

            while (hashSet.Count < _pagesCount)
            {
                var document = new HtmlWeb().Load(hashSet.Last());
                var linkedPages = document.DocumentNode.Descendants("a")
                    .Select(node => node.GetAttributeValue("href", null))
                    .Where(link => !string.IsNullOrEmpty(link) && link.StartsWith(_baseUrl));

                foreach (var page in linkedPages)
                {
                    if (!hashSet.Contains(page))
                    {
                        hashSet.Add(page);

                        if (hashSet.Count == _pagesCount)
                        {
                            break;
                        }
                    }
                }
            }

            return hashSet;
        }

        public IEnumerable<Document> ParsePages(IEnumerable<string> links)
        {
            var documents = new ConcurrentBag<Document>();
            var index = 0;

            foreach (var link in links)
            {
                var document = new HtmlWeb().LoadFromWebAsync(link, Encoding.UTF8).Result;

                var text = document.DocumentNode.InnerText;
                text = Regex.Replace(text, @"\s+", " ").Trim();

                Interlocked.Increment(ref index);
                documents.Add(new Document(link, text));
            }

            return documents;
        }
    }
}
