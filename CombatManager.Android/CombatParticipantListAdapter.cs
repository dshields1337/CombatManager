namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

internal sealed class CombatParticipantListAdapter(Activity context, IReadOnlyList<CombatParticipant> participants,
    int? activeSequence, bool compact = false) : BaseAdapter<CombatParticipant>
{
    public override int Count => participants.Count;
    public override CombatParticipant this[int position] => participants[position];
    public override long GetItemId(int position) => participants[position].Sequence;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.combat_participant_item, parent, false)!;
        CombatParticipant participant = participants[position];
        bool active = participant.Sequence == activeSequence;
        string inactivePrefix = participant.IsManual && !participant.IsPartyActive ? "Zzz…  " : string.Empty;
        view.FindViewById<TextView>(Resource.Id.combat_row_name)!.Text = (active ? "▶ " : "") + participant.DisplayName;
        view.FindViewById<TextView>(Resource.Id.combat_row_cr)!.Text = "CR " + participant.ChallengeRating +
            (string.IsNullOrWhiteSpace(participant.Notes) ? string.Empty : "\n" + participant.Notes) +
            (participant.Conditions.Count == 0 ? string.Empty : "\n" + string.Join(" • ", participant.Conditions.Select(condition => condition.DisplayText)));
        view.FindViewById<TextView>(Resource.Id.combat_row_initiative)!.Text = participant.Initiative.HasValue ? "Initiative " + participant.Initiative.Value : "Initiative —";
        string temporaryHp = participant.TemporaryHP > 0 ? $"  +  {participant.TemporaryHP} temp" : string.Empty;
        view.FindViewById<TextView>(Resource.Id.combat_row_hp)!.Text = participant.IsDefeated
            ? $"DEFEATED  •  HP {participant.CurrentHP} / {participant.MaximumHP}"
            : $"HP {participant.CurrentHP} / {participant.MaximumHP}{temporaryHp}";
        if (compact)
        {
            view.FindViewById<TextView>(Resource.Id.combat_row_cr)!.Visibility = ViewStates.Gone;
            view.FindViewById<TextView>(Resource.Id.combat_row_hp)!.Visibility = ViewStates.Gone;
            TextView initiative = view.FindViewById<TextView>(Resource.Id.combat_row_initiative)!;
            initiative.Text = participant.Initiative.HasValue
                ? (participant.InitiativeRoll.HasValue ? $"({participant.InitiativeRoll})  {participant.Initiative}" : participant.Initiative.ToString())
                : "—";
        }
        else
        {
            view.FindViewById<TextView>(Resource.Id.combat_row_cr)!.Visibility = ViewStates.Visible;
            view.FindViewById<TextView>(Resource.Id.combat_row_hp)!.Visibility = ViewStates.Visible;
        }
        SetParticipantName(view.FindViewById<TextView>(Resource.Id.combat_row_name)!, participant,
            (active ? "▶ " : string.Empty) + inactivePrefix);
        int background = active ? Resource.Color.primary_light : participant.IsManual && !participant.IsPartyActive
            ? Resource.Color.inactive_background : compact && !participant.IsManual ? MonsterHealthColor(participant)
            : participant.IsDefeated ? Resource.Color.defeated_background : Resource.Color.page_background;
        view.SetBackgroundColor(new global::Android.Graphics.Color(context.GetColor(background)));
        view.Alpha = participant.IsManual && !participant.IsPartyActive ? 0.55f : 1f;
        view.ContentDescription = string.Join(", ", new[]
        {
            active ? "Active combatant" : string.Empty, participant.DisplayName,
            $"HP {participant.CurrentHP} of {participant.MaximumHP}",
            participant.TemporaryHP > 0 ? $"Temporary HP {participant.TemporaryHP}" : string.Empty,
            participant.Initiative.HasValue ? "Initiative " + participant.Initiative.Value : "Initiative not set",
            participant.IsDefeated ? "Defeated" : string.Empty,
            participant.Notes ?? string.Empty,
            string.Join(", ", participant.Conditions.Select(condition => condition.DisplayText))
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return view;
    }

    private static int MonsterHealthColor(CombatParticipant participant)
    {
        if (participant.CurrentHP <= 0) return Resource.Color.health_defeated;
        double percentage = participant.MaximumHP <= 0 ? 0 : participant.CurrentHP * 100d / participant.MaximumHP;
        if (percentage < 20) return Resource.Color.health_critical;
        if (percentage < 51) return Resource.Color.health_wounded;
        return Resource.Color.health_good;
    }

    private void SetParticipantName(TextView view, CombatParticipant participant, string prefix)
    {
        string baseText = prefix + participant.BaseDisplayName;
        if (participant.IsManual || string.IsNullOrWhiteSpace(participant.MiniDescription))
        {
            view.Text = baseText;
            return;
        }
        string suffix = " (" + participant.MiniDescription + ")";
        var text = new global::Android.Text.SpannableString(baseText + suffix);
        text.SetSpan(new global::Android.Text.Style.RelativeSizeSpan(0.75f), baseText.Length, text.Length(),
            global::Android.Text.SpanTypes.ExclusiveExclusive);
        text.SetSpan(new global::Android.Text.Style.ForegroundColorSpan(
            new global::Android.Graphics.Color(context.GetColor(Resource.Color.text_secondary))), baseText.Length, text.Length(),
            global::Android.Text.SpanTypes.ExclusiveExclusive);
        view.SetText(text, TextView.BufferType.Spannable);
    }
}
