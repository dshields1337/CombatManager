using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CombatManager
{
    public class RuleSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public string Subtype { get; set; }
        public string Ability { get; set; }
        public string AbilityType { get; set; }
        public string Format { get; set; }
        public string Location { get; set; }
        public string Format2 { get; set; }
        public string Location2 { get; set; }
        public bool Untrained { get; set; }

        public static List<RuleSummary> Load(Stream stream) => XDocument.Load(stream).Descendants("Rule")
            .Select(element => new RuleSummary
            {
                Id = IntValue(element, "ID"), Name = Text(element, "Name"), Source = Text(element, "Source"),
                Type = Text(element, "Type"), Subtype = Text(element, "Subtype"), Ability = Text(element, "Ability"),
                AbilityType = Text(element, "AbilityType"), Format = Text(element, "Format"),
                Location = Text(element, "Location"), Format2 = Text(element, "Format2"),
                Location2 = Text(element, "Location2"), Untrained = BoolValue(element, "Untrained")
            }).Where(rule => !string.IsNullOrWhiteSpace(rule.Name))
            .OrderBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase).ToList();

        public static List<RuleSummary> Filter(IEnumerable<RuleSummary> rules, string query, string type)
        {
            string search = (query ?? string.Empty).Trim();
            return rules.Where(rule =>
                (search.Length == 0 || Contains(rule.Name, search) || Contains(rule.Subtype, search)
                    || Contains(rule.Source, search) || Contains(rule.Format, search))
                && (string.IsNullOrEmpty(type) || string.Equals(rule.Type, type, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        internal static string Text(XElement element, string name) => (string)element.Element(name) ?? string.Empty;
        internal static int IntValue(XElement element, string name)
        {
            int value;
            return int.TryParse(Text(element, name), out value) ? value : 0;
        }
        private static bool BoolValue(XElement element, string name)
        {
            bool value;
            return bool.TryParse(Text(element, name), out value) && value;
        }
        private static bool Contains(string value, string search) =>
            (value ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
