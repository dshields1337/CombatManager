using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CombatManager
{
    public class SavedEncounter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Snapshot { get; set; }
    }

    public class SavedEncounterLibrary
    {
        private readonly List<SavedEncounter> _encounters = new List<SavedEncounter>();
        private int _nextId = 1;

        public IReadOnlyList<SavedEncounter> Encounters => _encounters;

        public SavedEncounter Add(string name, string snapshot)
        {
            Validate(name, snapshot);
            var encounter = new SavedEncounter { Id = _nextId++, Name = name.Trim(), Snapshot = snapshot };
            _encounters.Add(encounter);
            Sort();
            return encounter;
        }

        public bool Update(int id, string name, string snapshot)
        {
            SavedEncounter encounter = Find(id);
            if (encounter == null || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(snapshot)) return false;
            encounter.Name = name.Trim();
            encounter.Snapshot = snapshot;
            Sort();
            return true;
        }

        public bool Rename(int id, string name)
        {
            SavedEncounter encounter = Find(id);
            if (encounter == null || string.IsNullOrWhiteSpace(name)) return false;
            encounter.Name = name.Trim();
            Sort();
            return true;
        }

        public bool Remove(int id)
        {
            SavedEncounter encounter = Find(id);
            return encounter != null && _encounters.Remove(encounter);
        }

        public SavedEncounter Find(int id) => _encounters.FirstOrDefault(item => item.Id == id);

        public void Save(Stream stream)
        {
            var settings = new XmlWriterSettings { Indent = true, CloseOutput = false };
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartElement("SavedEncounters");
                writer.WriteAttributeString("version", "1");
                foreach (SavedEncounter encounter in _encounters)
                {
                    writer.WriteStartElement("Encounter");
                    writer.WriteAttributeString("id", encounter.Id.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("name", encounter.Name);
                    writer.WriteElementString("Snapshot", encounter.Snapshot);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        public static bool TryLoad(Stream stream, out SavedEncounterLibrary library)
        {
            library = new SavedEncounterLibrary();
            try
            {
                XElement root = XDocument.Load(stream).Root;
                if (root == null || root.Name != "SavedEncounters" || IntAttribute(root, "version") != 1) return false;
                foreach (XElement element in root.Elements("Encounter"))
                {
                    var encounter = new SavedEncounter
                    {
                        Id = IntAttribute(element, "id"),
                        Name = ((string)element.Attribute("name") ?? string.Empty).Trim(),
                        Snapshot = (string)element.Element("Snapshot") ?? string.Empty
                    };
                    if (encounter.Id < 1 || string.IsNullOrWhiteSpace(encounter.Name) || string.IsNullOrWhiteSpace(encounter.Snapshot) ||
                        library._encounters.Any(item => item.Id == encounter.Id)) return false;
                    library._encounters.Add(encounter);
                }
                library._nextId = library._encounters.Count == 0 ? 1 : library._encounters.Max(item => item.Id) + 1;
                library.Sort();
                return true;
            }
            catch
            {
                library = new SavedEncounterLibrary();
                return false;
            }
        }

        private static int IntAttribute(XElement element, string name) =>
            int.Parse((string)element.Attribute(name), CultureInfo.InvariantCulture);

        private static void Validate(string name, string snapshot)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("A name is required.", "name");
            if (string.IsNullOrWhiteSpace(snapshot)) throw new System.ArgumentException("A snapshot is required.", "snapshot");
        }

        private void Sort() => _encounters.Sort((left, right) =>
            System.StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
    }
}
