using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpStache
{
    /// <summary>
    /// A <see cref="http://mustache.github.io">Mustache</see> template renderer for when style matters
    /// </summary>
    public static class SharpStache
    {
        private static readonly IDictionary<string, string> Empty = new Dictionary<string, string>();

        public static string Render(string template, object value)
        {
            return Render(template, Empty, value);
        }

        public static string Render(string template, IDictionary<string, string> partials, object value)
        {
            var templates = Parser.GetTemplates(template);
            var builder = new StringBuilder();
            var values = new Stack();
            values.Push(value);
            foreach (var temp in templates)
            {
                temp.Render(builder, partials, values);
            }
            return builder.ToString();
        }
    }
}
