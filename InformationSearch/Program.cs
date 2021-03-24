using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InformationSearch
{
    class Program
    {
        public static string BaseUrl => "https://ria.ru/";
        public static int PagesCount => 100;
        private static string RootPath = @"C:\Projects\InformationSearch";
        private static string StopWordsPath = $@"{RootPath}\InformationSearch\StopWords.txt";
        private static string Task1Path => $@"{RootPath}\Task1";
        private static string Task2Path => $@"{RootPath}\Task2";
        private static Lemmatizer Lemmatizer;

        static void Main(string[] args)
        {
            //ProcessPages();
            Lemmatize(new Lemmatizer(new StemDownloader().GetLocalPath()));
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

        private static void Lemmatize(Lemmatizer lemmatizer)
        {
            var allWords = new List<string>();
            for (int i = 1; i <= PagesCount; i++)
            {
                var fileText = File.ReadAllText($"{Task1Path}\\pages\\page_{i}.txt");
                var sb = new StringBuilder();
                foreach (char c in fileText)
                {
                    if (!char.IsPunctuation(c))
                        sb.Append(c);
                    else
                        sb.Append(" ");
                }
                allWords.AddRange(sb.ToString().Split(' '));
            }
            var stopWords = File.ReadAllLines(StopWordsPath);

            var words = allWords.ToHashSet();

            using (var wordsWriter = new StreamWriter($"{Task2Path}\\words.txt", true))
            {
                using (var lemmasWriter = new StreamWriter($"{Task2Path}\\lemmas.txt", true))
                {
                    foreach (var word in words)
                    {
                        wordsWriter.WriteLine(word);
                        var res = lemmatizer.Lemmatize(word);
                        var lemmas = new List<string>();
                        foreach (var analyze in res)
                        {
                            foreach (var analys in analyze.Analysis)
                            {
                                if (!string.IsNullOrEmpty(analys.Lex) && stopWords.All(x => x != analys.Lex))
                                {
                                    lemmas.Add(analys.Lex);
                                }
                            }
                        }
                        lemmasWriter.WriteLine($"{word} {string.Join(" ", lemmas.Distinct())}");
                    }
                }
            }
        }
	}
}
