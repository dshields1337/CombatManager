namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

internal sealed class FeatListAdapter(Activity context, IReadOnlyList<FeatSummary> feats) : BaseAdapter<FeatSummary>
{
    public override int Count => feats.Count;
    public override FeatSummary this[int position] => feats[position];
    public override long GetItemId(int position) => feats[position].Id;
    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.feat_list_item, parent, false)!;
        FeatSummary feat = feats[position];
        view.FindViewById<TextView>(Resource.Id.feat_row_name)!.Text = feat.Name;
        view.FindViewById<TextView>(Resource.Id.feat_row_meta)!.Text = feat.Type;
        view.FindViewById<TextView>(Resource.Id.feat_row_summary)!.Text = feat.Summary;
        return view;
    }
}
