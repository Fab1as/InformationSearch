using System;
using System.IO;
using System.Linq;

namespace InformationSearch
{
    class Program
    {
        public static string BaseUrl => "https://ria.ru/";
        public static int PagesCount => 100;
        private static string Task1Path => @"C:\Projects\InformationSearch\Task1";

        static void Main(string[] args)
        {
            ProcessPages();
        }

        private static void ProcessPages()
        {
            var parser = new PageParser(BaseUrl, PagesCount);
            var links = parser.GetLinks();
            var documents = parser.ParsePages(links).ToList();
            using (var indexWriter = new StreamWriter($"{Task1Path}\\index.txt", true))
            {
                var index = 1;
                foreach (var document in documents)
                {
                    indexWriter.WriteLine($"{index}) {document.Url}");
                    File.WriteAllText($"{Task1Path}\\pages\\page_{index++}.txt", document.Text);
                }
            }
        }
	}
}
