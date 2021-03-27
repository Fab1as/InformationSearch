using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private static string Task3Path => $@"{RootPath}\Task3";
        private static Lemmatizer Lemmatizer;

        static void Main(string[] args)
        {
            //ProcessPages();
            //Lemmatize(new Lemmatizer(new StemDownloader().GetLocalPath()));
            File.WriteAllLines($"{Task3Path}\\searchResult.txt", Search("арестовали появится заблокированы").Select(x => x.ToString()));
        }

        private static List<int> Search(string query)
        {
            var stopwords = File.ReadAllLines(StopWordsPath);
            var words = query.Split(" ").Where(x => x != "" && !stopwords.Contains(x)).ToList();
            var excludeWords = new HashSet<string>();
            var includeWords = new HashSet<string>();
            foreach (var word in words)
            {
                if (word.StartsWith('-'))
                {
                    excludeWords.Add(word.Substring(1));
                }
                else
                {
                    includeWords.Add(word);
                }
            }

            var lemmatizer = new Lemmatizer(new StemDownloader().GetLocalPath());
            var excludeLemmas = new HashSet<string>();
            foreach (var excludeWord in excludeWords)
            {
                var res = lemmatizer.Lemmatize(excludeWord);
                var lemma = GetLemma(res);
                if (lemma != null)
                {
                    excludeLemmas.Add(lemma);
                }
            }
            var includeLemmas = new HashSet<string>();
            foreach (var includeWord in includeWords)
            {
                var res = lemmatizer.Lemmatize(includeWord);
                var lemma = GetLemma(res);
                if (lemma != null)
                {
                    includeLemmas.Add(lemma);
                }
            }

            if (!includeLemmas.Any() && !excludeLemmas.Any())
            {
                return new List<int>();
            }

            var lemmasWithIndexes = File.ReadAllLines($"{Task3Path}\\invertedIndex.txt");
            var lemmaToIndexesDict = new Dictionary<string, HashSet<int>>();
            foreach (var lemmaWithIndexes in lemmasWithIndexes)
            {
                var parsedString = lemmaWithIndexes.Split(' ');
                lemmaToIndexesDict.Add(parsedString[0], new HashSet<int>(parsedString.Skip(1).Select(x => int.Parse(x))));
            }

            if (includeLemmas.Any())
            {
                var result = lemmaToIndexesDict[includeLemmas.FirstOrDefault()];
                var firstSkippedIncludeLemmas = includeLemmas.Skip(1);
                foreach (var includeLemma in firstSkippedIncludeLemmas)
                {
                    result.IntersectWith(lemmaToIndexesDict[includeLemma]);
                }

                return result.ToList();
            }

            if (excludeLemmas.Any())
            {
                var result = lemmaToIndexesDict[excludeLemmas.FirstOrDefault()];
                var firstSkippedExcludeLemmas = excludeLemmas.Skip(1);
                foreach (var excludeLemma in firstSkippedExcludeLemmas)
                {
                    result.IntersectWith(lemmaToIndexesDict[excludeLemma]);
                }

                return result.ToList();
            }

            return new List<int>();
        }

        private static string GetLemma(WordDefenition[] definitions)
        {
            foreach (var analyze in definitions)
            {
                foreach (var analys in analyze.Analysis)
                {
                    if (!string.IsNullOrEmpty(analys.Lex))
                    {
                        return analys.Lex;
                    }
                }
            }

            return null;
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

            var words = allWords.Select(x => x.ToLowerInvariant()).ToHashSet();
            var lemmaToWordsDict = new Dictionary<string, HashSet<string>>();

            using (var wordsWriter = new StreamWriter($"{Task2Path}\\words.txt", true))
            {
                foreach (var word in words)
                {
                    wordsWriter.WriteLine(word);
                    var res = lemmatizer.Lemmatize(word);
                    foreach (var analyze in res)
                    {
                        foreach (var analys in analyze.Analysis)
                        {
                            if (!string.IsNullOrEmpty(analys.Lex) && stopWords.All(x => x != analys.Lex))
                            {
                                if (lemmaToWordsDict.ContainsKey(analys.Lex))
                                {
                                    lemmaToWordsDict[analys.Lex].Add(word);
                                }
                                else
                                {
                                    lemmaToWordsDict.Add(analys.Lex, new HashSet<string>() { word });
                                }
                            }
                        }
                    }
                }
            }

            using (var lemmasWriter = new StreamWriter($"{Task2Path}\\lemmas.txt", true))
            {
                foreach (var (lemma, wordForms) in lemmaToWordsDict)
                {
                    lemmasWriter.WriteLine($"{lemma} {string.Join(" ", wordForms)}");
                }
            }

            CreateInvertedIndex(lemmaToWordsDict);
        }

        private static void CreateInvertedIndex(Dictionary<string, HashSet<string>> lemmaToWordsDict)
        {
            var sortedDict = lemmaToWordsDict.ToImmutableSortedDictionary();
            var invertedIndexDict = sortedDict.ToDictionary(x => x.Key, x => new List<int>());
            for (int i = 0; i < PagesCount; i++)
            {
                var pageText = File.ReadAllText($@"{Task1Path}\pages\page_{i + 1}.txt").ToLowerInvariant();
                foreach (var (lemma, words) in sortedDict)
                {
                    if (words.Any(x => pageText.Contains(x)))
                    {
                        if (invertedIndexDict.ContainsKey(lemma))
                        {
                            invertedIndexDict[lemma].Add(i);
                        }
                        else
                        {
                            invertedIndexDict.Add(lemma, new List<int> { i });
                        }
                    }
                }
            }

            using (var indexWriter = new StreamWriter($"{Task3Path}\\invertedIndex.txt", true))
            {
                foreach (var (lemma, docNumbers) in invertedIndexDict)
                {
                    if (docNumbers.Any())
                    {
                        indexWriter.WriteLine($"{lemma} {string.Join(" ", docNumbers)}");
                    }
                }
            }
        }
    }
}
