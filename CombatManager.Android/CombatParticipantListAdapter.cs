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
        view.FindViewById<TextView>(Resource.Id.combat_row_name)!.Text = (participant.Sequence == activeSequence ? "▶ " : "") + participant.DisplayName;
        view.FindViewById<TextView>(Resource.Id.combat_row_cr)!.Text = "CR " + participant.ChallengeRating;
        view.FindViewById<TextView>(Resource.Id.combat_row_initiative)!.Text = participant.Initiative.HasValue ? "Initiative " + participant.Initiative.Value : "Initiative —";
        view.FindViewById<TextView>(Resource.Id.combat_row_hp)!.Text = $"HP {participant.CurrentHP} / {participant.MaximumHP}";
        return view;
    }
}
