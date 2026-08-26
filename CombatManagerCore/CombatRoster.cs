using System.Collections.Generic;
using System.Linq;

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
            _participants.Sort((left, right) =>
            {
                int initiativeOrder = Nullable.Compare(right.Initiative, left.Initiative);
                return initiativeOrder != 0 ? initiativeOrder : left.Sequence.CompareTo(right.Sequence);
            });
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

        public void Clear()
        {
            _participants.Clear();
            _nextInstanceByCreature.Clear();
            _nextSequence = 1;
            ResetTurn();
        }
    }
}
