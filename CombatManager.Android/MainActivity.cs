namespace CombatManager.Android;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
    private const string PreferenceName = "combat_manager_modern";
    private const string SelectedPageKey = "selected_page";
    private readonly Page[] _pages =
    [
        new(Resource.Id.combat_button, "Combat", "C"), new(Resource.Id.monsters_button, "Monsters", "M"),
        new(Resource.Id.feats_button, "Feats", "F"), new(Resource.Id.spells_button, "Spells", "S"),
        new(Resource.Id.rules_button, "Rules", "R"), new(Resource.Id.treasure_button, "Treasure", "T")
    ];

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);
        foreach (Page page in _pages) FindViewById<Button>(page.ButtonId)!.Click += (_, _) => SelectPage(page);
        FindViewById<ImageButton>(Resource.Id.about_button)!.Click += (_, _) =>
        {
            var dialog = new AlertDialog.Builder(this);
            dialog.SetTitle(Resource.String.about);
            dialog.SetMessage(Resource.String.about_message);
            dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
            dialog.Show();
        };
        int savedIndex = GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!.GetInt(SelectedPageKey, 0);
        SelectPage(_pages[Math.Clamp(savedIndex, 0, _pages.Length - 1)]);
    }

    private void SelectPage(Page selected)
    {
        FindViewById<TextView>(Resource.Id.page_title)!.Text = selected.Title;
        FindViewById<TextView>(Resource.Id.page_icon)!.Text = selected.Initial;
        for (int index = 0; index < _pages.Length; index++)
        {
            Button button = FindViewById<Button>(_pages[index].ButtonId)!;
            bool isSelected = _pages[index] == selected;
            button.Enabled = !isSelected;
            button.Alpha = isSelected ? 1f : 0.65f;
            if (isSelected) GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!.Edit()!.PutInt(SelectedPageKey, index)!.Apply();
        }
    }

    private sealed record Page(int ButtonId, string Title, string Initial);
}
