namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

internal sealed class SavedCharacterListAdapter(Activity context, IReadOnlyList<SavedCharacter> characters) : BaseAdapter<SavedCharacter>
{
    public override int Count => characters.Count;
    public override SavedCharacter this[int position] => characters[position];
    public override long GetItemId(int position) => characters[position].Id;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.saved_character_list_item, parent, false)!;
        SavedCharacter character = characters[position];
        view.FindViewById<TextView>(Resource.Id.saved_character_row_name)!.Text = character.Name;
        view.FindViewById<TextView>(Resource.Id.saved_character_row_notes)!.Text = string.IsNullOrWhiteSpace(character.Notes) ? "No notes" : character.Notes;
        string modifier = character.InitiativeModifier >= 0 ? "+" + character.InitiativeModifier : character.InitiativeModifier.ToString();
        view.FindViewById<TextView>(Resource.Id.saved_character_row_stats)!.Text = $"HP {character.MaximumHP}  •  Init {modifier}";
        view.ContentDescription = $"{character.Name}, {character.MaximumHP} maximum HP, initiative modifier {modifier}";
        return view;
    }
}
