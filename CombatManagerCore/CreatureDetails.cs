using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace CombatManager
{
    public class CreatureDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AbilityScores { get; set; }
        public string Feats { get; set; }
        public string Skills { get; set; }
        public string Languages { get; set; }
        public string SpecialAttacks { get; set; }
        public string SpecialAbilities { get; set; }
        public string Environment { get; set; }
        public string Organization { get; set; }
        public string Treasure { get; set; }
        public string VisualDescription { get; set; }
        public string Description { get; set; }

        public static CreatureDetails Find(Stream stream, int id)
        {
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                while (reader.ReadToFollowing("Monster"))
                {
                    using (XmlReader subtree = reader.ReadSubtree())
                    {
                        XElement element = XElement.Load(subtree);
                        if (IntValue(element, "id") == id)
                        {
                            return Read(element);
                        }
                    }
                }
            }

            return null;
        }

        private static CreatureDetails Read(XElement element) => new CreatureDetails
        {
            Id = IntValue(element, "id"), Name = Text(element, "Name"),
            AbilityScores = Text(element, "AbilityScores"), Feats = Text(element, "Feats"),
            Skills = Text(element, "Skills"), Languages = Text(element, "Languages"),
            SpecialAttacks = Text(element, "SpecialAttacks"), SpecialAbilities = Text(element, "SpecialAbilities"),
            Environment = Text(element, "Environment"), Organization = Text(element, "Organization"),
            Treasure = Text(element, "Treasure"), VisualDescription = Text(element, "Description_Visual"),
            Description = Text(element, "Description")
        };

        private static string Text(XElement element, string name) => (string)element.Element(name) ?? string.Empty;
        private static int IntValue(XElement element, string name)
        {
            int value;
            return int.TryParse(Text(element, name), out value) ? value : 0;
        }
    }
}
