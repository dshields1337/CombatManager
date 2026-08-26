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
        public string DisplayName => InstanceNumber <= 1 ? Name : Name + " " + InstanceNumber;
    }

    public class CombatRoster
    {
        private readonly List<CombatParticipant> _participants = new List<CombatParticipant>();
        private readonly Dictionary<int, int> _nextInstanceByCreature = new Dictionary<int, int>();
        private int _nextSequence = 1;

        public IReadOnlyList<CombatParticipant> Participants => _participants;

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
            return participant != null && _participants.Remove(participant);
        }

        public void Clear()
        {
            _participants.Clear();
            _nextInstanceByCreature.Clear();
            _nextSequence = 1;
        }
    }
}
