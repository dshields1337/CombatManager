using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CombatManager
{
    public class CombatParticipant
    {
        public int Sequence { get; set; }
        public int CreatureId { get; set; }
        public int InstanceNumber { get; set; }
        public string Name { get; set; }
        public string ChallengeRating { get; set; }
        public int MaximumHP { get; set; }
        public int CurrentHP { get; set; }
        public int TemporaryHP { get; set; }
        public int? Initiative { get; set; }
        public int InitiativeModifier { get; set; }
        public string Notes { get; set; }
        public List<CombatCondition> Conditions { get; set; } = new List<CombatCondition>();
        public bool IsDefeated => CurrentHP <= 0;
        public bool IsManual => CreatureId <= 0;
        public string DisplayName => InstanceNumber <= 1 ? Name : Name + " " + InstanceNumber;
    }

    public class CombatCondition
    {
        public string Name { get; set; }
        public int RemainingTurns { get; set; }
        public bool IsTimed => RemainingTurns > 0;
        public string DisplayText => IsTimed ? Name + " (" + RemainingTurns + ")" : Name;
    }

    public class CombatRoster
    {
        private readonly List<CombatParticipant> _participants = new List<CombatParticipant>();
        private readonly Dictionary<int, int> _nextInstanceByCreature = new Dictionary<int, int>();
        private int _nextSequence = 1;
        private int? _activeSequence;
        private int _round;

        public IReadOnlyList<CombatParticipant> Participants => _participants;
        public CombatParticipant ActiveParticipant => _activeSequence.HasValue
            ? _participants.FirstOrDefault(item => item.Sequence == _activeSequence.Value) : null;
        public int Round => _round;

        public CombatParticipant Add(CreatureSummary creature)
        {
            int instanceNumber;
            if (!_nextInstanceByCreature.TryGetValue(creature.Id, out instanceNumber)) instanceNumber = 1;
            _nextInstanceByCreature[creature.Id] = instanceNumber + 1;
            var participant = new CombatParticipant
            {
                Sequence = _nextSequence++, CreatureId = creature.Id, InstanceNumber = instanceNumber,
                Name = creature.Name, ChallengeRating = creature.CR,
                MaximumHP = creature.HP, CurrentHP = creature.HP, InitiativeModifier = creature.InitiativeModifier
            };
            _participants.Add(participant);
            return participant;
        }

        public CombatParticipant AddManual(string name, int maximumHp)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("A name is required.", "name");
            if (maximumHp < 1) throw new System.ArgumentOutOfRangeException("maximumHp");
            string cleanName = name.Trim();
            int instanceNumber = _participants.Where(item => item.IsManual && string.Equals(item.Name, cleanName, System.StringComparison.OrdinalIgnoreCase))
                .Select(item => item.InstanceNumber).DefaultIfEmpty(0).Max() + 1;
            var participant = new CombatParticipant
            {
                Sequence = _nextSequence++, CreatureId = 0, InstanceNumber = instanceNumber,
                Name = cleanName, ChallengeRating = "—", MaximumHP = maximumHp, CurrentHP = maximumHp
            };
            _participants.Add(participant);
            return participant;
        }

        public CombatParticipant Duplicate(int sequence)
        {
            CombatParticipant source = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (source == null) return null;
            int instanceNumber;
            if (source.IsManual)
                instanceNumber = _participants.Where(item => item.IsManual && string.Equals(item.Name, source.Name, System.StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.InstanceNumber).DefaultIfEmpty(0).Max() + 1;
            else
            {
                if (!_nextInstanceByCreature.TryGetValue(source.CreatureId, out instanceNumber)) instanceNumber = source.InstanceNumber + 1;
                _nextInstanceByCreature[source.CreatureId] = instanceNumber + 1;
            }
            var duplicate = new CombatParticipant
            {
                Sequence = _nextSequence++, CreatureId = source.CreatureId, InstanceNumber = instanceNumber,
                Name = source.Name, ChallengeRating = source.ChallengeRating, MaximumHP = source.MaximumHP,
                CurrentHP = source.MaximumHP, Notes = source.Notes ?? string.Empty, InitiativeModifier = source.InitiativeModifier,
                Conditions = source.Conditions.Select(condition => new CombatCondition { Name = condition.Name, RemainingTurns = condition.RemainingTurns }).ToList()
            };
            _participants.Add(duplicate);
            return duplicate;
        }

        public bool Remove(int sequence)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null) return false;
            bool wasActive = _activeSequence == sequence;
            bool removed = _participants.Remove(participant);
            if (_participants.Count == 0) ResetTurn();
            else if (wasActive) _activeSequence = _participants[0].Sequence;
            return removed;
        }

        public bool SetInitiative(int sequence, int initiative)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null) return false;
            participant.Initiative = initiative;
            SortByInitiative();
            return true;
        }

        public bool SetInitiatives(IReadOnlyDictionary<int, int> initiatives)
        {
            if (initiatives == null || initiatives.Count == 0 ||
                initiatives.Keys.Any(sequence => !_participants.Any(item => item.Sequence == sequence))) return false;
            foreach (CombatParticipant participant in _participants)
                if (initiatives.TryGetValue(participant.Sequence, out int initiative)) participant.Initiative = initiative;
            SortByInitiative();
            return true;
        }

        public int RollMonsterInitiatives(System.Func<int> rollD20)
        {
            if (rollD20 == null) throw new System.ArgumentNullException("rollD20");
            int count = 0;
            foreach (CombatParticipant participant in _participants.Where(item => !item.IsManual))
            {
                int roll = rollD20();
                if (roll < 1 || roll > 20) throw new System.ArgumentOutOfRangeException("rollD20", "A d20 roll must be between 1 and 20.");
                participant.Initiative = roll + participant.InitiativeModifier;
                count++;
            }
            SortByInitiative();
            return count;
        }

        public bool ApplyDamage(int sequence, int amount)
        {
            if (amount < 0) return false;
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null) return false;
            int absorbed = System.Math.Min(participant.TemporaryHP, amount);
            participant.TemporaryHP -= absorbed;
            participant.CurrentHP -= amount - absorbed;
            return true;
        }

        public bool SetTemporaryHp(int sequence, int amount)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null || amount < 0) return false;
            participant.TemporaryHP = amount;
            return true;
        }

        public bool ApplyHealing(int sequence, int amount)
        {
            if (amount < 0) return false;
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null) return false;
            participant.CurrentHP = System.Math.Min(participant.MaximumHP, participant.CurrentHP + amount);
            return true;
        }

        public bool UpdateManual(int sequence, string name, int maximumHp, int currentHp)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence && item.IsManual);
            if (participant == null || string.IsNullOrWhiteSpace(name) || maximumHp < 1) return false;
            string cleanName = name.Trim();
            participant.Name = cleanName;
            participant.InstanceNumber = _participants.Where(item => item.Sequence != sequence && item.IsManual && string.Equals(item.Name, cleanName, System.StringComparison.OrdinalIgnoreCase))
                .Select(item => item.InstanceNumber).DefaultIfEmpty(0).Max() + 1;
            participant.MaximumHP = maximumHp;
            participant.CurrentHP = currentHp;
            return true;
        }

        public bool ResetHp(int sequence)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null) return false;
            participant.CurrentHP = participant.MaximumHP;
            return true;
        }

        public bool SetNotes(int sequence, string notes)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null) return false;
            participant.Notes = (notes ?? string.Empty).Trim();
            return true;
        }

        public bool AddCondition(int sequence, string name, int turns)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null || string.IsNullOrWhiteSpace(name) || turns < 0) return false;
            participant.Conditions.Add(new CombatCondition { Name = name.Trim(), RemainingTurns = turns });
            return true;
        }

        public bool RemoveCondition(int sequence, int index)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null || index < 0 || index >= participant.Conditions.Count) return false;
            participant.Conditions.RemoveAt(index);
            return true;
        }

        public bool UpdateCondition(int sequence, int index, string name, int turns)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null || index < 0 || index >= participant.Conditions.Count || string.IsNullOrWhiteSpace(name) || turns < 0) return false;
            participant.Conditions[index].Name = name.Trim();
            participant.Conditions[index].RemainingTurns = turns;
            return true;
        }

        public bool ClearConditions(int sequence)
        {
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null || participant.Conditions.Count == 0) return false;
            participant.Conditions.Clear();
            return true;
        }

        public void ResetTurns()
        {
            foreach (CombatParticipant participant in _participants) participant.Initiative = null;
            _participants.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            ResetTurn();
        }

        public string ToSummaryText()
        {
            var text = new StringBuilder("Combat Manager Encounter");
            if (_round > 0) text.Append(" — Round ").Append(_round);
            foreach (CombatParticipant participant in _participants)
            {
                text.AppendLine().Append(participant == ActiveParticipant ? "▶ " : "• ").Append(participant.DisplayName)
                    .Append(" — HP ").Append(participant.CurrentHP).Append('/').Append(participant.MaximumHP);
                if (participant.TemporaryHP > 0) text.Append(" + ").Append(participant.TemporaryHP).Append(" temporary");
                if (participant.Initiative.HasValue) text.Append(" — Initiative ").Append(participant.Initiative.Value);
                if (!string.IsNullOrWhiteSpace(participant.Notes)) text.Append(" — ").Append(participant.Notes);
                if (participant.Conditions.Count > 0) text.Append(" — ").Append(string.Join(", ", participant.Conditions.Select(condition => condition.DisplayText)));
            }
            return text.ToString();
        }

        public CombatParticipant NextTurn()
        {
            if (_participants.Count == 0) return null;
            int index = ActiveIndex();
            if (index < 0) { _round = 1; _activeSequence = _participants[0].Sequence; }
            else
            {
                TickConditions(_participants[index]);
                index = (index + 1) % _participants.Count;
                if (index == 0) _round++;
                _activeSequence = _participants[index].Sequence;
            }
            return ActiveParticipant;
        }

        private static void TickConditions(CombatParticipant participant)
        {
            List<CombatCondition> timed = participant.Conditions.Where(item => item.IsTimed).ToList();
            foreach (CombatCondition condition in timed) condition.RemainingTurns--;
            participant.Conditions.RemoveAll(condition => timed.Contains(condition) && condition.RemainingTurns <= 0);
        }

        public CombatParticipant PreviousTurn()
        {
            if (_participants.Count == 0) return null;
            int index = ActiveIndex();
            if (index < 0) { _round = 1; _activeSequence = _participants[0].Sequence; }
            else
            {
                if (index == 0 && _round > 1) _round--;
                index = (index - 1 + _participants.Count) % _participants.Count;
                _activeSequence = _participants[index].Sequence;
            }
            return ActiveParticipant;
        }

        private int ActiveIndex()
        {
            return _activeSequence.HasValue
                ? _participants.FindIndex(item => item.Sequence == _activeSequence.Value) : -1;
        }

        private void ResetTurn()
        {
            _activeSequence = null;
            _round = 0;
        }

        private void SortByInitiative()
        {
            _participants.Sort((left, right) =>
            {
                int initiativeOrder = System.Nullable.Compare(right.Initiative, left.Initiative);
                return initiativeOrder != 0 ? initiativeOrder : left.Sequence.CompareTo(right.Sequence);
            });
        }

        public void Save(Stream stream)
        {
            var settings = new XmlWriterSettings { Indent = true, CloseOutput = false };
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartElement("CombatRoster");
                writer.WriteAttributeString("version", "1");
                writer.WriteAttributeString("round", _round.ToString(CultureInfo.InvariantCulture));
                if (_activeSequence.HasValue) writer.WriteAttributeString("activeSequence", _activeSequence.Value.ToString(CultureInfo.InvariantCulture));
                foreach (CombatParticipant participant in _participants)
                {
                    writer.WriteStartElement("Participant");
                    writer.WriteAttributeString("sequence", participant.Sequence.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("creatureId", participant.CreatureId.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("instance", participant.InstanceNumber.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("name", participant.Name ?? string.Empty);
                    writer.WriteAttributeString("cr", participant.ChallengeRating ?? string.Empty);
                    writer.WriteAttributeString("maximumHp", participant.MaximumHP.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("currentHp", participant.CurrentHP.ToString(CultureInfo.InvariantCulture));
                    if (participant.TemporaryHP > 0) writer.WriteAttributeString("temporaryHp", participant.TemporaryHP.ToString(CultureInfo.InvariantCulture));
                    if (participant.Initiative.HasValue) writer.WriteAttributeString("initiative", participant.Initiative.Value.ToString(CultureInfo.InvariantCulture));
                    if (participant.InitiativeModifier != 0) writer.WriteAttributeString("initiativeModifier", participant.InitiativeModifier.ToString(CultureInfo.InvariantCulture));
                    if (!string.IsNullOrEmpty(participant.Notes)) writer.WriteAttributeString("notes", participant.Notes);
                    foreach (CombatCondition condition in participant.Conditions)
                    {
                        writer.WriteStartElement("Condition");
                        writer.WriteAttributeString("name", condition.Name ?? string.Empty);
                        writer.WriteAttributeString("turns", condition.RemainingTurns.ToString(CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        public static bool TryLoad(Stream stream, out CombatRoster roster)
        {
            roster = new CombatRoster();
            try
            {
                XElement root = XDocument.Load(stream).Root;
                if (root == null || root.Name != "CombatRoster" || AttributeInt(root, "version") != 1) return false;
                foreach (XElement element in root.Elements("Participant"))
                {
                    string initiativeText = (string)element.Attribute("initiative");
                    var participant = new CombatParticipant
                    {
                        Sequence = AttributeInt(element, "sequence"),
                        CreatureId = AttributeInt(element, "creatureId"),
                        InstanceNumber = AttributeInt(element, "instance"),
                        Name = (string)element.Attribute("name") ?? string.Empty,
                        ChallengeRating = (string)element.Attribute("cr") ?? string.Empty,
                        MaximumHP = AttributeInt(element, "maximumHp"),
                        CurrentHP = AttributeInt(element, "currentHp"),
                        TemporaryHP = AttributeIntOrDefault(element, "temporaryHp"),
                        Initiative = string.IsNullOrEmpty(initiativeText) ? (int?)null : int.Parse(initiativeText, CultureInfo.InvariantCulture),
                        InitiativeModifier = AttributeIntOrDefault(element, "initiativeModifier"),
                        Notes = (string)element.Attribute("notes") ?? string.Empty
                    };
                    foreach (XElement conditionElement in element.Elements("Condition"))
                    {
                        string name = (string)conditionElement.Attribute("name") ?? string.Empty;
                        int turns = AttributeInt(conditionElement, "turns");
                        if (!string.IsNullOrWhiteSpace(name) && turns >= 0)
                            participant.Conditions.Add(new CombatCondition { Name = name, RemainingTurns = turns });
                    }
                    roster._participants.Add(participant);
                }
                roster.SortByInitiative();
                roster._nextSequence = roster._participants.Count == 0 ? 1 : roster._participants.Max(item => item.Sequence) + 1;
                foreach (CombatParticipant participant in roster._participants)
                {
                    int nextInstance;
                    if (!roster._nextInstanceByCreature.TryGetValue(participant.CreatureId, out nextInstance)) nextInstance = 1;
                    roster._nextInstanceByCreature[participant.CreatureId] = System.Math.Max(nextInstance, participant.InstanceNumber + 1);
                }
                string activeText = (string)root.Attribute("activeSequence");
                int? activeSequence = string.IsNullOrEmpty(activeText) ? (int?)null : int.Parse(activeText, CultureInfo.InvariantCulture);
                if (activeSequence.HasValue && roster._participants.Any(item => item.Sequence == activeSequence.Value))
                {
                    roster._activeSequence = activeSequence;
                    roster._round = System.Math.Max(1, AttributeInt(root, "round"));
                }
                return true;
            }
            catch
            {
                roster = new CombatRoster();
                return false;
            }
        }

        private static int AttributeInt(XElement element, string name)
        {
            return int.Parse((string)element.Attribute(name), CultureInfo.InvariantCulture);
        }

        private static int AttributeIntOrDefault(XElement element, string name)
        {
            XAttribute attribute = element.Attribute(name);
            return attribute == null ? 0 : int.Parse(attribute.Value, CultureInfo.InvariantCulture);
        }

        public void Clear()
        {
            _participants.Clear();
            _nextInstanceByCreature.Clear();
            _nextSequence = 1;
            ResetTurn();
        }
    }

}
