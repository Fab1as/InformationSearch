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
        private static string Task1Path => $@"{RootPath}\Task1";
        private static string Task2Path => $@"{RootPath}\Task2";
        private static string Task3Path => $@"{RootPath}\Task3";
        private static string Task4Path => $@"{RootPath}\Task4";
        private static string Task5Path => $@"{RootPath}\Task5";
        private static Lemmatizer Lemmatizer;

        static void Main(string[] args)
        {
            //ProcessPages();
            //Lemmatize(new Lemmatizer(new StemDownloader().GetLocalPath()));
            //File.WriteAllLines($"{Task3Path}\\searchResult.txt", Search("арестовали появится заблокированы").Select(x => x.ToString()));
            //CreateTfIdf();
            //var result = BooleanSearch("брифинг аналогичной безопасности");
            //foreach (var link in result)
            //{
            //    Console.WriteLine(link);
            //}
            //LemmatizeAllPagesAndWrite();
            var resultLinks = VectorSearch("устанавливать приложения других производителей");
            foreach (var link in resultLinks)
            {
                Console.WriteLine(link);
            }
        }

        private static IEnumerable<string> VectorSearch(string query)
        {
            var queryWordToFrequencyDict = new Dictionary<string, int>();
            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLowerInvariant()).ToList();
            var queryWordsSet = queryWords.ToImmutableSortedSet();
            foreach (var queryWord in queryWordsSet)
            {
                queryWordToFrequencyDict.Add(queryWord, queryWords.Count(x => string.Equals(queryWord, x, StringComparison.InvariantCultureIgnoreCase)));
            }

            var queryVector = new Dictionary<string, double>();
            var lemmaToIndexesDict = GetLemmasWithIndexes();
            var lemmatizer = new Lemmatizer(new StemDownloader().GetLocalPath());
            var maxFrequency = queryWordToFrequencyDict.Max(x => x.Value);
            double queryLength = 0;
            foreach (var (queryWord, frequency) in queryWordToFrequencyDict)
            {
                var lemmatizedQueryWord = GetLemma(lemmatizer.Lemmatize(queryWord));
                if (lemmatizedQueryWord != null && lemmaToIndexesDict.ContainsKey(lemmatizedQueryWord))
                {
                    var vectorElement = ((double)frequency / maxFrequency) * Math.Log10((double)PagesCount / lemmaToIndexesDict[lemmatizedQueryWord].Count);
                    queryVector.Add(lemmatizedQueryWord, vectorElement);
                    queryLength += vectorElement * vectorElement;
                }
            }

            queryLength = Math.Sqrt(queryLength);

            var documentsLength = new List<double>();
            var pageLemmasToTfIdfDictList = new List<Dictionary<string, double>>();
            for (int i = 0; i < PagesCount; i++)
            {
                var text = File.ReadAllLines($"{Task4Path}\\page_{i + 1}_tfidf.txt");
                var squaredDocumentLength = 0.0;
                pageLemmasToTfIdfDictList.Add(new Dictionary<string, double>());
                foreach (var lemmaWithTfIdfString in text)
                {
                    var wordWithTfIdf = lemmaWithTfIdfString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var word = wordWithTfIdf[0];
                    var tfIdfString = wordWithTfIdf[3];
                    var tfIdf = double.Parse(tfIdfString.Substring(7));
                    squaredDocumentLength += tfIdf * tfIdf;
                    pageLemmasToTfIdfDictList[i].Add(word, tfIdf);
                }
                documentsLength.Add(Math.Sqrt(squaredDocumentLength));
            }

            if (queryLength == 0) return File.ReadAllLines($"{Task1Path}\\index.txt");

            var documentCosineSimilarities = new List<(int pageIndex, double cosineSimilarity)>();
            for (int i = 0; i < PagesCount; i++)
            {
                double cosineSimilarity = 0;
                foreach (var (lemmatizedQueryWord, vectorElementValue) in queryVector)
                {
                    if (pageLemmasToTfIdfDictList[i].ContainsKey(lemmatizedQueryWord))
                    {
                        cosineSimilarity += vectorElementValue * pageLemmasToTfIdfDictList[i][lemmatizedQueryWord];
                    }
                }
                documentCosineSimilarities.Add((i, cosineSimilarity / (queryLength * documentsLength[i])));
            }

            documentCosineSimilarities = documentCosineSimilarities.OrderByDescending(x => x.cosineSimilarity).ToList();
            var resultLinks = new List<string>();
            var links = File.ReadAllLines($"{Task1Path}\\index.txt");
            foreach (var documentCosineSimilarity in documentCosineSimilarities)
            {
                resultLinks.Add(links[documentCosineSimilarity.pageIndex]);
            }

            return resultLinks;
        }

        private static void LemmatizeAllPagesAndWrite()
        {
            var lemmatizer = new Lemmatizer(new StemDownloader().GetLocalPath());
            for (int i = 0; i < PagesCount; i++)
            {
                var text = File.ReadAllText($"{Task1Path}\\pages\\page_{i + 1}.txt");
                var words = ParseTextToWordsInLower(text);
                var lemmatizedWords = lemmatizer.LemmatizeWordsList(words);
                lemmatizedWords.Sort();
                File.WriteAllLines($"{Task5Path}\\sortedLemmatizedPages\\page_{i + 1}.txt", lemmatizedWords);
            }
        }

        private static Dictionary<string, List<int>> GetLemmasWithIndexes()
        {
            var lemmasWithIndexes = File.ReadAllLines($"{Task3Path}\\invertedIndex.txt");
            var lemmaToIndexesDict = new Dictionary<string, List<int>>();
            foreach (var lemmaWithIndexesString in lemmasWithIndexes)
            {
                var lemmaIndexes = lemmaWithIndexesString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                lemmaToIndexesDict.Add(lemmaIndexes[0], new List<int>(lemmaIndexes.Skip(1).Select(int.Parse)));
            }

            return lemmaToIndexesDict;
        }

        private static void CreateTfIdf()
        {
            var lemmaToIndexesDict = GetLemmasWithIndexes();

            var lemmatizer = new Lemmatizer(new StemDownloader().GetLocalPath());
            var texts = new List<List<string>>(100);
            for (int i = 0; i < PagesCount; i++)
            {
                var fileText = File.ReadAllText($"{Task1Path}\\pages\\page_{i + 1}.txt");
                var words = ParseTextToWordsInLower(fileText);
                var lemmatizedWords = lemmatizer.LemmatizeWordsList(words);
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
                        writer.WriteLine($"{lemma} tf={tf:0.#######} idf={idf:0.#######} tf-idf={tf * idf:0.#######}");
                    }
                }
            }
        }

        private static List<string> BooleanSearch(string query)
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


            var lemmasWithIndexes = File.ReadAllLines($"{Task3Path}\\invertedIndex.txt");
            var lemmaToIndexesDict = new Dictionary<string, HashSet<int>>();
            foreach (var lemmaWithIndexes in lemmasWithIndexes)
            {
                var parsedString = lemmaWithIndexes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                lemmaToIndexesDict.Add(parsedString[0], new HashSet<int>(parsedString.Skip(1).Select(int.Parse)));
            }

            var lemmatizer = new Lemmatizer(new StemDownloader().GetLocalPath());
            var excludeLemmas = lemmatizer.LemmatizeWordsList(excludeWords).Select(x => x.ToLowerInvariant()).Where(x => lemmaToIndexesDict.ContainsKey(x)).ToList();
            var includeLemmas = lemmatizer.LemmatizeWordsList(includeWords).Select(x => x.ToLowerInvariant()).Where(x => lemmaToIndexesDict.ContainsKey(x)).ToList();

            if (!includeLemmas.Any() && !excludeLemmas.Any())
            {
                return new List<string>();
            }

            var documentIndexesSet = new HashSet<int>();
            if (includeLemmas.Any())
            {
                var firstIncludeLemma = includeLemmas.FirstOrDefault();
                documentIndexesSet.UnionWith(lemmaToIndexesDict[firstIncludeLemma!]);;
                foreach (var includeLemma in includeLemmas.Skip(1))
                {
                    documentIndexesSet.IntersectWith(lemmaToIndexesDict[includeLemma]);
                }
            }
            if (excludeLemmas.Any())
            {
                foreach (var excludeLemma in excludeLemmas)
                {
                    documentIndexesSet.ExceptWith(lemmaToIndexesDict[excludeLemma]);
                }
            }

            var resultLinks = new List<string>();
            var links = File.ReadAllLines($"{Task1Path}\\index.txt");
            foreach (var index in documentIndexesSet)
            {
                resultLinks.Add(links[index]);
            }

            return resultLinks;
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
