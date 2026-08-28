namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

internal sealed class SavedMonsterListAdapter(Activity context, IReadOnlyList<SavedMonster> monsters) : BaseAdapter<SavedMonster>
{
    public override int Count => monsters.Count;
    public override SavedMonster this[int position] => monsters[position];
    public override long GetItemId(int position) => monsters[position].Id;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.saved_monster_list_item, parent, false)!;
        SavedMonster monster = monsters[position];
        view.FindViewById<TextView>(Resource.Id.saved_monster_row_name)!.Text = monster.Name;
        view.FindViewById<TextView>(Resource.Id.saved_monster_row_notes)!.Text =
            string.IsNullOrWhiteSpace(monster.Notes) ? "No notes" : monster.Notes;
        string modifier = monster.InitiativeModifier >= 0 ? "+" + monster.InitiativeModifier : monster.InitiativeModifier.ToString();
        view.FindViewById<TextView>(Resource.Id.saved_monster_row_stats)!.Text =
            $"HP {monster.MaximumHP}  •  AC {monster.ArmorClass}\nInit {modifier}";
        view.ContentDescription = $"{monster.Name}, {monster.MaximumHP} maximum HP, armor class {monster.ArmorClass}, initiative modifier {modifier}";
        return view;
    }
}
