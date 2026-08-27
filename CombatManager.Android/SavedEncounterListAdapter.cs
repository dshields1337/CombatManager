namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

internal sealed class SavedEncounterListAdapter(Activity context, IReadOnlyList<SavedEncounter> encounters) : BaseAdapter<SavedEncounter>
{
    public override int Count => encounters.Count;
    public override SavedEncounter this[int position] => encounters[position];
    public override long GetItemId(int position) => encounters[position].Id;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.saved_encounter_list_item, parent, false)!;
        SavedEncounter encounter = encounters[position];
        view.FindViewById<TextView>(Resource.Id.saved_encounter_row_name)!.Text = encounter.Name;
        view.FindViewById<TextView>(Resource.Id.saved_encounter_row_status)!.Text = "Saved encounter";
        view.ContentDescription = encounter.Name + ", saved encounter";
        return view;
    }
}
