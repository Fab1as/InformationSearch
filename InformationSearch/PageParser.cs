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
        private readonly List<string> Links = new List<string>
        {
            @"https://ria.ru/world/",
            @"https://ria.ru/economy/",
            @"https://ria.ru/society/",
            @"https://ria.ru/incidents/",
            @"https://ria.ru/defense_safety/",
            @"https://ria.ru/science/",
            @"https://ria.ru/culture/",
            @"https://ria.ru/religion/",
            @"https://ria.ru/politics/",
            @"https://ria.ru/tourism/"
        };

        public PageParser(string baseUrl, int pagesCount)
        {
            _baseUrl = baseUrl;
            _pagesCount = pagesCount;
        }

        public IEnumerable<string> GetLinks()
        {
            var hashSet = new HashSet<string>(_pagesCount);

            foreach (var link in Links)
            {
                var document = new HtmlWeb().Load(link);

                var linkedPages = document.DocumentNode.Descendants("a")
                    .Select(node => node.GetAttributeValue("href", null))
                    .Where(x => !string.IsNullOrEmpty(x) && x.StartsWith(_baseUrl) && x.EndsWith(".html"));

                foreach (var page in linkedPages)
                {
                    if (!hashSet.Contains(page))
                    {
                        var downloadedPage = new HtmlWeb().Load(page);
                        if (downloadedPage.DocumentNode.SelectNodes("//div[@class='article__text']") != null)
                        {
                            hashSet.Add(page);

                            if (hashSet.Count == _pagesCount)
                            {
                                break;
                            }
                        }
                    }
                }
                if (hashSet.Count == _pagesCount)
                {
                    break;
                }
            }

            //while (hashSet.Count < _pagesCount)
            //{
            //    var document = new HtmlWeb().Load(hashSet.Last());

            //    var linkedPages = document.DocumentNode.Descendants("a")
            //        .Select(node => node.GetAttributeValue("href", null))
            //        .Where(link => !string.IsNullOrEmpty(link) && link.StartsWith(_baseUrl));

            //    foreach (var page in linkedPages)
            //    {
            //        if (!hashSet.Contains(page))
            //        {
            //            var downloadedPage = new HtmlWeb().Load(page);
            //            if (downloadedPage.DocumentNode.SelectNodes("//div[@class='article__text']") != null)
            //            {
            //                hashSet.Add(page);

            //                if (hashSet.Count == _pagesCount)
            //                {
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}

            return hashSet;
        }

        public IEnumerable<Document> ParsePages(IEnumerable<string> links)
        {
            var documents = new ConcurrentBag<Document>();
            var index = 0;

            foreach (var link in links)
            {
                var document = new HtmlWeb().LoadFromWebAsync(link, Encoding.UTF8).Result;
                var s = document.DocumentNode.SelectNodes("//div[@class='article__text']");
                var text = string.Join(' ', document.DocumentNode.SelectNodes("//div[@class='article__text']").Select(x => x.InnerText));
                text = Regex.Replace(text, @"\s+", " ").Trim();

                Interlocked.Increment(ref index);
                documents.Add(new Document(link, text));
            }

            return documents;
         }
    }
}
