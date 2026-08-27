namespace CombatManager.Android;
using global::CombatManager;
using global::Android.Views;

internal sealed class MagicItemListAdapter(Activity context, IReadOnlyList<MagicItemSummary> items) : BaseAdapter<MagicItemSummary>
{
    public override int Count => items.Count;
    public override MagicItemSummary this[int position] => items[position];
    public override long GetItemId(int position) => items[position].Id;
    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.magic_item_list_item, parent, false)!;
        MagicItemSummary item = items[position];
        view.FindViewById<TextView>(Resource.Id.magic_item_row_name)!.Text = item.Name;
        view.FindViewById<TextView>(Resource.Id.magic_item_row_meta)!.Text = item.Group + "  •  CL " + item.CasterLevel;
        view.FindViewById<TextView>(Resource.Id.magic_item_row_source)!.Text = string.IsNullOrWhiteSpace(item.BaseMagicItem)
            ? item.Source : item.BaseMagicItem;
        return view;
    }
}
