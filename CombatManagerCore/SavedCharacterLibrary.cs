using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CombatManager
{
    public class SavedCharacter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaximumHP { get; set; }
        public int InitiativeModifier { get; set; }
        public string Notes { get; set; }
    }

    public class SavedCharacterLibrary
    {
        private readonly List<SavedCharacter> _characters = new List<SavedCharacter>();
        private int _nextId = 1;

        public IReadOnlyList<SavedCharacter> Characters => _characters;

        public SavedCharacter Add(string name, int maximumHp, int initiativeModifier, string notes)
        {
            Validate(name, maximumHp);
            var character = new SavedCharacter
            {
                Id = _nextId++, Name = name.Trim(), MaximumHP = maximumHp,
                InitiativeModifier = initiativeModifier, Notes = (notes ?? string.Empty).Trim()
            };
            _characters.Add(character);
            Sort();
            return character;
        }

        public bool Update(int id, string name, int maximumHp, int initiativeModifier, string notes)
        {
            SavedCharacter character = _characters.FirstOrDefault(item => item.Id == id);
            if (character == null || string.IsNullOrWhiteSpace(name) || maximumHp < 1) return false;
            character.Name = name.Trim();
            character.MaximumHP = maximumHp;
            character.InitiativeModifier = initiativeModifier;
            character.Notes = (notes ?? string.Empty).Trim();
            Sort();
            return true;
        }

        public bool Remove(int id)
        {
            SavedCharacter character = _characters.FirstOrDefault(item => item.Id == id);
            return character != null && _characters.Remove(character);
        }

        public SavedCharacter Find(int id) => _characters.FirstOrDefault(item => item.Id == id);

        public void Save(Stream stream)
        {
            var settings = new XmlWriterSettings { Indent = true, CloseOutput = false };
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartElement("SavedCharacters");
                writer.WriteAttributeString("version", "1");
                foreach (SavedCharacter character in _characters)
                {
                    writer.WriteStartElement("Character");
                    writer.WriteAttributeString("id", character.Id.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("name", character.Name);
                    writer.WriteAttributeString("maximumHp", character.MaximumHP.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("initiativeModifier", character.InitiativeModifier.ToString(CultureInfo.InvariantCulture));
                    if (!string.IsNullOrEmpty(character.Notes)) writer.WriteAttributeString("notes", character.Notes);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        public static bool TryLoad(Stream stream, out SavedCharacterLibrary library)
        {
            library = new SavedCharacterLibrary();
            try
            {
                XElement root = XDocument.Load(stream).Root;
                if (root == null || root.Name != "SavedCharacters" || IntAttribute(root, "version") != 1) return false;
                foreach (XElement element in root.Elements("Character"))
                {
                    var character = new SavedCharacter
                    {
                        Id = IntAttribute(element, "id"), Name = ((string)element.Attribute("name") ?? string.Empty).Trim(),
                        MaximumHP = IntAttribute(element, "maximumHp"),
                        InitiativeModifier = IntAttribute(element, "initiativeModifier"),
                        Notes = ((string)element.Attribute("notes") ?? string.Empty).Trim()
                    };
                    if (character.Id < 1 || string.IsNullOrWhiteSpace(character.Name) || character.MaximumHP < 1 ||
                        library._characters.Any(item => item.Id == character.Id)) return false;
                    library._characters.Add(character);
                }
                library._nextId = library._characters.Count == 0 ? 1 : library._characters.Max(item => item.Id) + 1;
                library.Sort();
                return true;
            }
            catch
            {
                library = new SavedCharacterLibrary();
                return false;
            }
        }

        private static int IntAttribute(XElement element, string name) =>
            int.Parse((string)element.Attribute(name), CultureInfo.InvariantCulture);

        private static void Validate(string name, int maximumHp)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("A name is required.", "name");
            if (maximumHp < 1) throw new System.ArgumentOutOfRangeException("maximumHp");
        }

        private void Sort() => _characters.Sort((left, right) =>
            System.StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
    }
}
