using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace InformationSearch
{
    public class Lemmatizer : IDisposable
    {
        private readonly Regex _cleanRegex;
        private readonly ConsoleProcessWrapper _processWrapper;

        public Lemmatizer(string path)
        {
            _cleanRegex = new Regex(@"[^\w\d\p{P}]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _processWrapper = new ConsoleProcessWrapper(startInfo: CreateStartInfo(path));
        }

        public WordDefenition[] Lemmatize(string text)
        {
            var cleanText = _cleanRegex.Replace(text ?? "", " ");

            var result = _processWrapper.GetProcessOutput(cleanText);
            return JArray.Parse(result)
                .ToObject<List<WordDefenition>>()
                .Select(def => new WordDefenition(def.Text.Replace("\n", "").Replace(@"\s", ""), def.Analysis))
                .ToArray();
        }

        public IEnumerable<string> LemmatizeText(string text)
        {
            var lemmatized = Lemmatize(text);

            return lemmatized.Select(x => x.GetText());
        }

        public List<string> LemmatizeWordsList(IEnumerable<string> words)
        {
            var lemmatizedWords = new List<string>();
            foreach (var word in words)
            {
                var lemmatizedWord = GetLemma(Lemmatize(word));
                if (!string.IsNullOrEmpty(lemmatizedWord))
                {
                    lemmatizedWords.Add(lemmatizedWord);
                }
            }

            return lemmatizedWords;
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

        private ProcessStartInfo CreateStartInfo(string path)
        {
            return new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = path,
                Arguments = "-cdis --format json"
            };
        }

        public void Dispose()
        {
            _processWrapper?.Dispose();
        }
    }
}
