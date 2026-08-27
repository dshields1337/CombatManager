namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

internal sealed class CombatParticipantListAdapter(Activity context, IReadOnlyList<CombatParticipant> participants, int? activeSequence) : BaseAdapter<CombatParticipant>
{
    public override int Count => participants.Count;
    public override CombatParticipant this[int position] => participants[position];
    public override long GetItemId(int position) => participants[position].Sequence;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.combat_participant_item, parent, false)!;
        CombatParticipant participant = participants[position];
        bool active = participant.Sequence == activeSequence;
        view.FindViewById<TextView>(Resource.Id.combat_row_name)!.Text = (active ? "▶ " : "") + participant.DisplayName;
        view.FindViewById<TextView>(Resource.Id.combat_row_cr)!.Text = "CR " + participant.ChallengeRating +
            (string.IsNullOrWhiteSpace(participant.Notes) ? string.Empty : "\n" + participant.Notes) +
            (participant.Conditions.Count == 0 ? string.Empty : "\n" + string.Join(" • ", participant.Conditions.Select(condition => condition.DisplayText)));
        view.FindViewById<TextView>(Resource.Id.combat_row_initiative)!.Text = participant.Initiative.HasValue ? "Initiative " + participant.Initiative.Value : "Initiative —";
        view.FindViewById<TextView>(Resource.Id.combat_row_hp)!.Text = participant.IsDefeated
            ? $"DEFEATED  •  HP {participant.CurrentHP} / {participant.MaximumHP}"
            : $"HP {participant.CurrentHP} / {participant.MaximumHP}";
        view.SetBackgroundColor(new global::Android.Graphics.Color(context.GetColor(active ? Resource.Color.primary_light : participant.IsDefeated
            ? Resource.Color.defeated_background : Resource.Color.page_background)));
        view.ContentDescription = string.Join(", ", new[]
        {
            active ? "Active combatant" : string.Empty, participant.DisplayName,
            $"HP {participant.CurrentHP} of {participant.MaximumHP}",
            participant.Initiative.HasValue ? "Initiative " + participant.Initiative.Value : "Initiative not set",
            participant.IsDefeated ? "Defeated" : string.Empty,
            participant.Notes ?? string.Empty,
            string.Join(", ", participant.Conditions.Select(condition => condition.DisplayText))
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return view;
    }
}
