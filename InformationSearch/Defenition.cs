using System;
using System.Collections.Generic;
using System.Text;

namespace InformationSearch
{
    public struct Defenition
    {
        public string Gr { get; }
        public string Lex { get; }

        public Defenition(string gr, string lex)
        {
            Gr = gr;
            Lex = lex;
        }
    }
}
