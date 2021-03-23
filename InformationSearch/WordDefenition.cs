using System;
using System.Collections.Generic;
using System.Text;

namespace InformationSearch
{
    public class WordDefenition
    {
        private readonly Defenition[] _analysis;
        public string Text { get; }

        public Defenition[] Analysis => _analysis ?? new Defenition[0];

        public WordDefenition(string text, Defenition[] analysis)
        {
            Text = text;
            _analysis = analysis;
        }

        public string GetText()
        {
            if (Analysis.Length == 0)
                return Text;
            return Analysis[0].Lex;
        }
    }
}
