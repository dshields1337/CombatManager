using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CombatManager
{
    public sealed class ConditionReference
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public static List<ConditionReference> Load(Stream stream)
        {
            return XDocument.Load(stream).Root?.Elements("Condition")
                .Select(element => new ConditionReference
                {
                    Name = ((string)element.Element("Name") ?? string.Empty).Trim(),
                    Description = ((string)element.Element("Text") ?? string.Empty).Trim()
                })
                .Where(item => item.Name.Length > 0)
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList() ?? new List<ConditionReference>();
        }

        public static ConditionReference Find(IEnumerable<ConditionReference> conditions, string name) =>
            conditions?.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
