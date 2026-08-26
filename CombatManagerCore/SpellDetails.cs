using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace CombatManager
{
    public class SpellDetails
    {
        public int Id { get; set; }
        public string CastingTime { get; set; }
        public string Components { get; set; }
        public string Range { get; set; }
        public string Target { get; set; }
        public string Effect { get; set; }
        public string Area { get; set; }
        public string Duration { get; set; }
        public string SavingThrow { get; set; }
        public string SpellResistance { get; set; }
        public string Description { get; set; }

        public static SpellDetails Find(Stream stream, int id)
        {
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                while (reader.ReadToFollowing("Spell"))
                {
                    using (XmlReader subtree = reader.ReadSubtree())
                    {
                        XElement element = XElement.Load(subtree);
                        if (SpellSummary.IntValue(element, "id") == id) return new SpellDetails
                        {
                            Id = id, CastingTime = SpellSummary.Text(element, "casting_time"),
                            Components = SpellSummary.Text(element, "components"), Range = SpellSummary.Text(element, "range"),
                            Target = SpellSummary.Text(element, "target"), Effect = SpellSummary.Text(element, "effect"),
                            Area = SpellSummary.Text(element, "area"), Duration = SpellSummary.Text(element, "duration"),
                            SavingThrow = SpellSummary.Text(element, "saving_throw"),
                            SpellResistance = SpellSummary.Text(element, "spell_resistence"),
                            Description = SpellSummary.Text(element, "description")
                        };
                    }
                }
            }
            return null;
        }
    }
}
