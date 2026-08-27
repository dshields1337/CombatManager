namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

internal sealed class MonsterListAdapter(Activity context, IReadOnlyList<CreatureSummary> creatures,
    bool selectionMode = false, IReadOnlySet<int>? selectedIds = null) : BaseAdapter<CreatureSummary>
{
    public override int Count => creatures.Count;
    public override CreatureSummary this[int position] => creatures[position];
    public override long GetItemId(int position) => creatures[position].Id;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.monster_list_item, parent, false)!;
        CreatureSummary creature = creatures[position];
        view.FindViewById<TextView>(Resource.Id.monster_row_name)!.Text = creature.Name;
        view.FindViewById<TextView>(Resource.Id.monster_row_type)!.Text =
            string.Join(" ", new[] { creature.Size, creature.Type, creature.SubType }.Where(value => !string.IsNullOrWhiteSpace(value)));
        view.FindViewById<TextView>(Resource.Id.monster_row_cr)!.Text = "CR " + creature.CR;
        CheckBox selected = view.FindViewById<CheckBox>(Resource.Id.monster_row_selected)!;
        selected.Visibility = selectionMode ? ViewStates.Visible : ViewStates.Gone;
        selected.Checked = selectionMode && selectedIds?.Contains(creature.Id) == true;
        return view;
    }
}
