using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        public int? Initiative { get; set; }
        public bool IsDefeated => CurrentHP <= 0;
        public string DisplayName => InstanceNumber <= 1 ? Name : Name + " " + InstanceNumber;
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
                MaximumHP = creature.HP, CurrentHP = creature.HP
            };
            _participants.Add(participant);
            return participant;
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

        public bool ApplyDamage(int sequence, int amount)
        {
            if (amount < 0) return false;
            CombatParticipant participant = _participants.FirstOrDefault(item => item.Sequence == sequence);
            if (participant == null) return false;
            participant.CurrentHP -= amount;
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

        public CombatParticipant NextTurn()
        {
            if (_participants.Count == 0) return null;
            int index = ActiveIndex();
            if (index < 0) { _round = 1; _activeSequence = _participants[0].Sequence; }
            else
            {
                index = (index + 1) % _participants.Count;
                if (index == 0) _round++;
                _activeSequence = _participants[index].Sequence;
            }
            return ActiveParticipant;
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
                    if (participant.Initiative.HasValue) writer.WriteAttributeString("initiative", participant.Initiative.Value.ToString(CultureInfo.InvariantCulture));
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
                    roster._participants.Add(new CombatParticipant
                    {
                        Sequence = AttributeInt(element, "sequence"),
                        CreatureId = AttributeInt(element, "creatureId"),
                        InstanceNumber = AttributeInt(element, "instance"),
                        Name = (string)element.Attribute("name") ?? string.Empty,
                        ChallengeRating = (string)element.Attribute("cr") ?? string.Empty,
                        MaximumHP = AttributeInt(element, "maximumHp"),
                        CurrentHP = AttributeInt(element, "currentHp"),
                        Initiative = string.IsNullOrEmpty(initiativeText) ? (int?)null : int.Parse(initiativeText, CultureInfo.InvariantCulture)
                    });
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

        public void Clear()
        {
            _participants.Clear();
            _nextInstanceByCreature.Clear();
            _nextSequence = 1;
            ResetTurn();
        }
    }

}
