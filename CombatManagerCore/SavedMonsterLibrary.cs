using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CombatManager
{
    public class SavedMonster
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaximumHP { get; set; }
        public int ArmorClass { get; set; }
        public int TouchArmorClass { get; set; }
        public int FlatFootedArmorClass { get; set; }
        public int CMD { get; set; }
        public int CMB { get; set; }
        public int InitiativeModifier { get; set; }
        public string Notes { get; set; }
    }

    public class SavedMonsterLibrary
    {
        private readonly List<SavedMonster> _monsters = new List<SavedMonster>();
        private int _nextId = 1;

        public IReadOnlyList<SavedMonster> Monsters => _monsters;

        public SavedMonster Add(string name, int maximumHp, int armorClass, int touchArmorClass,
            int flatFootedArmorClass, int cmd, int cmb, int initiativeModifier, string notes)
        {
            Validate(name, maximumHp, armorClass, touchArmorClass, flatFootedArmorClass, cmd);
            var monster = new SavedMonster
            {
                Id = _nextId++, Name = name.Trim(), MaximumHP = maximumHp, ArmorClass = armorClass,
                TouchArmorClass = touchArmorClass, FlatFootedArmorClass = flatFootedArmorClass,
                CMD = cmd, CMB = cmb, InitiativeModifier = initiativeModifier,
                Notes = (notes ?? string.Empty).Trim()
            };
            _monsters.Add(monster);
            Sort();
            return monster;
        }

        public bool Update(int id, string name, int maximumHp, int armorClass, int touchArmorClass,
            int flatFootedArmorClass, int cmd, int cmb, int initiativeModifier, string notes)
        {
            SavedMonster monster = _monsters.FirstOrDefault(item => item.Id == id);
            if (monster == null) return false;
            try { Validate(name, maximumHp, armorClass, touchArmorClass, flatFootedArmorClass, cmd); }
            catch { return false; }
            monster.Name = name.Trim();
            monster.MaximumHP = maximumHp;
            monster.ArmorClass = armorClass;
            monster.TouchArmorClass = touchArmorClass;
            monster.FlatFootedArmorClass = flatFootedArmorClass;
            monster.CMD = cmd;
            monster.CMB = cmb;
            monster.InitiativeModifier = initiativeModifier;
            monster.Notes = (notes ?? string.Empty).Trim();
            Sort();
            return true;
        }

        public bool Remove(int id)
        {
            SavedMonster monster = _monsters.FirstOrDefault(item => item.Id == id);
            return monster != null && _monsters.Remove(monster);
        }

        public SavedMonster Find(int id) => _monsters.FirstOrDefault(item => item.Id == id);

        public void Save(Stream stream)
        {
            var settings = new XmlWriterSettings { Indent = true, CloseOutput = false };
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartElement("SavedMonsters");
                writer.WriteAttributeString("version", "1");
                foreach (SavedMonster monster in _monsters)
                {
                    writer.WriteStartElement("Monster");
                    writer.WriteAttributeString("id", monster.Id.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("name", monster.Name);
                    WriteInt(writer, "maximumHp", monster.MaximumHP);
                    WriteInt(writer, "ac", monster.ArmorClass);
                    WriteInt(writer, "touchAc", monster.TouchArmorClass);
                    WriteInt(writer, "flatFootedAc", monster.FlatFootedArmorClass);
                    WriteInt(writer, "cmd", monster.CMD);
                    WriteInt(writer, "cmb", monster.CMB);
                    WriteInt(writer, "initiativeModifier", monster.InitiativeModifier);
                    if (!string.IsNullOrEmpty(monster.Notes)) writer.WriteAttributeString("notes", monster.Notes);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        public static bool TryLoad(Stream stream, out SavedMonsterLibrary library)
        {
            library = new SavedMonsterLibrary();
            try
            {
                XElement root = XDocument.Load(stream).Root;
                if (root == null || root.Name != "SavedMonsters" || IntAttribute(root, "version") != 1) return false;
                foreach (XElement element in root.Elements("Monster"))
                {
                    var monster = new SavedMonster
                    {
                        Id = IntAttribute(element, "id"), Name = ((string)element.Attribute("name") ?? string.Empty).Trim(),
                        MaximumHP = IntAttribute(element, "maximumHp"), ArmorClass = IntAttribute(element, "ac"),
                        TouchArmorClass = IntAttribute(element, "touchAc"),
                        FlatFootedArmorClass = IntAttribute(element, "flatFootedAc"),
                        CMD = IntAttribute(element, "cmd"), CMB = IntAttribute(element, "cmb"),
                        InitiativeModifier = IntAttribute(element, "initiativeModifier"),
                        Notes = ((string)element.Attribute("notes") ?? string.Empty).Trim()
                    };
                    Validate(monster.Name, monster.MaximumHP, monster.ArmorClass, monster.TouchArmorClass,
                        monster.FlatFootedArmorClass, monster.CMD);
                    if (monster.Id < 1 || library._monsters.Any(item => item.Id == monster.Id)) return false;
                    library._monsters.Add(monster);
                }
                library._nextId = library._monsters.Count == 0 ? 1 : library._monsters.Max(item => item.Id) + 1;
                library.Sort();
                return true;
            }
            catch
            {
                library = new SavedMonsterLibrary();
                return false;
            }
        }

        private static void WriteInt(XmlWriter writer, string name, int value) =>
            writer.WriteAttributeString(name, value.ToString(CultureInfo.InvariantCulture));

        private static int IntAttribute(XElement element, string name) =>
            int.Parse((string)element.Attribute(name), CultureInfo.InvariantCulture);

        private static void Validate(string name, int maximumHp, int armorClass, int touchArmorClass,
            int flatFootedArmorClass, int cmd)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("A name is required.", "name");
            if (maximumHp < 1) throw new System.ArgumentOutOfRangeException("maximumHp");
            if (armorClass < 0 || touchArmorClass < 0 || flatFootedArmorClass < 0 || cmd < 0)
                throw new System.ArgumentOutOfRangeException("Combat defenses cannot be negative.");
        }

        private void Sort() => _monsters.Sort((left, right) =>
            System.StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
    }
}
