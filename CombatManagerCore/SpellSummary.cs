using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CombatManager
{
    public class SpellSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string School { get; set; }
        public string Subschool { get; set; }
        public string Descriptor { get; set; }
        public string Levels { get; set; }
        public string Duration { get; set; }
        public string Summary { get; set; }
        public string Source { get; set; }

        public static List<SpellSummary> Load(Stream stream) => XDocument.Load(stream).Descendants("Spell")
            .Select(element => new SpellSummary
            {
                Id = IntValue(element, "id"), Name = Text(element, "name"), School = Text(element, "school"),
                Subschool = Text(element, "subschool"), Descriptor = Text(element, "descriptor"),
                Levels = Text(element, "spell_level"), Duration = Text(element, "duration"),
                Summary = Text(element, "short_description").Trim(), Source = Text(element, "source")
            }).Where(spell => !string.IsNullOrWhiteSpace(spell.Name))
            .OrderBy(spell => spell.Name, StringComparer.OrdinalIgnoreCase).ToList();

        public static List<SpellSummary> Filter(IEnumerable<SpellSummary> spells, string query, string school)
        {
            string search = (query ?? string.Empty).Trim();
            return spells.Where(spell =>
                (search.Length == 0 || spell.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || spell.Summary.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                && (string.IsNullOrEmpty(school) || string.Equals(spell.School, school, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        internal static string Text(XElement element, string name) => (string)element.Element(name) ?? string.Empty;
        internal static int IntValue(XElement element, string name)
        {
            int value;
            return int.TryParse(Text(element, name), out value) ? value : 0;
        }
    }
}
