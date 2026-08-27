namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
    private const string PreferenceName = "combat_manager_modern";
    private const string EncounterFileName = "active-encounter.xml";
    private const string SelectedPageKey = "selected_page";
    private const string MonsterQueryKey = "monster_query";
    private const string MonsterTypeKey = "monster_type";
    private const string MonsterCrKey = "monster_cr";
    private const string FeatQueryKey = "feat_query";
    private const string FeatTypeKey = "feat_type";
    private const string SpellQueryKey = "spell_query";
    private const string SpellSchoolKey = "spell_school";
    private const string RuleQueryKey = "rule_query";
    private const string RuleTypeKey = "rule_type";
    private const string MagicItemQueryKey = "magic_item_query";
    private const string MagicItemGroupKey = "magic_item_group";
    private const string AllTypes = "All types";
    private const string AllChallengeRatings = "All CRs";
    private const string AllFeatTypes = "All feat types";
    private const string AllSpellSchools = "All schools";
    private List<CreatureSummary>? _creatures;
    private List<CreatureSummary> _visibleCreatures = [];
    private readonly Dictionary<int, CreatureDetails> _detailCache = [];
    private bool _initializingFilters;
    private List<FeatSummary>? _feats;
    private List<FeatSummary> _visibleFeats = [];
    private bool _initializingFeatFilters;
    private List<SpellSummary>? _spells;
    private List<SpellSummary> _visibleSpells = [];
    private readonly Dictionary<int, SpellDetails> _spellDetailCache = [];
    private bool _initializingSpellFilters;
    private const string AllRuleTypes = "All rule types";
    private List<RuleSummary>? _rules;
    private List<RuleSummary> _visibleRules = [];
    private readonly Dictionary<int, RuleDetails> _ruleDetailCache = [];
    private bool _initializingRuleFilters;
    private const string AllMagicItemGroups = "All item groups";
    private List<MagicItemSummary>? _magicItems;
    private List<MagicItemSummary> _visibleMagicItems = [];
    private readonly Dictionary<int, MagicItemDetails> _magicItemDetailCache = [];
    private bool _initializingMagicItemFilters;
    private CombatRoster _combatRoster = new();
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
        _combatRoster = LoadPersistedCombatRoster();
        foreach (Page page in _pages) FindViewById<Button>(page.ButtonId)!.Click += (_, _) => SelectPage(page);
        FindViewById<ListView>(Resource.Id.combat_list)!.ItemClick += (_, args) => ShowCombatParticipant(_combatRoster.Participants[args.Position]);
        FindViewById<Button>(Resource.Id.clear_combat_button)!.Click += (_, _) => ConfirmClearCombat();
        FindViewById<Button>(Resource.Id.add_combatant_button)!.Click += (_, _) => ShowAddCombatantDialog();
        FindViewById<Button>(Resource.Id.next_turn_button)!.Click += (_, _) =>
        {
            _combatRoster.NextTurn();
            CommitCombatChange();
        };
        FindViewById<Button>(Resource.Id.previous_turn_button)!.Click += (_, _) =>
        {
            _combatRoster.PreviousTurn();
            CommitCombatChange();
        };
        FindViewById<SearchView>(Resource.Id.monster_search)!.QueryTextChange += (_, args) => OnQueryChanged(args.NewText);
        FindViewById<Spinner>(Resource.Id.monster_type_filter)!.ItemSelected += (_, _) => OnFilterChanged();
        FindViewById<Spinner>(Resource.Id.monster_cr_filter)!.ItemSelected += (_, _) => OnFilterChanged();
        FindViewById<ListView>(Resource.Id.monster_list)!.ItemClick += (_, args) => ShowCreature(_visibleCreatures[args.Position]);
        FindViewById<SearchView>(Resource.Id.feat_search)!.QueryTextChange += (_, args) => OnFeatQueryChanged(args.NewText);
        FindViewById<Spinner>(Resource.Id.feat_type_filter)!.ItemSelected += (_, _) => OnFeatFilterChanged();
        FindViewById<ListView>(Resource.Id.feat_list)!.ItemClick += (_, args) => ShowFeat(_visibleFeats[args.Position]);
        FindViewById<SearchView>(Resource.Id.spell_search)!.QueryTextChange += (_, args) => OnSpellQueryChanged(args.NewText);
        FindViewById<Spinner>(Resource.Id.spell_school_filter)!.ItemSelected += (_, _) => OnSpellFilterChanged();
        FindViewById<ListView>(Resource.Id.spell_list)!.ItemClick += (_, args) => ShowSpell(_visibleSpells[args.Position]);
        FindViewById<SearchView>(Resource.Id.rule_search)!.QueryTextChange += (_, args) => OnRuleQueryChanged(args.NewText);
        FindViewById<Spinner>(Resource.Id.rule_type_filter)!.ItemSelected += (_, _) => OnRuleFilterChanged();
        FindViewById<ListView>(Resource.Id.rule_list)!.ItemClick += (_, args) => _ = ShowRuleAsync(_visibleRules[args.Position]);
        FindViewById<SearchView>(Resource.Id.magic_item_search)!.QueryTextChange += (_, args) => OnMagicItemQueryChanged(args.NewText);
        FindViewById<Spinner>(Resource.Id.magic_item_group_filter)!.ItemSelected += (_, _) => OnMagicItemFilterChanged();
        FindViewById<ListView>(Resource.Id.magic_item_list)!.ItemClick += (_, args) => _ = ShowMagicItemAsync(_visibleMagicItems[args.Position]);
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
        bool showCombat = selected.Title == "Combat";
        bool showMonsters = selected.Title == "Monsters";
        bool showFeats = selected.Title == "Feats";
        bool showSpells = selected.Title == "Spells";
        bool showRules = selected.Title == "Rules";
        bool showTreasure = selected.Title == "Treasure";
        FindViewById<LinearLayout>(Resource.Id.placeholder_panel)!.Visibility = showCombat || showMonsters || showFeats || showSpells || showRules || showTreasure ? ViewStates.Gone : ViewStates.Visible;
        FindViewById<LinearLayout>(Resource.Id.combat_panel)!.Visibility = showCombat ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.monsters_panel)!.Visibility = showMonsters ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.feats_panel)!.Visibility = showFeats ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.spells_panel)!.Visibility = showSpells ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.rules_panel)!.Visibility = showRules ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.treasure_panel)!.Visibility = showTreasure ? ViewStates.Visible : ViewStates.Gone;
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

        if (showCombat) RefreshCombatRoster();
        if (showMonsters) _ = EnsureCreaturesLoadedAsync();
        if (showFeats) _ = EnsureFeatsLoadedAsync();
        if (showSpells) _ = EnsureSpellsLoadedAsync();
        if (showRules) _ = EnsureRulesLoadedAsync();
        if (showTreasure) _ = EnsureMagicItemsLoadedAsync();
    }

    private async Task EnsureMagicItemsLoadedAsync()
    {
        if (_magicItems is not null) { FilterMagicItems(CurrentMagicItemQuery()); return; }
        try
        {
            _magicItems = await Task.Run(() =>
            {
                using Stream stream = Assets!.Open("MagicItemsShort.xml");
                return MagicItemSummary.Load(stream);
            });
            if (IsDestroyed) return;
            string[] groups = [AllMagicItemGroups, .. _magicItems.Select(item => item.Group)
                .Where(group => !string.IsNullOrWhiteSpace(group)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group, StringComparer.OrdinalIgnoreCase)];
            _initializingMagicItemFilters = true;
            FindViewById<Spinner>(Resource.Id.magic_item_group_filter)!.Adapter =
                new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerDropDownItem, groups);
            var preferences = GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!;
            SelectSpinnerValue(FindViewById<Spinner>(Resource.Id.magic_item_group_filter)!, groups,
                preferences.GetString(MagicItemGroupKey, AllMagicItemGroups) ?? AllMagicItemGroups);
            FindViewById<SearchView>(Resource.Id.magic_item_search)!.SetQuery(
                preferences.GetString(MagicItemQueryKey, string.Empty), false);
            _initializingMagicItemFilters = false;
            FindViewById<ProgressBar>(Resource.Id.magic_item_progress)!.Visibility = ViewStates.Gone;
            FindViewById<ListView>(Resource.Id.magic_item_list)!.Visibility = ViewStates.Visible;
            FilterMagicItems(CurrentMagicItemQuery());
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            FindViewById<ProgressBar>(Resource.Id.magic_item_progress)!.Visibility = ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.magic_item_count)!.SetText(Resource.String.unable_to_load_magic_items);
        }
    }

    private void FilterMagicItems(string? query)
    {
        if (_magicItems is null) return;
        string selectedGroup = FindViewById<Spinner>(Resource.Id.magic_item_group_filter)!.SelectedItem?.ToString() ?? AllMagicItemGroups;
        _visibleMagicItems = MagicItemSummary.Filter(_magicItems, query ?? string.Empty,
            selectedGroup == AllMagicItemGroups ? string.Empty : selectedGroup);
        FindViewById<ListView>(Resource.Id.magic_item_list)!.Adapter = new MagicItemListAdapter(this, _visibleMagicItems);
        string noun = _visibleMagicItems.Count == 1 ? "item" : "items";
        FindViewById<TextView>(Resource.Id.magic_item_count)!.Text = $"{_visibleMagicItems.Count:N0} {noun}";
    }

    private string CurrentMagicItemQuery() => FindViewById<SearchView>(Resource.Id.magic_item_search)!.Query ?? string.Empty;

    private void OnMagicItemQueryChanged(string? query)
    {
        if (_initializingMagicItemFilters) return;
        SavePreference(MagicItemQueryKey, query ?? string.Empty);
        FilterMagicItems(query);
    }

    private void OnMagicItemFilterChanged()
    {
        if (_initializingMagicItemFilters || _magicItems is null) return;
        SavePreference(MagicItemGroupKey,
            FindViewById<Spinner>(Resource.Id.magic_item_group_filter)!.SelectedItem?.ToString() ?? AllMagicItemGroups);
        FilterMagicItems(CurrentMagicItemQuery());
    }

    private async Task ShowMagicItemAsync(MagicItemSummary item)
    {
        var loadingBuilder = new AlertDialog.Builder(this);
        loadingBuilder.SetMessage(Resource.String.loading_magic_item_details);
        loadingBuilder.SetCancelable(false);
        AlertDialog? loading = loadingBuilder.Show();
        try
        {
            if (!_magicItemDetailCache.TryGetValue(item.Id, out MagicItemDetails? details))
            {
                details = await Task.Run(() =>
                {
                    using Stream stream = Assets!.Open("MagicItemDetails.xml");
                    return MagicItemDetails.Find(stream, item.Id);
                });
                if (details is not null) _magicItemDetailCache[item.Id] = details;
            }
            loading?.Dismiss();
            if (IsDestroyed) return;
            var sections = new List<string>();
            if (details is null) AddSection(sections, null, GetString(Resource.String.magic_item_details_not_found));
            else
            {
                AddSection(sections, "AURA", details.Aura);
                AddSection(sections, "CASTER LEVEL", item.CasterLevel);
                AddSection(sections, "SLOT", details.Slot);
                AddSection(sections, "PRICE", details.Price);
                AddSection(sections, "WEIGHT", details.Weight);
                AddSection(sections, "DESCRIPTION", details.Description);
                AddSection(sections, "REQUIREMENTS", details.Requirements);
                AddSection(sections, "COST", details.Cost);
                AddSection(sections, "DESTRUCTION", details.Destruction);
                string abilities = string.Join(", ", new[] { Pair("AL", details.Alignment), Pair("Int", details.Intelligence),
                    Pair("Wis", details.Wisdom), Pair("Cha", details.Charisma), Pair("Ego", details.Ego) }
                    .Where(value => value.Length > 0));
                AddSection(sections, "INTELLIGENT ITEM", abilities);
                AddSection(sections, "COMMUNICATION", details.Communication);
                AddSection(sections, "SENSES", details.Senses);
                AddSection(sections, "POWERS", details.Powers);
                AddSection(sections, "RELATED ITEMS", details.RelatedItems);
                if (details.Mythic) AddSection(sections, "MYTHIC", "Yes");
                if (details.LegendaryWeapon) AddSection(sections, "LEGENDARY WEAPON", "Yes");
            }
            AddSection(sections, "GROUP", item.Group);
            AddSection(sections, "BASE ITEM", item.BaseMagicItem);
            AddSection(sections, "SOURCE", item.Source);
            var dialog = new AlertDialog.Builder(this);
            dialog.SetTitle(item.Name);
            dialog.SetMessage(string.Join("\n\n", sections));
            dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
            dialog.Show();
        }
        catch (Exception exception)
        {
            loading?.Dismiss();
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            Toast.MakeText(this, Resource.String.magic_item_details_not_found, ToastLength.Long)?.Show();
        }
    }

    private static string Pair(string name, string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : name + " " + value;

    private async Task EnsureRulesLoadedAsync()
    {
        if (_rules is not null) { FilterRules(CurrentRuleQuery()); return; }
        try
        {
            _rules = await Task.Run(() =>
            {
                using Stream stream = Assets!.Open("RuleShort.xml");
                return RuleSummary.Load(stream);
            });
            if (IsDestroyed) return;
            string[] types = [AllRuleTypes, .. _rules.Select(rule => rule.Type)
                .Where(type => !string.IsNullOrWhiteSpace(type)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(type => type, StringComparer.OrdinalIgnoreCase)];
            _initializingRuleFilters = true;
            FindViewById<Spinner>(Resource.Id.rule_type_filter)!.Adapter =
                new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerDropDownItem, types);
            var preferences = GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!;
            SelectSpinnerValue(FindViewById<Spinner>(Resource.Id.rule_type_filter)!, types,
                preferences.GetString(RuleTypeKey, AllRuleTypes) ?? AllRuleTypes);
            FindViewById<SearchView>(Resource.Id.rule_search)!.SetQuery(
                preferences.GetString(RuleQueryKey, string.Empty), false);
            _initializingRuleFilters = false;
            FindViewById<ProgressBar>(Resource.Id.rule_progress)!.Visibility = ViewStates.Gone;
            FindViewById<ListView>(Resource.Id.rule_list)!.Visibility = ViewStates.Visible;
            FilterRules(CurrentRuleQuery());
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            FindViewById<ProgressBar>(Resource.Id.rule_progress)!.Visibility = ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.rule_count)!.SetText(Resource.String.unable_to_load_rules);
        }
    }

    private void FilterRules(string? query)
    {
        if (_rules is null) return;
        string selectedType = FindViewById<Spinner>(Resource.Id.rule_type_filter)!.SelectedItem?.ToString() ?? AllRuleTypes;
        _visibleRules = RuleSummary.Filter(_rules, query ?? string.Empty,
            selectedType == AllRuleTypes ? string.Empty : selectedType);
        FindViewById<ListView>(Resource.Id.rule_list)!.Adapter = new RuleListAdapter(this, _visibleRules);
        string noun = _visibleRules.Count == 1 ? "rule" : "rules";
        FindViewById<TextView>(Resource.Id.rule_count)!.Text = $"{_visibleRules.Count:N0} {noun}";
    }

    private string CurrentRuleQuery() => FindViewById<SearchView>(Resource.Id.rule_search)!.Query ?? string.Empty;

    private void OnRuleQueryChanged(string? query)
    {
        if (_initializingRuleFilters) return;
        SavePreference(RuleQueryKey, query ?? string.Empty);
        FilterRules(query);
    }

    private void OnRuleFilterChanged()
    {
        if (_initializingRuleFilters || _rules is null) return;
        SavePreference(RuleTypeKey,
            FindViewById<Spinner>(Resource.Id.rule_type_filter)!.SelectedItem?.ToString() ?? AllRuleTypes);
        FilterRules(CurrentRuleQuery());
    }

    private async Task ShowRuleAsync(RuleSummary rule)
    {
        var loadingBuilder = new AlertDialog.Builder(this);
        loadingBuilder.SetMessage(Resource.String.loading_rule_details);
        loadingBuilder.SetCancelable(false);
        AlertDialog? loading = loadingBuilder.Show();
        try
        {
            if (!_ruleDetailCache.TryGetValue(rule.Id, out RuleDetails? details))
            {
                details = await Task.Run(() =>
                {
                    using Stream stream = Assets!.Open("RuleDetails.xml");
                    return RuleDetails.Find(stream, rule.Id);
                });
                if (details is not null) _ruleDetailCache[rule.Id] = details;
            }
            loading?.Dismiss();
            if (IsDestroyed) return;
            var sections = new List<string>();
            AddSection(sections, null, details?.Details ?? GetString(Resource.String.rule_details_not_found));
            AddSection(sections, "TYPE", rule.Type);
            AddSection(sections, "SUBTYPE", rule.Subtype);
            AddSection(sections, "ABILITY", rule.Ability);
            AddSection(sections, "ABILITY TYPE", rule.AbilityType);
            AddSection(sections, "FORMAT", rule.Format);
            AddSection(sections, "LOCATION", rule.Location);
            AddSection(sections, "SECOND FORMAT", rule.Format2);
            AddSection(sections, "SECOND LOCATION", rule.Location2);
            if (rule.Untrained) AddSection(sections, "UNTRAINED", "Yes");
            AddSection(sections, "SOURCE", rule.Source);
            var dialog = new AlertDialog.Builder(this);
            dialog.SetTitle(rule.Name);
            dialog.SetMessage(string.Join("\n\n", sections));
            dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
            dialog.Show();
        }
        catch (Exception exception)
        {
            loading?.Dismiss();
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            Toast.MakeText(this, Resource.String.rule_details_not_found, ToastLength.Long)?.Show();
        }
    }

    private async Task EnsureSpellsLoadedAsync()
    {
        if (_spells is not null) { FilterSpells(CurrentSpellQuery()); return; }
        try
        {
            _spells = await Task.Run(() =>
            {
                using Stream stream = Assets!.Open("SpellsShort.xml");
                return SpellSummary.Load(stream);
            });
            if (IsDestroyed) return;
            string[] schools = [AllSpellSchools, .. _spells.Select(spell => spell.School)
                .Where(school => !string.IsNullOrWhiteSpace(school))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(school => school, StringComparer.OrdinalIgnoreCase)];
            _initializingSpellFilters = true;
            FindViewById<Spinner>(Resource.Id.spell_school_filter)!.Adapter =
                new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerDropDownItem, schools);
            var preferences = GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!;
            SelectSpinnerValue(FindViewById<Spinner>(Resource.Id.spell_school_filter)!, schools,
                preferences.GetString(SpellSchoolKey, AllSpellSchools) ?? AllSpellSchools);
            FindViewById<SearchView>(Resource.Id.spell_search)!.SetQuery(
                preferences.GetString(SpellQueryKey, string.Empty), false);
            _initializingSpellFilters = false;
            FindViewById<ProgressBar>(Resource.Id.spell_progress)!.Visibility = ViewStates.Gone;
            FindViewById<ListView>(Resource.Id.spell_list)!.Visibility = ViewStates.Visible;
            FilterSpells(CurrentSpellQuery());
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            FindViewById<ProgressBar>(Resource.Id.spell_progress)!.Visibility = ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.spell_count)!.SetText(Resource.String.unable_to_load_spells);
        }
    }

    private void FilterSpells(string? query)
    {
        if (_spells is null) return;
        string selectedSchool = FindViewById<Spinner>(Resource.Id.spell_school_filter)!.SelectedItem?.ToString() ?? AllSpellSchools;
        _visibleSpells = SpellSummary.Filter(_spells, query ?? string.Empty,
            selectedSchool == AllSpellSchools ? string.Empty : selectedSchool);
        FindViewById<ListView>(Resource.Id.spell_list)!.Adapter = new SpellListAdapter(this, _visibleSpells);
        string noun = _visibleSpells.Count == 1 ? "spell" : "spells";
        FindViewById<TextView>(Resource.Id.spell_count)!.Text = $"{_visibleSpells.Count:N0} {noun}";
    }

    private string CurrentSpellQuery() => FindViewById<SearchView>(Resource.Id.spell_search)!.Query ?? string.Empty;

    private void OnSpellQueryChanged(string? query)
    {
        if (_initializingSpellFilters) return;
        SavePreference(SpellQueryKey, query ?? string.Empty);
        FilterSpells(query);
    }

    private void OnSpellFilterChanged()
    {
        if (_initializingSpellFilters || _spells is null) return;
        SavePreference(SpellSchoolKey,
            FindViewById<Spinner>(Resource.Id.spell_school_filter)!.SelectedItem?.ToString() ?? AllSpellSchools);
        FilterSpells(CurrentSpellQuery());
    }

    private void ShowSpell(SpellSummary spell)
    {
        var sections = new List<string>();
        string classification = string.Join(" ", new[] { spell.School, spell.Subschool, spell.Descriptor }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        AddSection(sections, null, classification);
        AddSection(sections, "LEVEL", spell.Levels);
        AddSection(sections, "DURATION", spell.Duration);
        AddSection(sections, "SUMMARY", spell.Summary);
        AddSection(sections, "SOURCE", spell.Source);
        var dialog = new AlertDialog.Builder(this);
        dialog.SetTitle(spell.Name);
        dialog.SetMessage(string.Join("\n\n", sections));
        dialog.SetNeutralButton(Resource.String.full_spell_details, (_, _) => _ = ShowFullSpellDetailsAsync(spell));
        dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        dialog.Show();
    }

    private async Task ShowFullSpellDetailsAsync(SpellSummary spell)
    {
        var loadingBuilder = new AlertDialog.Builder(this);
        loadingBuilder.SetMessage(Resource.String.loading_spell_details);
        loadingBuilder.SetCancelable(false);
        AlertDialog? loading = loadingBuilder.Show();
        try
        {
            if (!_spellDetailCache.TryGetValue(spell.Id, out SpellDetails? details))
            {
                details = await Task.Run(() =>
                {
                    using Stream stream = Assets!.Open("Spells.xml");
                    return SpellDetails.Find(stream, spell.Id);
                });
                if (details is not null) _spellDetailCache[spell.Id] = details;
            }

            loading?.Dismiss();
            if (IsDestroyed) return;
            var dialog = new AlertDialog.Builder(this);
            dialog.SetTitle(spell.Name);
            dialog.SetMessage(details is null ? GetString(Resource.String.spell_details_not_found) : FormatFullSpellDetails(details));
            dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
            dialog.Show();
        }
        catch (Exception exception)
        {
            loading?.Dismiss();
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            Toast.MakeText(this, Resource.String.spell_details_not_found, ToastLength.Long)?.Show();
        }
    }

    private static string FormatFullSpellDetails(SpellDetails details)
    {
        var sections = new List<string>();
        AddSection(sections, "CASTING TIME", details.CastingTime);
        AddSection(sections, "COMPONENTS", details.Components);
        AddSection(sections, "RANGE", details.Range);
        AddSection(sections, "TARGET", details.Target);
        AddSection(sections, "EFFECT", details.Effect);
        AddSection(sections, "AREA", details.Area);
        AddSection(sections, "DURATION", details.Duration);
        AddSection(sections, "SAVING THROW", details.SavingThrow);
        AddSection(sections, "SPELL RESISTANCE", details.SpellResistance);
        AddSection(sections, "DESCRIPTION", details.Description);
        return string.Join("\n\n", sections);
    }

    private async Task EnsureFeatsLoadedAsync()
    {
        if (_feats is not null) { FilterFeats(CurrentFeatQuery()); return; }
        try
        {
            _feats = await Task.Run(() =>
            {
                using Stream stream = Assets!.Open("Feats.xml");
                return FeatSummary.Load(stream);
            });
            if (IsDestroyed) return;
            string[] types = [AllFeatTypes, .. _feats.SelectMany(feat => feat.Type.Split(','))
                .Select(type => type.Trim()).Where(type => type.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(type => type, StringComparer.OrdinalIgnoreCase)];
            _initializingFeatFilters = true;
            FindViewById<Spinner>(Resource.Id.feat_type_filter)!.Adapter =
                new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerDropDownItem, types);
            var preferences = GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!;
            SelectSpinnerValue(FindViewById<Spinner>(Resource.Id.feat_type_filter)!, types,
                preferences.GetString(FeatTypeKey, AllFeatTypes) ?? AllFeatTypes);
            FindViewById<SearchView>(Resource.Id.feat_search)!.SetQuery(
                preferences.GetString(FeatQueryKey, string.Empty), false);
            _initializingFeatFilters = false;
            FindViewById<ProgressBar>(Resource.Id.feat_progress)!.Visibility = ViewStates.Gone;
            FindViewById<ListView>(Resource.Id.feat_list)!.Visibility = ViewStates.Visible;
            FilterFeats(CurrentFeatQuery());
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            FindViewById<ProgressBar>(Resource.Id.feat_progress)!.Visibility = ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.feat_count)!.SetText(Resource.String.unable_to_load_feats);
        }
    }

    private void FilterFeats(string? query)
    {
        if (_feats is null) return;
        string selectedType = FindViewById<Spinner>(Resource.Id.feat_type_filter)!.SelectedItem?.ToString() ?? AllFeatTypes;
        _visibleFeats = FeatSummary.Filter(_feats, query ?? string.Empty,
            selectedType == AllFeatTypes ? string.Empty : selectedType);
        FindViewById<ListView>(Resource.Id.feat_list)!.Adapter = new FeatListAdapter(this, _visibleFeats);
        string noun = _visibleFeats.Count == 1 ? "feat" : "feats";
        FindViewById<TextView>(Resource.Id.feat_count)!.Text = $"{_visibleFeats.Count:N0} {noun}";
    }

    private string CurrentFeatQuery() => FindViewById<SearchView>(Resource.Id.feat_search)!.Query ?? string.Empty;

    private void OnFeatQueryChanged(string? query)
    {
        if (_initializingFeatFilters) return;
        SavePreference(FeatQueryKey, query ?? string.Empty);
        FilterFeats(query);
    }

    private void OnFeatFilterChanged()
    {
        if (_initializingFeatFilters || _feats is null) return;
        SavePreference(FeatTypeKey,
            FindViewById<Spinner>(Resource.Id.feat_type_filter)!.SelectedItem?.ToString() ?? AllFeatTypes);
        FilterFeats(CurrentFeatQuery());
    }

    private void ShowFeat(FeatSummary feat)
    {
        var sections = new List<string>();
        AddSection(sections, null, feat.Summary);
        AddSection(sections, "PREREQUISITES", feat.Prerequisites);
        AddSection(sections, "BENEFIT", feat.Benefit);
        AddSection(sections, "NORMAL", feat.Normal);
        AddSection(sections, "SPECIAL", feat.Special);
        AddSection(sections, "SOURCE", feat.Source);
        var dialog = new AlertDialog.Builder(this);
        dialog.SetTitle(feat.Name + " (" + feat.Type + ")");
        dialog.SetMessage(string.Join("\n\n", sections));
        dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        dialog.Show();
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
                using Stream firstStream = Assets!.Open("BestiaryShort.xml");
                using Stream secondStream = Assets.Open("BestiaryShort2.xml");
                return CreatureSummary.Combine(
                    CreatureSummary.Load(firstStream), CreatureSummary.Load(secondStream));
            });

            if (IsDestroyed) return;
            _creatures = loaded;
            PopulateCreatureFilters();
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
        string selectedType = FindViewById<Spinner>(Resource.Id.monster_type_filter)!.SelectedItem?.ToString() ?? AllTypes;
        string selectedCr = FindViewById<Spinner>(Resource.Id.monster_cr_filter)!.SelectedItem?.ToString() ?? AllChallengeRatings;
        _visibleCreatures = CreatureSummary.Filter(_creatures, query ?? string.Empty,
            selectedType == AllTypes ? string.Empty : selectedType,
            selectedCr == AllChallengeRatings ? string.Empty : selectedCr);

        FindViewById<ListView>(Resource.Id.monster_list)!.Adapter = new MonsterListAdapter(this, _visibleCreatures);
        string noun = _visibleCreatures.Count == 1 ? "creature" : "creatures";
        FindViewById<TextView>(Resource.Id.monster_count)!.Text = $"{_visibleCreatures.Count:N0} {noun}";
    }

    private void PopulateCreatureFilters()
    {
        _initializingFilters = true;
        List<CreatureSummary> creatures = _creatures!;
        string[] types = [AllTypes, .. creatures.Select(creature => creature.Type)
            .Where(type => !string.IsNullOrWhiteSpace(type)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(type => type, StringComparer.OrdinalIgnoreCase)];
        string[] challengeRatings = [AllChallengeRatings, .. creatures.Select(creature => creature.CR)
            .Where(cr => !string.IsNullOrWhiteSpace(cr)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(CreatureSummary.ChallengeRatingValue)];
        FindViewById<Spinner>(Resource.Id.monster_type_filter)!.Adapter =
            new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerDropDownItem, types);
        FindViewById<Spinner>(Resource.Id.monster_cr_filter)!.Adapter =
            new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerDropDownItem, challengeRatings);
        var preferences = GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!;
        SelectSpinnerValue(FindViewById<Spinner>(Resource.Id.monster_type_filter)!, types,
            preferences.GetString(MonsterTypeKey, AllTypes) ?? AllTypes);
        SelectSpinnerValue(FindViewById<Spinner>(Resource.Id.monster_cr_filter)!, challengeRatings,
            preferences.GetString(MonsterCrKey, AllChallengeRatings) ?? AllChallengeRatings);
        FindViewById<SearchView>(Resource.Id.monster_search)!.SetQuery(preferences.GetString(MonsterQueryKey, string.Empty), false);
        _initializingFilters = false;
    }

    private void OnQueryChanged(string? query)
    {
        if (_initializingFilters) return;
        SavePreference(MonsterQueryKey, query ?? string.Empty);
        FilterCreatures(query);
    }

    private void OnFilterChanged()
    {
        if (_initializingFilters || _creatures is null) return;
        SavePreference(MonsterTypeKey, FindViewById<Spinner>(Resource.Id.monster_type_filter)!.SelectedItem?.ToString() ?? AllTypes);
        SavePreference(MonsterCrKey, FindViewById<Spinner>(Resource.Id.monster_cr_filter)!.SelectedItem?.ToString() ?? AllChallengeRatings);
        FilterCreatures(CurrentQuery());
    }

    private void SavePreference(string key, string value) =>
        GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!.Edit()!.PutString(key, value)!.Apply();

    private static void SelectSpinnerValue(Spinner spinner, string[] values, string selected)
    {
        int index = Array.FindIndex(values, value => string.Equals(value, selected, StringComparison.OrdinalIgnoreCase));
        spinner.SetSelection(Math.Max(index, 0));
    }

    private string CurrentQuery() => FindViewById<SearchView>(Resource.Id.monster_search)!.Query ?? string.Empty;

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
        dialog.SetNeutralButton(Resource.String.full_details, (_, _) => _ = ShowFullDetailsAsync(creature));
        dialog.SetNegativeButton(Resource.String.add_to_combat, (_, _) => AddCreatureToCombat(creature));
        dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        dialog.Show();
    }

    private void AddCreatureToCombat(CreatureSummary creature)
    {
        CombatParticipant participant = _combatRoster.Add(creature);
        CommitCombatChange();
        Toast.MakeText(this, participant.DisplayName + " added to combat.", ToastLength.Short)?.Show();
    }

    private void RefreshCombatRoster()
    {
        int count = _combatRoster.Participants.Count;
        FindViewById<TextView>(Resource.Id.combat_count)!.Text = count == 1 ? "1 combatant" : $"{count:N0} combatants";
        FindViewById<Button>(Resource.Id.clear_combat_button)!.Enabled = count > 0;
        FindViewById<TextView>(Resource.Id.combat_empty)!.Visibility = count == 0 ? ViewStates.Visible : ViewStates.Gone;
        ListView list = FindViewById<ListView>(Resource.Id.combat_list)!;
        list.Visibility = count == 0 ? ViewStates.Gone : ViewStates.Visible;
        list.Adapter = new CombatParticipantListAdapter(this, _combatRoster.Participants, _combatRoster.ActiveParticipant?.Sequence);
        bool initiativeReady = count > 0 && _combatRoster.Participants.All(participant => participant.Initiative.HasValue);
        FindViewById<LinearLayout>(Resource.Id.turn_controls)!.Visibility = count == 0 ? ViewStates.Gone : ViewStates.Visible;
        FindViewById<Button>(Resource.Id.next_turn_button)!.Enabled = initiativeReady;
        FindViewById<Button>(Resource.Id.previous_turn_button)!.Enabled = initiativeReady;
        FindViewById<TextView>(Resource.Id.round_status)!.Text = !initiativeReady
            ? "Set initiative for all combatants"
            : _combatRoster.Round == 0 ? "Ready to start" : $"Round {_combatRoster.Round}";
    }

    private void ShowCombatParticipant(CombatParticipant participant)
    {
        View actions = LayoutInflater.Inflate(Resource.Layout.combat_participant_actions, null)!;
        actions.FindViewById<TextView>(Resource.Id.combatant_details)!.Text =
            $"CR {participant.ChallengeRating}\nHP {participant.CurrentHP} / {participant.MaximumHP}";
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(participant.DisplayName);
        builder.SetView(actions);
        builder.SetNegativeButton(Resource.String.remove_from_combat, (_, _) =>
        {
            _combatRoster.Remove(participant.Sequence);
            CommitCombatChange();
        });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        AlertDialog? dialog = builder.Show();
        actions.FindViewById<Button>(Resource.Id.damage_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowHpPrompt(participant, true);
        };
        actions.FindViewById<Button>(Resource.Id.heal_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowHpPrompt(participant, false);
        };
        actions.FindViewById<Button>(Resource.Id.set_initiative_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowInitiativePrompt(participant);
        };
        Button edit = actions.FindViewById<Button>(Resource.Id.edit_combatant_button)!;
        edit.Visibility = participant.IsManual ? ViewStates.Visible : ViewStates.Gone;
        edit.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowEditCombatantDialog(participant);
        };
        actions.FindViewById<Button>(Resource.Id.reset_hp_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            _combatRoster.ResetHp(participant.Sequence);
            CommitCombatChange();
        };
    }

    private void ShowHpPrompt(CombatParticipant participant, bool damage)
    {
        var input = new EditText(this) { InputType = global::Android.Text.InputTypes.ClassNumber };
        int padding = (int)(24 * Resources!.DisplayMetrics!.Density);
        var container = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical };
        container.SetPadding(padding, 0, padding, 0);
        container.AddView(input);

        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(participant.DisplayName);
        builder.SetMessage(damage ? Resource.String.damage_prompt : Resource.String.healing_prompt);
        builder.SetView(container);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (int.TryParse(input.Text, out int amount) && amount >= 0)
            {
                if (damage) _combatRoster.ApplyDamage(participant.Sequence, amount);
                else _combatRoster.ApplyHealing(participant.Sequence, amount);
                CommitCombatChange();
            }
            else Toast.MakeText(this, Resource.String.invalid_hp_amount, ToastLength.Short)?.Show();
        });
        builder.Show();
        input.RequestFocus();
    }

    private void ShowAddCombatantDialog()
    {
        View view = LayoutInflater.Inflate(Resource.Layout.manual_combatant_dialog, null)!;
        EditText name = view.FindViewById<EditText>(Resource.Id.manual_name)!;
        EditText hp = view.FindViewById<EditText>(Resource.Id.manual_hp)!;
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.add_combatant_title);
        builder.SetView(view);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(name.Text) && int.TryParse(hp.Text, out int maximumHp) && maximumHp >= 1)
            {
                _combatRoster.AddManual(name.Text, maximumHp);
                CommitCombatChange();
            }
            else Toast.MakeText(this, Resource.String.invalid_combatant, ToastLength.Short)?.Show();
        });
        builder.Show();
        name.RequestFocus();
    }

    private void ShowEditCombatantDialog(CombatParticipant participant)
    {
        View view = LayoutInflater.Inflate(Resource.Layout.edit_manual_combatant_dialog, null)!;
        EditText name = view.FindViewById<EditText>(Resource.Id.edit_manual_name)!;
        EditText maximumHp = view.FindViewById<EditText>(Resource.Id.edit_manual_max_hp)!;
        EditText currentHp = view.FindViewById<EditText>(Resource.Id.edit_manual_current_hp)!;
        name.Text = participant.Name;
        maximumHp.Text = participant.MaximumHP.ToString();
        currentHp.Text = participant.CurrentHP.ToString();
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.edit_combatant_title);
        builder.SetView(view);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (int.TryParse(maximumHp.Text, out int maximum) && int.TryParse(currentHp.Text, out int current) &&
                _combatRoster.UpdateManual(participant.Sequence, name.Text ?? string.Empty, maximum, current))
                CommitCombatChange();
            else Toast.MakeText(this, Resource.String.invalid_combatant, ToastLength.Short)?.Show();
        });
        builder.Show();
        name.RequestFocus();
        name.SetSelectAllOnFocus(true);
    }

    private void ShowInitiativePrompt(CombatParticipant participant)
    {
        var input = new EditText(this)
        {
            InputType = global::Android.Text.InputTypes.ClassNumber | global::Android.Text.InputTypes.NumberFlagSigned,
            Text = participant.Initiative?.ToString() ?? string.Empty
        };
        input.SetSelectAllOnFocus(true);
        int padding = (int)(24 * Resources!.DisplayMetrics!.Density);
        var container = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical };
        container.SetPadding(padding, 0, padding, 0);
        container.AddView(input);

        var dialog = new AlertDialog.Builder(this);
        dialog.SetTitle(participant.DisplayName);
        dialog.SetMessage(Resource.String.initiative_prompt);
        dialog.SetView(container);
        dialog.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (int.TryParse(input.Text, out int initiative))
            {
                _combatRoster.SetInitiative(participant.Sequence, initiative);
                CommitCombatChange();
            }
            else Toast.MakeText(this, "Enter a whole number for initiative.", ToastLength.Short)?.Show();
        });
        dialog.Show();
        input.RequestFocus();
    }

    private void ConfirmClearCombat()
    {
        if (_combatRoster.Participants.Count == 0) return;
        var dialog = new AlertDialog.Builder(this);
        dialog.SetTitle(Resource.String.clear_encounter_title);
        dialog.SetMessage(Resource.String.clear_encounter_message);
        dialog.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.clear_encounter, (_, _) =>
        {
            _combatRoster.Clear();
            CommitCombatChange();
        });
        dialog.Show();
    }

    private CombatRoster LoadPersistedCombatRoster()
    {
        try
        {
            if (!(FileList()?.Contains(EncounterFileName) ?? false)) return new CombatRoster();
            using Stream stream = OpenFileInput(EncounterFileName)!;
            if (CombatRoster.TryLoad(stream, out CombatRoster roster)) return roster;
            DeleteFile(EncounterFileName);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to restore encounter: " + exception);
        }
        return new CombatRoster();
    }

    private void CommitCombatChange()
    {
        try
        {
            using Stream stream = OpenFileOutput(EncounterFileName, global::Android.Content.FileCreationMode.Private)!;
            _combatRoster.Save(stream);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to save encounter: " + exception);
        }
        RefreshCombatRoster();
    }

    private async Task ShowFullDetailsAsync(CreatureSummary creature)
    {
        var loadingBuilder = new AlertDialog.Builder(this);
        loadingBuilder.SetMessage(Resource.String.loading_details);
        loadingBuilder.SetCancelable(false);
        AlertDialog? loading = loadingBuilder.Show();
        try
        {
            if (!_detailCache.TryGetValue(creature.Id, out CreatureDetails? details))
            {
                details = await Task.Run(() =>
                {
                    using Stream stream = Assets!.Open("Bestiary.xml");
                    return CreatureDetails.Find(stream, creature.Id);
                });
                if (details is not null) _detailCache[creature.Id] = details;
            }

            loading?.Dismiss();
            if (IsDestroyed) return;
            var dialog = new AlertDialog.Builder(this);
            dialog.SetTitle(creature.Name);
            dialog.SetMessage(details is null ? GetString(Resource.String.details_not_found) : FormatFullDetails(details));
            dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
            dialog.Show();
        }
        catch (Exception exception)
        {
            loading?.Dismiss();
            global::Android.Util.Log.Error("CombatManager", exception.ToString());
            Toast.MakeText(this, Resource.String.details_not_found, ToastLength.Long)?.Show();
        }
    }

    private static string FormatFullDetails(CreatureDetails details)
    {
        var sections = new List<string>();
        AddSection(sections, null, details.VisualDescription);
        AddSection(sections, "ABILITY SCORES", details.AbilityScores);
        AddSection(sections, "FEATS", details.Feats);
        AddSection(sections, "SKILLS", details.Skills);
        AddSection(sections, "LANGUAGES", details.Languages);
        AddSection(sections, "SPECIAL ATTACKS", details.SpecialAttacks);
        AddSection(sections, "SPECIAL ABILITIES", details.SpecialAbilities);
        AddSection(sections, "ENVIRONMENT", details.Environment);
        AddSection(sections, "ORGANIZATION", details.Organization);
        AddSection(sections, "TREASURE", details.Treasure);
        AddSection(sections, "DESCRIPTION", details.Description);
        return string.Join("\n\n", sections);
    }

    private static void AddSection(List<string> sections, string? heading, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sections.Add(heading is null ? value : heading + "\n" + value);
    }

    private static string ValueOrDash(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private sealed record Page(int ButtonId, string Title, string Initial);
}
