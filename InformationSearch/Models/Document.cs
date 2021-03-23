using System;
using System.Collections.Generic;
using System.Text;

namespace InformationSearch.Models
{
    public class Document
    {
        public string Url { get; set; }

        public string Text { get; set; }

        public Document(string url, string text)
        {
            Url = url;
            Text = text;
        }
	}
}
