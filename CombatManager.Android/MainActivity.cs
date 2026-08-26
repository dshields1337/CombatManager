namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
    private const string PreferenceName = "combat_manager_modern";
    private const string SelectedPageKey = "selected_page";
    private List<CreatureSummary>? _creatures;
    private List<CreatureSummary> _visibleCreatures = [];
    private ArrayAdapter<string>? _monsterAdapter;
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
        FindViewById<SearchView>(Resource.Id.monster_search)!.QueryTextChange += (_, args) => FilterCreatures(args.NewText);
        FindViewById<ListView>(Resource.Id.monster_list)!.ItemClick += (_, args) => ShowCreature(_visibleCreatures[args.Position]);
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
        bool showMonsters = selected.Title == "Monsters";
        FindViewById<LinearLayout>(Resource.Id.placeholder_panel)!.Visibility = showMonsters ? ViewStates.Gone : ViewStates.Visible;
        FindViewById<LinearLayout>(Resource.Id.monsters_panel)!.Visibility = showMonsters ? ViewStates.Visible : ViewStates.Gone;
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

        if (showMonsters) _ = EnsureCreaturesLoadedAsync();
    }

    private async Task EnsureCreaturesLoadedAsync()
    {
        if (_creatures is not null)
        {
            FilterCreatures(FindViewById<SearchView>(Resource.Id.monster_search)!.Query);
            return;
        }

        try
        {
            List<CreatureSummary> loaded = await Task.Run(() =>
            {
                using Stream stream = Assets!.Open("BestiaryShort.xml");
                return CreatureSummary.Load(stream);
            });

            if (IsDestroyed) return;
            _creatures = loaded;
            FindViewById<ProgressBar>(Resource.Id.monster_progress)!.Visibility = ViewStates.Gone;
            FindViewById<ListView>(Resource.Id.monster_list)!.Visibility = ViewStates.Visible;
            FilterCreatures(FindViewById<SearchView>(Resource.Id.monster_search)!.Query);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            FindViewById<ProgressBar>(Resource.Id.monster_progress)!.Visibility = ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.monster_count)!.SetText(Resource.String.unable_to_load_bestiary);
        }
    }

    private void FilterCreatures(string? query)
    {
        if (_creatures is null) return;
        string search = query?.Trim() ?? string.Empty;
        _visibleCreatures = _creatures
            .Where(creature => creature.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || creature.Type.Contains(search, StringComparison.OrdinalIgnoreCase)
                || creature.CR.Equals(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _monsterAdapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItem1,
            _visibleCreatures.Select(creature => creature.ListText).ToArray());
        FindViewById<ListView>(Resource.Id.monster_list)!.Adapter = _monsterAdapter;
        string noun = _visibleCreatures.Count == 1 ? "creature" : "creatures";
        FindViewById<TextView>(Resource.Id.monster_count)!.Text = $"{_visibleCreatures.Count:N0} {noun}";
    }

    private void ShowCreature(CreatureSummary creature)
    {
        string type = string.Join(" ", new[] { creature.Size, creature.Type, creature.SubType }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        string details = $"CR {creature.CR}  •  XP {creature.XP}\n{creature.Alignment} {type}\n\n" +
            $"HP {creature.HP} {creature.HD}\nAC {creature.AC}\nSaves {creature.Saves}\nSpeed {creature.Speed}\n\n" +
            $"Melee: {ValueOrDash(creature.Melee)}\nRanged: {ValueOrDash(creature.Ranged)}\n\n" +
            $"Senses: {ValueOrDash(creature.Senses)}\nSource: {ValueOrDash(creature.Source)}";

        var dialog = new AlertDialog.Builder(this);
        dialog.SetTitle(creature.Name);
        dialog.SetMessage(details);
        dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        dialog.Show();
    }

    private static string ValueOrDash(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private sealed record Page(int ButtonId, string Title, string Initial);
}
