using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace CombatManager
{
    public class MagicItemDetails
    {
        public int Id { get; set; }
        public string Aura { get; set; }
        public string Slot { get; set; }
        public string Price { get; set; }
        public string Weight { get; set; }
        public string Description { get; set; }
        public string Requirements { get; set; }
        public string Cost { get; set; }
        public string Destruction { get; set; }
        public string Alignment { get; set; }
        public string Intelligence { get; set; }
        public string Wisdom { get; set; }
        public string Charisma { get; set; }
        public string Ego { get; set; }
        public string Communication { get; set; }
        public string Senses { get; set; }
        public string Powers { get; set; }
        public string RelatedItems { get; set; }
        public bool Mythic { get; set; }
        public bool LegendaryWeapon { get; set; }

        public static MagicItemDetails Find(Stream stream, int id)
        {
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                while (reader.ReadToFollowing("MagicItemDetails"))
                {
                    using (XmlReader subtree = reader.ReadSubtree())
                    {
                        XElement element = XElement.Load(subtree);
                        if (MagicItemSummary.IntValue(element, "ID") == id)
                        {
                            string html = MagicItemSummary.Text(element, "DescHTML");
                            return new MagicItemDetails
                            {
                                Id = id, Aura = MagicItemSummary.Text(element, "Aura"), Slot = MagicItemSummary.Text(element, "Slot"),
                                Price = MagicItemSummary.Text(element, "Price"), Weight = MagicItemSummary.Text(element, "Weight"),
                                Description = RuleDetails.Normalize(string.IsNullOrWhiteSpace(html)
                                    ? MagicItemSummary.Text(element, "Description") : html),
                                Requirements = MagicItemSummary.Text(element, "Requirements"), Cost = MagicItemSummary.Text(element, "Cost"),
                                Destruction = RuleDetails.Normalize(MagicItemSummary.Text(element, "Destruction")),
                                Alignment = MagicItemSummary.Text(element, "AL"), Intelligence = MagicItemSummary.Text(element, "Int"),
                                Wisdom = MagicItemSummary.Text(element, "Wis"), Charisma = MagicItemSummary.Text(element, "Cha"),
                                Ego = MagicItemSummary.Text(element, "Ego"), Communication = MagicItemSummary.Text(element, "Communication"),
                                Senses = MagicItemSummary.Text(element, "Senses"), Powers = MagicItemSummary.Text(element, "Powers"),
                                RelatedItems = MagicItemSummary.Text(element, "MagicItems"), Mythic = BoolValue(element, "Mythic"),
                                LegendaryWeapon = BoolValue(element, "LegendaryWeapon")
                            };
                        }
                    }
                }
            }
            return null;
        }

        private static bool BoolValue(XElement element, string name) => MagicItemSummary.Text(element, name) == "1";
    }
}
