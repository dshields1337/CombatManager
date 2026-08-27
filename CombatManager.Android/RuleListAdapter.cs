namespace CombatManager.Android;
using global::CombatManager;
using global::Android.Views;

internal sealed class RuleListAdapter(Activity context, IReadOnlyList<RuleSummary> rules) : BaseAdapter<RuleSummary>
{
    public override int Count => rules.Count;
    public override RuleSummary this[int position] => rules[position];
    public override long GetItemId(int position) => rules[position].Id;
    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.rule_list_item, parent, false)!;
        RuleSummary rule = rules[position];
        view.FindViewById<TextView>(Resource.Id.rule_row_name)!.Text = rule.Name;
        view.FindViewById<TextView>(Resource.Id.rule_row_meta)!.Text = string.IsNullOrWhiteSpace(rule.Subtype)
            ? rule.Type : rule.Type + "  •  " + rule.Subtype;
        view.FindViewById<TextView>(Resource.Id.rule_row_format)!.Text = string.IsNullOrWhiteSpace(rule.Format)
            ? rule.Source : rule.Format;
        return view;
    }
}
