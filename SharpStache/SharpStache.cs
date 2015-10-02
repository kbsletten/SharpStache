using System.Collections.Generic;
using System.IO;

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
            return Mustache.RenderValue(template, partials, new LegacyValue(value));
        }
    }
}
