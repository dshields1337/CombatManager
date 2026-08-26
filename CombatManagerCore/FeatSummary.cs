using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CombatManager
{
    public class FeatSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Summary { get; set; }
        public string Prerequisites { get; set; }
        public string Benefit { get; set; }
        public string Normal { get; set; }
        public string Special { get; set; }
        public string Source { get; set; }

        public static List<FeatSummary> Load(Stream stream)
        {
            return XDocument.Load(stream).Descendants("Feat").Select(element => new FeatSummary
            {
                Id = IntValue(element, "Id"), Name = Text(element, "Name"), Type = Text(element, "Type"),
                Summary = Text(element, "Summary"), Prerequisites = Text(element, "Prerequistites"),
                Benefit = Text(element, "Benefit"), Normal = Text(element, "Normal"),
                Special = Text(element, "Special"), Source = Text(element, "Source")
            }).Where(feat => !string.IsNullOrWhiteSpace(feat.Name))
                .OrderBy(feat => feat.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static List<FeatSummary> Filter(IEnumerable<FeatSummary> feats, string query, string type)
        {
            string search = (query ?? string.Empty).Trim();
            return feats.Where(feat =>
                (search.Length == 0 || feat.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || feat.Summary.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                && (string.IsNullOrEmpty(type) || feat.Type.Split(',').Any(value =>
                    string.Equals(value.Trim(), type, StringComparison.OrdinalIgnoreCase)))).ToList();
        }

        private static string Text(XElement element, string name) => (string)element.Element(name) ?? string.Empty;
        private static int IntValue(XElement element, string name)
        {
            int value;
            return int.TryParse(Text(element, name), out value) ? value : 0;
        }
    }
}
