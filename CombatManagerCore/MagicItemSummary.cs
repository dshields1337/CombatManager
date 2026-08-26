using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CombatManager
{
    public class MagicItemSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CasterLevel { get; set; }
        public string Group { get; set; }
        public string Source { get; set; }
        public string BaseMagicItem { get; set; }

        public static List<MagicItemSummary> Load(Stream stream) => XDocument.Load(stream).Descendants("MagicItem")
            .Select(element => new MagicItemSummary
            {
                Id = IntValue(element, "id"), Name = Text(element, "Name"),
                CasterLevel = Text(element, "CL"), Group = Text(element, "Group"),
                Source = Text(element, "Source"), BaseMagicItem = Text(element, "BaseMagicItem")
            }).Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

        public static List<MagicItemSummary> Filter(IEnumerable<MagicItemSummary> items, string query, string group)
        {
            string search = (query ?? string.Empty).Trim();
            return items.Where(item =>
                (search.Length == 0 || Contains(item.Name, search) || Contains(item.BaseMagicItem, search)
                    || Contains(item.Source, search))
                && (string.IsNullOrEmpty(group) || string.Equals(item.Group, group, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        internal static string Text(XElement element, string name) => (string)element.Element(name) ?? string.Empty;
        internal static int IntValue(XElement element, string name)
        {
            int value;
            return int.TryParse(Text(element, name), out value) ? value : 0;
        }
        private static bool Contains(string value, string search) =>
            (value ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
