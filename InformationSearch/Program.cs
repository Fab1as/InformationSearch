using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace InformationSearch
{
    class Program
    {
        public static string BaseUrl => "https://ria.ru/";
        public static int PagesCount => 100;
        private static string RootPath = @"C:\Projects\InformationSearch";
        private static string StopWordsPath = $@"{RootPath}\InformationSearch\StopWords.txt";
        private static string Task1Path => $@"{RootPath}\Task1Updated";
        private static string Task2Path => $@"{RootPath}\Task2Updated";
        private static string Task3Path => $@"{RootPath}\Task3Updated";
        private static string Task4Path => $@"{RootPath}\Task4Updated";
        private static Lemmatizer Lemmatizer;

        static void Main(string[] args)
        {
            //ProcessPages();
            //Lemmatize(new Lemmatizer(new StemDownloader().GetLocalPath()));
            //File.WriteAllLines($"{Task3Path}\\searchResult.txt", Search("арестовали появится заблокированы").Select(x => x.ToString()));
            CreateTfIdf();
        }

        private static void CreateTfIdf()
        {
            var lemmasWithIndexes = File.ReadAllLines($"{Task3Path}\\invertedIndex.txt");
            var lemmaToIndexesDict = new Dictionary<string, List<int>>();
            foreach (var lemmaWithIndexesString in lemmasWithIndexes)
            {
                var lemmaIndexes = lemmaWithIndexesString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                lemmaToIndexesDict.Add(lemmaIndexes[0], new List<int>(lemmaIndexes.Skip(1).Select(int.Parse)));
            }

            var lemmatizer = new Lemmatizer(new StemDownloader().GetLocalPath());
            var texts = new List<List<string>>(100);
            for (int i = 0; i < PagesCount; i++)
            {
                var fileText = File.ReadAllText($"{Task1Path}\\pages\\page_{i + 1}.txt");
                var words = ParseTextToWordsInLower(fileText);
                var lemmatizedWords = new List<string>();
                foreach (var word in words)
                {
                    var res = lemmatizer.Lemmatize(word);
                    var lemma = GetLemma(res);
                    if (lemma != null)
                    {
                        lemmatizedWords.Add(lemma);
                    }
                }
                texts.Add(lemmatizedWords);
            }

            foreach (var (lemma, indexes) in lemmaToIndexesDict)
            {
                var idf = Math.Log10(Convert.ToDouble(PagesCount) / indexes.Count);
                foreach (var index in indexes)
                {
                    using (var writer = File.AppendText($"{Task4Path}\\page_{index + 1}_tfidf.txt"))
                    {
                        var text = texts[index];
                        var wordEntriesCount = 0;
                        foreach (var word in text)
                        {
                            if (word == lemma)
                            {
                                wordEntriesCount++;
                            }
                        }
                        var tf = Convert.ToDouble(wordEntriesCount) / text.Count;
                        writer.WriteLine($"{lemma} tf={tf:0.#######} idf={idf:0.#######} tf-idf={tf*idf:0.#######}");
                    }
                }
            }
        }

        private static List<int> Search(string query)
        {
            var stopwords = File.ReadAllLines(StopWordsPath);
            var words = query.Split(" ", StringSplitOptions.RemoveEmptyEntries).Where(x => x != "" && !stopwords.Contains(x)).ToList();
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
                var parsedString = lemmaWithIndexes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
            using (var indexWriter = File.AppendText($"{Task1Path}\\index.txt"))
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
                allWords.AddRange(ParseTextToWordsInLower(fileText));
            }
            var stopWords = File.ReadAllLines(StopWordsPath);
            var words = allWords.Where(x => !stopWords.Contains(x)).ToHashSet();
            var lemmaToWordsDict = new Dictionary<string, HashSet<string>>();

            using (var wordsWriter = new StreamWriter($"{Task2Path}\\words.txt", true))
            {
                foreach (var word in words)
                {
                    var lemmatizeResult = lemmatizer.Lemmatize(word);
                    var lemma = GetLemma(lemmatizeResult);
                    if (!string.IsNullOrEmpty(lemma))
                    {
                        wordsWriter.WriteLine(word);
                        if (lemmaToWordsDict.ContainsKey(lemma))
                        {
                            lemmaToWordsDict[lemma].Add(word);
                        }
                        else
                        {
                            lemmaToWordsDict.Add(lemma, new HashSet<string> { word });
                        }
                    }
                }
            }

            using (var lemmasWriter = new StreamWriter($"{Task2Path}\\lemmas.txt", true))
            {
                foreach (var (lemma, wordForms) in lemmaToWordsDict)
                    lemmasWriter.WriteLine($"{lemma} {string.Join(" ", wordForms)}");
            }

            CreateInvertedIndex(lemmaToWordsDict);
        }

        private static IEnumerable<string> ParseTextToWordsInLower(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if ('а' <= c && c <= 'я' || 'А' <= c && c <= 'Я')
                    sb.Append(char.ToLowerInvariant(c));
                else
                    sb.Append(" ");
            }

            return sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        private static void CreateInvertedIndex(Dictionary<string, HashSet<string>> lemmaToWordsDict)
        {
            var sortedDict = lemmaToWordsDict.ToImmutableSortedDictionary();
            var invertedIndexDict = sortedDict.ToDictionary(x => x.Key, x => new List<int>());
            var lemmatizer = new Lemmatizer(new StemDownloader().GetLocalPath());
            for (int i = 0; i < PagesCount; i++)
            {
                var pageText = File.ReadAllText($@"{Task1Path}\pages\page_{i + 1}.txt");
                var pageWords = ParseTextToWordsInLower(pageText);
                var lemmatizedTextWords = lemmatizer.LemmatizeWordsList(pageWords);
                foreach (var (lemma, words) in sortedDict)
                {
                    if (lemmatizedTextWords.Any(x => string.Equals(lemma, x, StringComparison.InvariantCultureIgnoreCase)))
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
