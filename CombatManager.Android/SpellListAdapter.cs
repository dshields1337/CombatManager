namespace CombatManager.Android;
using global::CombatManager;
using global::Android.Views;

internal sealed class SpellListAdapter(Activity context, IReadOnlyList<SpellSummary> spells) : BaseAdapter<SpellSummary>
{
    public override int Count => spells.Count;
    public override SpellSummary this[int position] => spells[position];
    public override long GetItemId(int position) => spells[position].Id;
    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.spell_list_item, parent, false)!;
        SpellSummary spell = spells[position];
        view.FindViewById<TextView>(Resource.Id.spell_row_name)!.Text = spell.Name;
        view.FindViewById<TextView>(Resource.Id.spell_row_meta)!.Text = spell.School + "  •  " + spell.Levels;
        view.FindViewById<TextView>(Resource.Id.spell_row_summary)!.Text = spell.Summary;
        return view;
    }
}
