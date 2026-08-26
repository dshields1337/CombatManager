using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CombatManager
{
    /// <summary>
    /// Read-only bestiary projection that can be loaded without the legacy Monster database graph.
    /// </summary>
    public class CreatureSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CR { get; set; }
        public string XP { get; set; }
        public string Alignment { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Senses { get; set; }
        public string AC { get; set; }
        public int HP { get; set; }
        public string HD { get; set; }
        public string Saves { get; set; }
        public string Speed { get; set; }
        public string Melee { get; set; }
        public string Ranged { get; set; }
        public string Source { get; set; }

        public string ListText => Name + "  •  CR " + CR;

        public static List<CreatureSummary> Load(Stream stream)
        {
            XDocument document = XDocument.Load(stream);
            return document.Descendants("Monster")
                .Select(Read)
                .Where(creature => !string.IsNullOrWhiteSpace(creature.Name))
                .OrderBy(creature => creature.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static CreatureSummary Read(XElement element)
        {
            return new CreatureSummary
            {
                Id = IntValue(element, "id"),
                Name = Text(element, "Name"),
                CR = Text(element, "CR"),
                XP = Text(element, "XP"),
                Alignment = Text(element, "Alignment"),
                Size = Text(element, "Size"),
                Type = Text(element, "Type"),
                SubType = Text(element, "SubType"),
                Senses = Text(element, "Senses"),
                AC = Text(element, "AC"),
                HP = IntValue(element, "HP"),
                HD = Text(element, "HD"),
                Saves = Text(element, "Saves"),
                Speed = Text(element, "Speed"),
                Melee = Text(element, "Melee"),
                Ranged = Text(element, "Ranged"),
                Source = Text(element, "Source")
            };
        }

        private static string Text(XElement element, string name) =>
            (string)element.Element(name) ?? string.Empty;

        private static int IntValue(XElement element, string name)
        {
            int value;
            return int.TryParse(Text(element, name), out value) ? value : 0;
        }
    }
}
