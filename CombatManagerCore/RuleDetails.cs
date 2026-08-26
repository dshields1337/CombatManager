using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace CombatManager
{
    public class RuleDetails
    {
        public int Id { get; set; }
        public string Details { get; set; }

        public static RuleDetails Find(Stream stream, int id)
        {
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                while (reader.ReadToFollowing("RuleDetails"))
                {
                    using (XmlReader subtree = reader.ReadSubtree())
                    {
                        XElement element = XElement.Load(subtree);
                        if (RuleSummary.IntValue(element, "ID") == id)
                        {
                            return new RuleDetails { Id = id, Details = Normalize(RuleSummary.Text(element, "Details")) };
                        }
                    }
                }
            }
            return null;
        }

        internal static string Normalize(string details)
        {
            string value = details ?? string.Empty;
            value = Regex.Replace(value, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
            value = Regex.Replace(value, @"</\s*(p|div|li|h[1-6])\s*>", "\n", RegexOptions.IgnoreCase);
            value = Regex.Replace(value, @"<[^>]+>", string.Empty);
            value = value.Replace("&nbsp;", " ");
            return Regex.Replace(value, @"\n{3,}", "\n\n").Trim();
        }
    }
}
