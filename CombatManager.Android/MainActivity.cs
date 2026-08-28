namespace CombatManager.Android;

using global::CombatManager;
using global::Android.Views;

[Activity(Label = "@string/app_name", Icon = "@mipmap/appicon", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
    private const string PreferenceName = "combat_manager_modern";
    private const string EncounterFileName = "active-encounter.xml";
    private const string SavedCharactersFileName = "saved-characters.xml";
    private const string SavedMonstersFileName = "saved-monsters.xml";
    private const string SavedEncountersFileName = "saved-encounters.xml";
    private const string ActiveSavedEncounterIdKey = "active_saved_encounter_id";
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
    private readonly HashSet<int> _selectedCreatureIds = [];
    private bool _selectingMonsters;
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
    private List<CombatParticipant> _sequenceParticipants = [];
    private List<CombatParticipant> _partyParticipants = [];
    private List<CombatParticipant> _encounterParticipants = [];
    private int _combatSubTab;
    private int _monsterSubTab;
    private int? _draggedCombatantSequence;
    private SavedCharacterLibrary _savedCharacters = new();
    private SavedMonsterLibrary _savedMonsters = new();
    private SavedEncounterLibrary _savedEncounters = new();
    private int _activeSavedEncounterId;
    private List<ConditionReference>? _conditionReferences;
    private readonly Page[] _pages =
    [
        new(Resource.Id.combat_button, "Combat", "C"), new(Resource.Id.monsters_button, "Monsters", "M"),
        new(Resource.Id.characters_button, "Characters", "C"),
        new(Resource.Id.encounters_button, "Encounters", "E"),
        new(Resource.Id.feats_button, "Feats", "F"), new(Resource.Id.spells_button, "Spells", "S"),
        new(Resource.Id.rules_button, "Rules", "R"), new(Resource.Id.treasure_button, "Treasure", "T")
    ];

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);
        _combatRoster = LoadPersistedCombatRoster();
        _savedCharacters = LoadSavedCharacters();
        _savedMonsters = LoadSavedMonsters();
        _savedEncounters = LoadSavedEncounters();
        _activeSavedEncounterId = GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!.GetInt(ActiveSavedEncounterIdKey, 0);
        if (_savedEncounters.Find(_activeSavedEncounterId) is null) _activeSavedEncounterId = 0;
        foreach (Page page in _pages) FindViewById<Button>(page.ButtonId)!.Click += (_, _) => SelectPage(page);
        FindViewById<ListView>(Resource.Id.combat_list)!.ItemClick += (_, args) => ShowCombatParticipant(_sequenceParticipants[args.Position]);
        FindViewById<ListView>(Resource.Id.combat_list)!.ItemLongClick += (_, args) =>
        {
            if (args.View is not null) BeginCombatantDragAtPosition(args.Position, args.View);
        };
        FindViewById<ListView>(Resource.Id.combat_list)!.Drag += (_, args) => HandleCombatantDrag(args);
        FindViewById<ListView>(Resource.Id.combat_party_list)!.ItemClick += (_, args) => ShowCombatParticipant(_partyParticipants[args.Position]);
        FindViewById<ListView>(Resource.Id.combat_monsters_list)!.ItemClick += (_, args) => ShowCombatParticipant(_encounterParticipants[args.Position]);
        FindViewById<Button>(Resource.Id.combat_sequence_tab)!.Click += (_, _) => SelectCombatSubTab(0);
        FindViewById<Button>(Resource.Id.combat_party_tab)!.Click += (_, _) => SelectCombatSubTab(1);
        FindViewById<Button>(Resource.Id.combat_monsters_tab)!.Click += (_, _) => SelectCombatSubTab(2);
        FindViewById<Button>(Resource.Id.clear_combat_button)!.Click += (_, _) => ConfirmClearCombat();
        FindViewById<Button>(Resource.Id.add_combatant_button)!.Click += (_, _) => ShowAddCombatantOptions();
        FindViewById<Button>(Resource.Id.browse_encounter_monsters_button)!.Click += (_, _) => SelectPage(_pages[1]);
        FindViewById<Button>(Resource.Id.new_saved_character_button)!.Click += (_, _) => ShowSavedCharacterEditor();
        FindViewById<ListView>(Resource.Id.saved_character_list)!.ItemClick += (_, args) => ShowSavedCharacter(_savedCharacters.Characters[args.Position]);
        FindViewById<Button>(Resource.Id.save_encounter_button)!.Click += (_, _) => PromptSaveEncounter();
        FindViewById<Button>(Resource.Id.load_encounter_button)!.Click += (_, _) => ShowSavedEncounterPicker();
        FindViewById<ListView>(Resource.Id.saved_encounter_list)!.ItemClick += (_, args) => ShowSavedEncounter(_savedEncounters.Encounters[args.Position]);
        FindViewById<Button>(Resource.Id.name_encounter_button)!.Click += (_, _) => ShowEncounterNamePrompt();
        FindViewById<Button>(Resource.Id.share_encounter_button)!.Click += (_, _) => ShareEncounter();
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
        FindViewById<Button>(Resource.Id.set_all_initiative_button)!.Click += (_, _) => StartCombat();
        FindViewById<SearchView>(Resource.Id.monster_search)!.QueryTextChange += (_, args) => OnQueryChanged(args.NewText);
        FindViewById<Spinner>(Resource.Id.monster_type_filter)!.ItemSelected += (_, _) => OnFilterChanged();
        FindViewById<Spinner>(Resource.Id.monster_cr_filter)!.ItemSelected += (_, _) => OnFilterChanged();
        FindViewById<ListView>(Resource.Id.monster_list)!.ItemClick += (_, args) => OnMonsterClicked(_visibleCreatures[args.Position]);
        FindViewById<Button>(Resource.Id.select_monsters_button)!.Click += (_, _) => OnMonsterSelectionAction();
        FindViewById<Button>(Resource.Id.cancel_monster_selection_button)!.Click += (_, _) => ExitMonsterSelection();
        FindViewById<Button>(Resource.Id.monster_bestiary_tab)!.Click += (_, _) => SelectMonsterSubTab(0);
        FindViewById<Button>(Resource.Id.monster_custom_tab)!.Click += (_, _) => SelectMonsterSubTab(1);
        FindViewById<Button>(Resource.Id.new_saved_monster_button)!.Click += (_, _) => ShowSavedMonsterEditor();
        FindViewById<ListView>(Resource.Id.saved_monster_list)!.ItemClick += (_, args) => ShowSavedMonster(_savedMonsters.Monsters[args.Position]);
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
        int savedIndex = GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!.GetInt(SelectedPageKey, 0);
        SelectPage(_pages[Math.Clamp(savedIndex, 0, _pages.Length - 1)]);
    }

    private void SelectPage(Page selected)
    {
        bool showCombat = selected.Title == "Combat";
        bool showMonsters = selected.Title == "Monsters";
        bool showCharacters = selected.Title == "Characters";
        bool showEncounters = selected.Title == "Encounters";
        bool showFeats = selected.Title == "Feats";
        bool showSpells = selected.Title == "Spells";
        bool showRules = selected.Title == "Rules";
        bool showTreasure = selected.Title == "Treasure";
        FindViewById<LinearLayout>(Resource.Id.placeholder_panel)!.Visibility = showCombat || showMonsters || showCharacters || showEncounters || showFeats || showSpells || showRules || showTreasure ? ViewStates.Gone : ViewStates.Visible;
        FindViewById<LinearLayout>(Resource.Id.combat_panel)!.Visibility = showCombat ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.monsters_panel)!.Visibility = showMonsters ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.characters_panel)!.Visibility = showCharacters ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.encounters_panel)!.Visibility = showEncounters ? ViewStates.Visible : ViewStates.Gone;
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
        if (showMonsters)
        {
            SelectMonsterSubTab(_monsterSubTab);
            if (_monsterSubTab == 0) _ = EnsureCreaturesLoadedAsync();
            else RefreshSavedMonsters();
        }
        if (showCharacters) RefreshSavedCharacters();
        if (showEncounters) RefreshSavedEncounters();
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

    private void SelectMonsterSubTab(int index)
    {
        _monsterSubTab = index == 1 ? 1 : 0;
        FindViewById<LinearLayout>(Resource.Id.monster_bestiary_panel)!.Visibility = _monsterSubTab == 0 ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.custom_monsters_panel)!.Visibility = _monsterSubTab == 1 ? ViewStates.Visible : ViewStates.Gone;
        Button bestiary = FindViewById<Button>(Resource.Id.monster_bestiary_tab)!;
        Button custom = FindViewById<Button>(Resource.Id.monster_custom_tab)!;
        bestiary.Enabled = _monsterSubTab != 0;
        custom.Enabled = _monsterSubTab != 1;
        bestiary.Alpha = _monsterSubTab == 0 ? 1f : 0.65f;
        custom.Alpha = _monsterSubTab == 1 ? 1f : 0.65f;
        if (_monsterSubTab == 0) _ = EnsureCreaturesLoadedAsync();
        else RefreshSavedMonsters();
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

        FindViewById<ListView>(Resource.Id.monster_list)!.Adapter =
            new MonsterListAdapter(this, _visibleCreatures, _selectingMonsters, _selectedCreatureIds);
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
        var dialog = new AlertDialog.Builder(this);
        dialog.SetTitle(creature.Name);
        dialog.SetMessage(FormatCombatDetails(creature));
        dialog.SetNeutralButton(Resource.String.full_details, (_, _) => _ = ShowFullDetailsAsync(creature));
        dialog.SetNegativeButton(Resource.String.add_to_combat, (_, _) => AddCreatureToCombat(creature));
        dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        dialog.Show();
    }

    private void OnMonsterClicked(CreatureSummary creature)
    {
        if (!_selectingMonsters)
        {
            ShowCreature(creature);
            return;
        }

        if (!_selectedCreatureIds.Add(creature.Id)) _selectedCreatureIds.Remove(creature.Id);
        RefreshMonsterSelectionControls();
        FilterCreatures(CurrentQuery());
    }

    private void OnMonsterSelectionAction()
    {
        if (!_selectingMonsters)
        {
            _selectingMonsters = true;
            _selectedCreatureIds.Clear();
            RefreshMonsterSelectionControls();
            FilterCreatures(CurrentQuery());
            return;
        }

        if (_selectedCreatureIds.Count == 0) return;
        CreatureSummary[] selected = _creatures!
            .Where(creature => _selectedCreatureIds.Contains(creature.Id)).ToArray();
        foreach (CreatureSummary creature in selected) _combatRoster.Add(creature);
        CommitCombatChange();
        Toast.MakeText(this, $"Added {selected.Length} monster{(selected.Length == 1 ? string.Empty : "s")} to combat.", ToastLength.Short)?.Show();
        ExitMonsterSelection();
    }

    private void ExitMonsterSelection()
    {
        _selectingMonsters = false;
        _selectedCreatureIds.Clear();
        RefreshMonsterSelectionControls();
        FilterCreatures(CurrentQuery());
    }

    private void RefreshMonsterSelectionControls()
    {
        Button action = FindViewById<Button>(Resource.Id.select_monsters_button)!;
        action.Text = _selectingMonsters
            ? GetString(Resource.String.add_selected_monsters, _selectedCreatureIds.Count)
            : GetString(Resource.String.select_monsters);
        action.Enabled = !_selectingMonsters || _selectedCreatureIds.Count > 0;
        FindViewById<Button>(Resource.Id.cancel_monster_selection_button)!.Visibility =
            _selectingMonsters ? ViewStates.Visible : ViewStates.Gone;
    }

    private void AddCreatureToCombat(CreatureSummary creature)
    {
        CombatParticipant participant = _combatRoster.Add(creature);
        CommitCombatChange();
        Toast.MakeText(this, participant.DisplayName + " added to combat.", ToastLength.Short)?.Show();
    }

    private void RefreshCombatRoster()
    {
        _sequenceParticipants = [.. _combatRoster.Participants.Where(participant => participant.IsInCombat)];
        _partyParticipants = [.. _combatRoster.Participants.Where(participant => participant.IsManual)];
        _encounterParticipants = [.. _combatRoster.Participants.Where(participant => !participant.IsManual)];
        int count = _sequenceParticipants.Count;
        int monsterCount = _encounterParticipants.Count;
        string countText = monsterCount == 1 ? "1 encounter monster" : $"{monsterCount:N0} encounter monsters";
        FindViewById<TextView>(Resource.Id.combat_count)!.Text = string.IsNullOrEmpty(_combatRoster.EncounterName)
            ? countText : $"{_combatRoster.EncounterName}  •  {countText}";
        FindViewById<TextView>(Resource.Id.combat_party_count)!.Text = _partyParticipants.Count == 1
            ? "1 persistent party member" : $"{_partyParticipants.Count:N0} persistent party members";
        FindViewById<Button>(Resource.Id.clear_combat_button)!.Enabled = monsterCount > 0;
        FindViewById<Button>(Resource.Id.share_encounter_button)!.Enabled = count > 0;
        FindViewById<Button>(Resource.Id.load_encounter_button)!.Enabled = _savedEncounters.Encounters.Count > 0;
        FindViewById<TextView>(Resource.Id.combat_empty)!.Visibility = count == 0 ? ViewStates.Visible : ViewStates.Gone;
        ListView list = FindViewById<ListView>(Resource.Id.combat_list)!;
        list.Visibility = count == 0 ? ViewStates.Gone : ViewStates.Visible;
        list.Adapter = new CombatParticipantListAdapter(this, _sequenceParticipants,
            _combatRoster.ActiveParticipant?.Sequence, true, UpdateParticipantHp,
            sequence => ShowCombatParticipant(_combatRoster.Participants.Single(participant => participant.Sequence == sequence)),
            BeginCombatantDrag);
        FindViewById<TextView>(Resource.Id.combat_party_empty)!.Visibility = _partyParticipants.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        ListView partyList = FindViewById<ListView>(Resource.Id.combat_party_list)!;
        partyList.Visibility = _partyParticipants.Count == 0 ? ViewStates.Gone : ViewStates.Visible;
        partyList.Adapter = new CombatParticipantListAdapter(this, _partyParticipants, _combatRoster.ActiveParticipant?.Sequence);
        FindViewById<TextView>(Resource.Id.combat_monsters_empty)!.Visibility = monsterCount == 0 ? ViewStates.Visible : ViewStates.Gone;
        ListView monsterList = FindViewById<ListView>(Resource.Id.combat_monsters_list)!;
        monsterList.Visibility = monsterCount == 0 ? ViewStates.Gone : ViewStates.Visible;
        monsterList.Adapter = new CombatParticipantListAdapter(this, _encounterParticipants, _combatRoster.ActiveParticipant?.Sequence);
        bool initiativeReady = count > 0 && _sequenceParticipants.All(participant => participant.Initiative.HasValue);
        FindViewById<Button>(Resource.Id.next_turn_button)!.Enabled = initiativeReady && _combatRoster.Round > 0;
        FindViewById<Button>(Resource.Id.previous_turn_button)!.Enabled = initiativeReady && _combatRoster.Round > 0;
        FindViewById<Button>(Resource.Id.set_all_initiative_button)!.Enabled = count > 0;
        Button combatButton = FindViewById<Button>(Resource.Id.set_all_initiative_button)!;
        bool inCombat = _combatRoster.Round > 0;
        combatButton.SetText(inCombat ? Resource.String.in_combat : Resource.String.start_initiative);
        combatButton.BackgroundTintList = global::Android.Content.Res.ColorStateList.ValueOf(
            new global::Android.Graphics.Color(GetColor(inCombat ? Resource.Color.combat_red : Resource.Color.combat_green)));
        FindViewById<TextView>(Resource.Id.round_status)!.Text = !initiativeReady
            ? "Not started"
            : _combatRoster.Round == 0 ? "Ready to start" : $"Round {_combatRoster.Round}";
        SelectCombatSubTab(_combatSubTab);
    }

    private void SelectCombatSubTab(int tab)
    {
        _combatSubTab = Math.Clamp(tab, 0, 2);
        FindViewById<LinearLayout>(Resource.Id.combat_sequence_panel)!.Visibility = _combatSubTab == 0 ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.combat_party_panel)!.Visibility = _combatSubTab == 1 ? ViewStates.Visible : ViewStates.Gone;
        FindViewById<LinearLayout>(Resource.Id.combat_monsters_panel)!.Visibility = _combatSubTab == 2 ? ViewStates.Visible : ViewStates.Gone;
        int[] buttons = [Resource.Id.combat_sequence_tab, Resource.Id.combat_party_tab, Resource.Id.combat_monsters_tab];
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = FindViewById<Button>(buttons[index])!;
            button.Enabled = index != _combatSubTab;
            button.Alpha = index == _combatSubTab ? 1f : 0.65f;
        }
    }

    private void UpdateParticipantHp(int sequence, int currentHp)
    {
        if (_combatRoster.SetCurrentHp(sequence, currentHp)) CommitCombatChange();
    }

    private void BeginCombatantDragAtPosition(int position, View row)
    {
        if (_combatRoster.Round <= 0 || position < 0 || position >= _sequenceParticipants.Count) return;
        BeginCombatantDrag(_sequenceParticipants[position].Sequence, row);
    }

    private void BeginCombatantDrag(int sequence, View row)
    {
        if (_combatRoster.Round <= 0 || !_sequenceParticipants.Any(participant => participant.Sequence == sequence)) return;
        _draggedCombatantSequence = sequence;
        var data = global::Android.Content.ClipData.NewPlainText("combatant", _draggedCombatantSequence.Value.ToString());
        row.StartDragAndDrop(data, new View.DragShadowBuilder(row), null, 0);
    }

    private void HandleCombatantDrag(View.DragEventArgs args)
    {
        args.Handled = true;
        if (args.Event?.Action != DragAction.Drop || !_draggedCombatantSequence.HasValue) return;
        ListView list = FindViewById<ListView>(Resource.Id.combat_list)!;
        int position = list.PointToPosition((int)args.Event.GetX(), (int)args.Event.GetY());
        if (position >= 0 && position < _sequenceParticipants.Count &&
            _combatRoster.MoveInCombatOrder(_draggedCombatantSequence.Value, _sequenceParticipants[position].Sequence))
            CommitCombatChange();
        _draggedCombatantSequence = null;
    }

    private void StartCombat()
    {
        if (_combatRoster.Round > 0)
        {
            ConfirmEndCombat();
            return;
        }
        int rolled = _combatRoster.StartCombat(() => Random.Shared.Next(1, 21));
        CommitCombatChange();
        Toast.MakeText(this, $"Rolled initiative for {rolled} combatant{(rolled == 1 ? string.Empty : "s")}. Combat started.", ToastLength.Short)?.Show();
    }

    private void ConfirmEndCombat()
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.end_combat_title);
        builder.SetMessage(Resource.String.end_combat_message);
        builder.SetNegativeButton("No", (_, _) => { });
        builder.SetPositiveButton(Resource.String.end_combat, (_, _) =>
        {
            _combatRoster.ResetTurns();
            CommitCombatChange();
        });
        builder.Show();
    }

    private void ShowEncounterNamePrompt()
    {
        var input = new EditText(this)
        {
            Hint = GetString(Resource.String.encounter_name_prompt),
            Text = _combatRoster.EncounterName
        };
        input.SetSingleLine(true);
        int padding = (int)(20 * Resources!.DisplayMetrics!.Density);
        var container = new FrameLayout(this);
        container.SetPadding(padding, 0, padding, 0);
        container.AddView(input);
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.name_encounter_title);
        builder.SetView(container);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            _combatRoster.SetEncounterName(input.Text ?? string.Empty);
            CommitCombatChange();
        });
        AlertDialog? dialog = builder.Show();
        dialog?.Window?.SetSoftInputMode(SoftInput.StateAlwaysVisible);
        input.RequestFocus();
    }

    private void ShowCombatParticipant(CombatParticipant participant)
    {
        View actions = LayoutInflater.Inflate(Resource.Layout.combat_participant_actions, null)!;
        string initiative = participant.Initiative.HasValue ? participant.Initiative.Value.ToString() : "Not set";
        string initiativeModifier = participant.InitiativeModifier >= 0 ? "+" + participant.InitiativeModifier : participant.InitiativeModifier.ToString();
        string conditionCount = participant.Conditions.Count == 0 ? "None" : participant.Conditions.Count.ToString();
        TextView quickDetails = actions.FindViewById<TextView>(Resource.Id.combatant_details)!;
        if (participant.IsManual)
        {
            string participantStats = $"HP {participant.CurrentHP} / {participant.MaximumHP}" +
                (participant.TemporaryHP > 0 ? $"  +  {participant.TemporaryHP} temporary" : string.Empty) +
                $"\nInitiative: {initiative} ({initiativeModifier} modifier)";
            SetQuickDetails(quickDetails, participantStats, conditionCount);
        }
        else if (participant.IsSavedMonster)
        {
            SetQuickDetails(quickDetails, FormatSavedMonsterQuickStats(participant), conditionCount);
        }
        else
        {
            SetQuickDetails(quickDetails, "Loading combat statistics…", conditionCount);
            _ = PopulateMonsterQuickDetailsAsync(quickDetails, participant, conditionCount);
        }
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(participant.DisplayName);
        builder.SetView(actions);
        builder.SetNegativeButton(Resource.String.remove_from_combat, (_, _) => ConfirmRemoveCombatant(participant));
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        AlertDialog? dialog = builder.Show();
        Button fullDetails = actions.FindViewById<Button>(Resource.Id.combatant_full_details_button)!;
        fullDetails.Visibility = !participant.IsManual || participant.IsSavedCharacter ? ViewStates.Visible : ViewStates.Gone;
        fullDetails.Click += (_, _) =>
        {
            dialog?.Dismiss();
            if (participant.IsSavedCharacter || participant.IsSavedMonster) ShowSavedCombatantDetails(participant);
            else _ = ShowFullDetailsAsync(new CreatureSummary { Id = participant.CreatureId, Name = participant.DisplayName });
        };
        Button combatDetails = actions.FindViewById<Button>(Resource.Id.combatant_combat_details_button)!;
        combatDetails.Visibility = participant.IsManual ? ViewStates.Gone : ViewStates.Visible;
        combatDetails.Click += (_, _) =>
        {
            dialog?.Dismiss();
            if (participant.IsSavedMonster) ShowSavedMonsterCombatDetails(participant);
            else _ = ShowCombatDetailsAsync(participant);
        };
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
        actions.FindViewById<Button>(Resource.Id.temporary_hp_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowTemporaryHpPrompt(participant);
        };
        actions.FindViewById<Button>(Resource.Id.set_initiative_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowInitiativePrompt(participant);
        };
        Button assignMini = actions.FindViewById<Button>(Resource.Id.assign_mini_button)!;
        assignMini.Visibility = participant.IsManual ? ViewStates.Gone : ViewStates.Visible;
        assignMini.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowMiniDescriptionPrompt(participant);
        };
        Button togglePartyActive = actions.FindViewById<Button>(Resource.Id.toggle_party_active_button)!;
        togglePartyActive.Visibility = participant.IsManual ? ViewStates.Visible : ViewStates.Gone;
        togglePartyActive.SetText(participant.IsPartyActive ? Resource.String.set_inactive : Resource.String.set_active);
        togglePartyActive.Click += (_, _) =>
        {
            dialog?.Dismiss();
            _combatRoster.SetPartyActive(participant.Sequence, !participant.IsPartyActive);
            _combatSubTab = 1;
            CommitCombatChange();
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
        actions.FindViewById<Button>(Resource.Id.notes_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowNotesPrompt(participant);
        };
        actions.FindViewById<Button>(Resource.Id.duplicate_combatant_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            _combatRoster.Duplicate(participant.Sequence);
            CommitCombatChange();
        };
        actions.FindViewById<Button>(Resource.Id.timed_conditions_button)!.Click += (_, _) =>
        {
            dialog?.Dismiss();
            ShowConditionManager(participant);
        };
    }

    private void ShowMiniDescriptionPrompt(CombatParticipant participant)
    {
        var input = new EditText(this)
        {
            Hint = GetString(Resource.String.mini_description),
            Text = participant.MiniDescription ?? string.Empty
        };
        input.SetSingleLine(true);
        input.SetSelectAllOnFocus(true);
        int padding = (int)(24 * Resources!.DisplayMetrics!.Density);
        var container = new FrameLayout(this);
        container.SetPadding(padding, 0, padding, 0);
        container.AddView(input);
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.assign_mini);
        builder.SetView(container);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            _combatRoster.SetMiniDescription(participant.Sequence, input.Text ?? string.Empty);
            CommitCombatChange();
        });
        builder.Show();
        input.RequestFocus();
    }

    private void ShowSavedCombatantDetails(CombatParticipant participant)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(participant.DisplayName);
        builder.SetMessage(string.IsNullOrWhiteSpace(participant.Notes) ? "No notes." : participant.Notes);
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        builder.Show();
    }

    private void ConfirmRemoveCombatant(CombatParticipant participant)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.remove_combatant_title);
        builder.SetMessage($"Remove {participant.DisplayName} from this encounter?");
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.remove_from_combat, (_, _) =>
        {
            _combatRoster.Remove(participant.Sequence);
            CommitCombatChange();
        });
        builder.Show();
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

    private void ShowTemporaryHpPrompt(CombatParticipant participant)
    {
        var input = new EditText(this)
        {
            InputType = global::Android.Text.InputTypes.ClassNumber,
            Text = participant.TemporaryHP.ToString()
        };
        input.SetSelectAllOnFocus(true);
        int padding = (int)(24 * Resources!.DisplayMetrics!.Density);
        var container = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical };
        container.SetPadding(padding, 0, padding, 0);
        container.AddView(input);
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(participant.DisplayName);
        builder.SetMessage(Resource.String.temporary_hp_prompt);
        builder.SetView(container);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (int.TryParse(input.Text, out int amount) && _combatRoster.SetTemporaryHp(participant.Sequence, amount))
                CommitCombatChange();
            else Toast.MakeText(this, Resource.String.invalid_hp_amount, ToastLength.Short)?.Show();
        });
        builder.Show();
        input.RequestFocus();
    }

    private void ShowNotesPrompt(CombatParticipant participant)
    {
        var input = new EditText(this)
        {
            InputType = global::Android.Text.InputTypes.ClassText | global::Android.Text.InputTypes.TextFlagCapSentences | global::Android.Text.InputTypes.TextFlagMultiLine,
            Text = participant.Notes ?? string.Empty,
            Gravity = global::Android.Views.GravityFlags.Top
        };
        input.SetMinLines(3);
        int padding = (int)(24 * Resources!.DisplayMetrics!.Density);
        var container = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical };
        container.SetPadding(padding, 0, padding, 0);
        container.AddView(input);
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(participant.DisplayName);
        builder.SetMessage(Resource.String.notes_prompt);
        builder.SetView(container);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            _combatRoster.SetNotes(participant.Sequence, input.Text ?? string.Empty);
            CommitCombatChange();
        });
        builder.Show();
        input.RequestFocus();
    }

    private void ShowTimedConditionDialog(CombatParticipant participant, int? conditionIndex = null)
    {
        View view = LayoutInflater.Inflate(Resource.Layout.timed_condition_dialog, null)!;
        EditText name = view.FindViewById<EditText>(Resource.Id.condition_name)!;
        EditText turns = view.FindViewById<EditText>(Resource.Id.condition_turns)!;
        Spinner preset = view.FindViewById<Spinner>(Resource.Id.condition_preset)!;
        _conditionReferences ??= LoadConditionReferences();
        string[] choices = [GetString(Resource.String.custom_condition), .. _conditionReferences.Select(item => item.Name)];
        preset.Adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerDropDownItem, choices);
        if (conditionIndex.HasValue)
        {
            CombatCondition condition = participant.Conditions[conditionIndex.Value];
            name.Text = condition.Name;
            turns.Text = condition.RemainingTurns.ToString();
            int presetIndex = Array.FindIndex(choices, choice => string.Equals(choice, condition.Name, StringComparison.OrdinalIgnoreCase));
            preset.SetSelection(Math.Max(0, presetIndex));
        }
        preset.ItemSelected += (_, args) =>
        {
            if (args.Position > 0) name.Text = choices[args.Position];
        };
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(conditionIndex.HasValue ? Resource.String.edit_condition : Resource.String.add_timed_condition);
        builder.SetView(view);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            bool validDuration = int.TryParse(turns.Text, out int duration);
            bool changed = validDuration && (conditionIndex.HasValue
                ? _combatRoster.UpdateCondition(participant.Sequence, conditionIndex.Value, name.Text ?? string.Empty, duration)
                : _combatRoster.AddCondition(participant.Sequence, name.Text ?? string.Empty, duration));
            if (changed)
                CommitCombatChange();
            else Toast.MakeText(this, Resource.String.invalid_condition, ToastLength.Short)?.Show();
        });
        builder.Show();
        name.RequestFocus();
    }

    private List<ConditionReference> LoadConditionReferences()
    {
        try
        {
            using Stream stream = Assets!.Open("Condition.xml");
            return ConditionReference.Load(stream);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to load conditions: " + exception);
            return [];
        }
    }

    private void ShowConditionManager(CombatParticipant participant)
    {
        if (participant.Conditions.Count == 0) { ShowTimedConditionDialog(participant); return; }
        string[] choices = [.. participant.Conditions.Select(condition => condition.DisplayText),
            GetString(Resource.String.add_timed_condition), GetString(Resource.String.clear_all_conditions)];
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.timed_conditions);
        builder.SetItems(choices, (_, args) =>
        {
            if (args.Which == participant.Conditions.Count) { ShowTimedConditionDialog(participant); return; }
            if (args.Which == participant.Conditions.Count + 1) { ConfirmClearConditions(participant); return; }
            ShowConditionActions(participant, args.Which);
        });
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.Show();
    }

    private void ConfirmClearConditions(CombatParticipant participant)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.clear_all_conditions_title);
        builder.SetMessage($"Remove all conditions from {participant.DisplayName}?");
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.clear_all_conditions, (_, _) =>
        {
            _combatRoster.ClearConditions(participant.Sequence);
            CommitCombatChange();
        });
        builder.Show();
    }

    private void ShowConditionActions(CombatParticipant participant, int index)
    {
        CombatCondition condition = participant.Conditions[index];
        _conditionReferences ??= LoadConditionReferences();
        ConditionReference? reference = ConditionReference.Find(_conditionReferences, condition.Name);
        var actions = new List<string>();
        if (reference is not null) actions.Add(GetString(Resource.String.view_condition_rules));
        int editIndex = actions.Count;
        actions.Add(GetString(Resource.String.edit_condition));
        actions.Add(GetString(Resource.String.remove_condition));
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(condition.DisplayText);
        builder.SetItems(actions.ToArray(), (_, args) =>
        {
            if (reference is not null && args.Which == 0) { ShowConditionRules(reference); return; }
            if (args.Which == editIndex) { ShowTimedConditionDialog(participant, index); return; }
            ConfirmRemoveCondition(participant, index);
        });
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.Show();
    }

    private void ShowConditionRules(ConditionReference condition)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(condition.Name);
        builder.SetMessage(condition.Description);
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        builder.Show();
    }

    private void ConfirmRemoveCondition(CombatParticipant participant, int index)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.remove_condition_title);
        builder.SetMessage(participant.Conditions[index].DisplayText);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.remove_condition, (_, _) =>
        {
            _combatRoster.RemoveCondition(participant.Sequence, index);
            CommitCombatChange();
        });
        builder.Show();
    }

    private void RefreshSavedCharacters()
    {
        int count = _savedCharacters.Characters.Count;
        FindViewById<TextView>(Resource.Id.saved_character_count)!.Text = count == 1 ? "1 saved character" : $"{count:N0} saved characters";
        FindViewById<TextView>(Resource.Id.saved_character_empty)!.Visibility = count == 0 ? ViewStates.Visible : ViewStates.Gone;
        ListView list = FindViewById<ListView>(Resource.Id.saved_character_list)!;
        list.Visibility = count == 0 ? ViewStates.Gone : ViewStates.Visible;
        list.Adapter = new SavedCharacterListAdapter(this, _savedCharacters.Characters);
    }

    private void RefreshSavedMonsters()
    {
        int count = _savedMonsters.Monsters.Count;
        FindViewById<TextView>(Resource.Id.saved_monster_count)!.Text = count == 1 ? "1 custom monster" : $"{count:N0} custom monsters";
        FindViewById<TextView>(Resource.Id.saved_monster_empty)!.Visibility = count == 0 ? ViewStates.Visible : ViewStates.Gone;
        ListView list = FindViewById<ListView>(Resource.Id.saved_monster_list)!;
        list.Visibility = count == 0 ? ViewStates.Gone : ViewStates.Visible;
        list.Adapter = new SavedMonsterListAdapter(this, _savedMonsters.Monsters);
    }

    private void ShowSavedMonster(SavedMonster monster)
    {
        string modifier = Signed(monster.InitiativeModifier);
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(monster.Name);
        builder.SetMessage($"HP {monster.MaximumHP}  •  AC {monster.ArmorClass}  •  FF {monster.FlatFootedArmorClass}\n" +
            $"Touch {monster.TouchArmorClass}  •  CMD {monster.CMD}  •  CMB {Signed(monster.CMB)}\n" +
            $"Initiative modifier: {modifier}\n\n" + (string.IsNullOrWhiteSpace(monster.Notes) ? "No notes." : monster.Notes));
        builder.SetNegativeButton(Resource.String.delete_monster, (_, _) => ConfirmDeleteSavedMonster(monster));
        builder.SetNeutralButton(Resource.String.add_to_combat, (_, _) => AddSavedMonsterToCombat(monster));
        builder.SetPositiveButton(Resource.String.edit_combatant, (_, _) => ShowSavedMonsterEditor(monster));
        builder.Show();
    }

    private void ShowSavedMonsterEditor(SavedMonster? monster = null)
    {
        View view = LayoutInflater.Inflate(Resource.Layout.saved_monster_dialog, null)!;
        EditText name = view.FindViewById<EditText>(Resource.Id.saved_monster_name)!;
        EditText hp = view.FindViewById<EditText>(Resource.Id.saved_monster_hp)!;
        EditText ac = view.FindViewById<EditText>(Resource.Id.saved_monster_ac)!;
        EditText touch = view.FindViewById<EditText>(Resource.Id.saved_monster_touch_ac)!;
        EditText flatFooted = view.FindViewById<EditText>(Resource.Id.saved_monster_flat_footed_ac)!;
        EditText cmd = view.FindViewById<EditText>(Resource.Id.saved_monster_cmd)!;
        EditText cmb = view.FindViewById<EditText>(Resource.Id.saved_monster_cmb)!;
        EditText initiative = view.FindViewById<EditText>(Resource.Id.saved_monster_initiative_modifier)!;
        EditText notes = view.FindViewById<EditText>(Resource.Id.saved_monster_notes)!;
        if (monster is not null)
        {
            name.Text = monster.Name;
            hp.Text = monster.MaximumHP.ToString();
            ac.Text = monster.ArmorClass.ToString();
            touch.Text = monster.TouchArmorClass.ToString();
            flatFooted.Text = monster.FlatFootedArmorClass.ToString();
            cmd.Text = monster.CMD.ToString();
            cmb.Text = monster.CMB.ToString();
            initiative.Text = monster.InitiativeModifier.ToString();
            notes.Text = monster.Notes;
        }
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(monster is null ? Resource.String.new_monster_title : Resource.String.edit_monster_title);
        builder.SetView(view);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            int maximumHp = 0, armorClass = 0, touchArmorClass = 0, flatFootedArmorClass = 0;
            int combatManeuverDefense = 0, combatManeuverBonus = 0, initiativeModifier = 0;
            bool valid = !string.IsNullOrWhiteSpace(name.Text) && int.TryParse(hp.Text, out maximumHp) && maximumHp >= 1 &&
                int.TryParse(ac.Text, out armorClass) && armorClass >= 0 &&
                int.TryParse(touch.Text, out touchArmorClass) && touchArmorClass >= 0 &&
                int.TryParse(flatFooted.Text, out flatFootedArmorClass) && flatFootedArmorClass >= 0 &&
                int.TryParse(cmd.Text, out combatManeuverDefense) && combatManeuverDefense >= 0 &&
                int.TryParse(cmb.Text, out combatManeuverBonus) && int.TryParse(initiative.Text, out initiativeModifier);
            if (!valid)
            {
                Toast.MakeText(this, Resource.String.invalid_monster, ToastLength.Long)?.Show();
                return;
            }
            if (monster is null)
                _savedMonsters.Add(name.Text, maximumHp, armorClass, touchArmorClass, flatFootedArmorClass,
                    combatManeuverDefense, combatManeuverBonus, initiativeModifier, notes.Text ?? string.Empty);
            else
                _savedMonsters.Update(monster.Id, name.Text, maximumHp, armorClass, touchArmorClass,
                    flatFootedArmorClass, combatManeuverDefense, combatManeuverBonus, initiativeModifier,
                    notes.Text ?? string.Empty);
            CommitSavedMonsters();
        });
        builder.Show();
        name.RequestFocus();
        if (monster is not null) name.SetSelectAllOnFocus(true);
    }

    private void ConfirmDeleteSavedMonster(SavedMonster monster)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.delete_monster_title);
        builder.SetMessage(Resource.String.delete_monster_message);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.delete_monster, (_, _) =>
        {
            _savedMonsters.Remove(monster.Id);
            CommitSavedMonsters();
        });
        builder.Show();
    }

    private void AddSavedMonsterToCombat(SavedMonster monster)
    {
        CombatParticipant participant = _combatRoster.AddSavedMonster(monster);
        CommitCombatChange();
        Toast.MakeText(this, participant.DisplayName + " added to encounter.", ToastLength.Short)?.Show();
    }

    private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();

    private void ShowSavedCharacter(SavedCharacter character)
    {
        string modifier = character.InitiativeModifier >= 0 ? "+" + character.InitiativeModifier : character.InitiativeModifier.ToString();
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(character.Name);
        builder.SetMessage($"Maximum HP: {character.MaximumHP}\nInitiative modifier: {modifier}\n\n" +
            (string.IsNullOrWhiteSpace(character.Notes) ? "No notes." : character.Notes));
        builder.SetNegativeButton(Resource.String.delete_character, (_, _) => ConfirmDeleteSavedCharacter(character));
        builder.SetNeutralButton(Resource.String.add_to_combat, (_, _) => AddSavedCharacterToCombat(character));
        builder.SetPositiveButton(Resource.String.edit_combatant, (_, _) => ShowSavedCharacterEditor(character));
        builder.Show();
    }

    private void ShowSavedCharacterEditor(SavedCharacter? character = null)
    {
        View view = LayoutInflater.Inflate(Resource.Layout.saved_character_dialog, null)!;
        EditText name = view.FindViewById<EditText>(Resource.Id.saved_character_name)!;
        EditText hp = view.FindViewById<EditText>(Resource.Id.saved_character_hp)!;
        EditText initiativeModifier = view.FindViewById<EditText>(Resource.Id.saved_character_initiative_modifier)!;
        EditText notes = view.FindViewById<EditText>(Resource.Id.saved_character_notes)!;
        if (character is not null)
        {
            name.Text = character.Name;
            hp.Text = character.MaximumHP.ToString();
            initiativeModifier.Text = character.InitiativeModifier.ToString();
            notes.Text = character.Notes;
        }
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(character is null ? Resource.String.new_character_title : Resource.String.edit_character_title);
        builder.SetView(view);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(name.Text) && int.TryParse(hp.Text, out int maximumHp) && maximumHp >= 1 &&
                int.TryParse(initiativeModifier.Text, out int modifier))
            {
                if (character is null) _savedCharacters.Add(name.Text, maximumHp, modifier, notes.Text ?? string.Empty);
                else _savedCharacters.Update(character.Id, name.Text, maximumHp, modifier, notes.Text ?? string.Empty);
                CommitSavedCharacters();
            }
            else Toast.MakeText(this, Resource.String.invalid_combatant, ToastLength.Short)?.Show();
        });
        builder.Show();
        name.RequestFocus();
        if (character is not null) name.SetSelectAllOnFocus(true);
    }

    private void ConfirmDeleteSavedCharacter(SavedCharacter character)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.delete_character_title);
        builder.SetMessage(Resource.String.delete_character_message);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.delete_character, (_, _) =>
        {
            _savedCharacters.Remove(character.Id);
            CommitSavedCharacters();
        });
        builder.Show();
    }

    private void AddSavedCharacterToCombat(SavedCharacter character)
    {
        CombatParticipant participant = _combatRoster.AddSavedCharacter(character);
        CommitCombatChange();
        Toast.MakeText(this, participant.DisplayName + " added to combat.", ToastLength.Short)?.Show();
    }

    private void RefreshSavedEncounters()
    {
        int count = _savedEncounters.Encounters.Count;
        FindViewById<TextView>(Resource.Id.saved_encounter_count)!.Text = count == 1 ? "1 saved encounter" : $"{count:N0} saved encounters";
        FindViewById<TextView>(Resource.Id.saved_encounter_empty)!.Visibility = count == 0 ? ViewStates.Visible : ViewStates.Gone;
        ListView list = FindViewById<ListView>(Resource.Id.saved_encounter_list)!;
        list.Visibility = count == 0 ? ViewStates.Gone : ViewStates.Visible;
        list.Adapter = new SavedEncounterListAdapter(this, _savedEncounters.Encounters);
        FindViewById<Button>(Resource.Id.load_encounter_button)!.Enabled = count > 0;
    }

    private void ShowSavedEncounter(SavedEncounter encounter)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(encounter.Name);
        builder.SetMessage("Saved encounter");
        builder.SetNegativeButton(Resource.String.delete_encounter, (_, _) => ConfirmDeleteSavedEncounter(encounter));
        builder.SetNeutralButton(Resource.String.rename_encounter, (_, _) => PromptRenameSavedEncounter(encounter));
        builder.SetPositiveButton(Resource.String.open_encounter, (_, _) => ConfirmOpenSavedEncounter(encounter));
        builder.Show();
    }

    private void ShowSavedEncounterPicker()
    {
        if (_savedEncounters.Encounters.Count == 0)
        {
            Toast.MakeText(this, Resource.String.no_saved_encounters, ToastLength.Long)?.Show();
            return;
        }
        SavedEncounter[] encounters = [.. _savedEncounters.Encounters];
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.load_encounter);
        builder.SetItems(encounters.Select(encounter => encounter.Name).ToArray(), (_, args) => ConfirmOpenSavedEncounter(encounters[args.Which]));
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.Show();
    }

    private void ConfirmOpenSavedEncounter(SavedEncounter encounter)
    {
        if (!_combatRoster.Participants.Any(participant => !participant.IsManual) && string.IsNullOrWhiteSpace(_combatRoster.EncounterName))
        {
            OpenSavedEncounter(encounter);
            return;
        }
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.save_before_switch_title);
        builder.SetMessage(Resource.String.save_before_switch_message);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetNeutralButton(Resource.String.dont_save, (_, _) => OpenSavedEncounter(encounter));
        builder.SetPositiveButton(Resource.String.save_encounter, (_, _) => PromptSaveEncounter(() => OpenSavedEncounter(encounter)));
        builder.Show();
    }

    private void OpenSavedEncounter(SavedEncounter encounter)
    {
        if (!TryDeserializeEncounter(encounter.Snapshot, out CombatRoster roster))
        {
            Toast.MakeText(this, "Unable to open this saved encounter.", ToastLength.Long)?.Show();
            return;
        }
        _combatRoster.ReplaceEncounter(roster);
        SetActiveSavedEncounter(encounter.Id);
        CommitCombatChange();
        SelectPage(_pages[0]);
    }

    private void PromptSaveEncounter(Action? afterSave = null)
    {
        SavedEncounter? existing = _savedEncounters.Find(_activeSavedEncounterId);
        var input = new EditText(this)
        {
            Hint = GetString(Resource.String.name_encounter_title),
            Text = string.IsNullOrWhiteSpace(_combatRoster.EncounterName) ? existing?.Name ?? string.Empty : _combatRoster.EncounterName
        };
        input.SetSingleLine(true);
        input.SetSelectAllOnFocus(true);
        int padding = (int)(24 * Resources!.DisplayMetrics!.Density);
        var container = new FrameLayout(this);
        container.SetPadding(padding, 0, padding, 0);
        container.AddView(input);
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.save_encounter_title);
        builder.SetView(container);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.save_encounter, (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                Toast.MakeText(this, Resource.String.encounter_name_required, ToastLength.Short)?.Show();
                return;
            }
            string name = input.Text.Trim();
            _combatRoster.SetEncounterName(name);
            string snapshot = SerializeEncounter(_combatRoster.CreateEncounterSnapshot());
            if (existing is null)
            {
                existing = _savedEncounters.Add(name, snapshot);
                SetActiveSavedEncounter(existing.Id);
            }
            else _savedEncounters.Update(existing.Id, name, snapshot);
            CommitSavedEncounters();
            CommitCombatChange();
            afterSave?.Invoke();
        });
        builder.Show();
        input.RequestFocus();
    }

    private void PromptRenameSavedEncounter(SavedEncounter encounter)
    {
        var input = new EditText(this) { Text = encounter.Name };
        input.SetSingleLine(true);
        input.SetSelectAllOnFocus(true);
        int padding = (int)(24 * Resources!.DisplayMetrics!.Density);
        var container = new FrameLayout(this);
        container.SetPadding(padding, 0, padding, 0);
        container.AddView(input);
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.rename_encounter_title);
        builder.SetView(container);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                Toast.MakeText(this, Resource.String.encounter_name_required, ToastLength.Short)?.Show();
                return;
            }
            string name = input.Text.Trim();
            if (TryDeserializeEncounter(encounter.Snapshot, out CombatRoster roster))
            {
                roster.SetEncounterName(name);
                _savedEncounters.Update(encounter.Id, name, SerializeEncounter(roster));
            }
            else _savedEncounters.Rename(encounter.Id, name);
            if (_activeSavedEncounterId == encounter.Id)
            {
                _combatRoster.SetEncounterName(name);
                CommitCombatChange();
            }
            CommitSavedEncounters();
        });
        builder.Show();
        input.RequestFocus();
    }

    private void ConfirmDeleteSavedEncounter(SavedEncounter encounter)
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.delete_encounter_title);
        builder.SetMessage(Resource.String.delete_encounter_message);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.delete_encounter, (_, _) =>
        {
            _savedEncounters.Remove(encounter.Id);
            if (_activeSavedEncounterId == encounter.Id) SetActiveSavedEncounter(0);
            CommitSavedEncounters();
        });
        builder.Show();
    }

    private void ShowAddCombatantOptions()
    {
        string[] choices = [GetString(Resource.String.saved_character), GetString(Resource.String.temporary_combatant)];
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.add_combatant_title);
        builder.SetItems(choices, (_, args) =>
        {
            if (args.Which == 0) ShowSavedCharacterPicker();
            else ShowTemporaryCombatantDialog();
        });
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.Show();
    }

    private void ShowSavedCharacterPicker()
    {
        if (_savedCharacters.Characters.Count == 0)
        {
            Toast.MakeText(this, Resource.String.no_saved_characters, ToastLength.Long)?.Show();
            return;
        }
        SavedCharacter[] characters = [.. _savedCharacters.Characters];
        bool[] selected = new bool[characters.Length];
        AlertDialog? dialog = null;
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.choose_saved_character);
        builder.SetMultiChoiceItems(characters.Select(character => character.Name).ToArray(), selected, (_, args) =>
        {
            selected[args.Which] = args.IsChecked;
            if (dialog is not null) dialog.GetButton((int)global::Android.Content.DialogButtonType.Positive)!.Enabled = selected.Any(value => value);
        });
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.add_selected_characters, (_, _) =>
        {
            int added = 0;
            for (int index = 0; index < characters.Length; index++)
            {
                if (!selected[index]) continue;
                _combatRoster.AddSavedCharacter(characters[index]);
                added++;
            }
            CommitCombatChange();
            Toast.MakeText(this, $"Added {added} character{(added == 1 ? string.Empty : "s")} to combat.", ToastLength.Short)?.Show();
        });
        dialog = builder.Show();
        dialog?.GetButton((int)global::Android.Content.DialogButtonType.Positive)!.Enabled = false;
    }

    private void ShowTemporaryCombatantDialog()
    {
        View view = LayoutInflater.Inflate(Resource.Layout.manual_combatant_dialog, null)!;
        EditText name = view.FindViewById<EditText>(Resource.Id.manual_name)!;
        EditText hp = view.FindViewById<EditText>(Resource.Id.manual_hp)!;
        EditText initiativeModifier = view.FindViewById<EditText>(Resource.Id.manual_initiative_modifier)!;
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.temporary_combatant);
        builder.SetView(view);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(name.Text) && int.TryParse(hp.Text, out int maximumHp) && maximumHp >= 1 &&
                int.TryParse(initiativeModifier.Text, out int modifier))
            {
                _combatRoster.AddManual(name.Text, maximumHp, modifier);
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
        EditText initiativeModifier = view.FindViewById<EditText>(Resource.Id.edit_manual_initiative_modifier)!;
        name.Text = participant.Name;
        maximumHp.Text = participant.MaximumHP.ToString();
        currentHp.Text = participant.CurrentHP.ToString();
        initiativeModifier.Text = participant.InitiativeModifier.ToString();
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.edit_combatant_title);
        builder.SetView(view);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            if (int.TryParse(maximumHp.Text, out int maximum) && int.TryParse(currentHp.Text, out int current) &&
                int.TryParse(initiativeModifier.Text, out int modifier) &&
                _combatRoster.UpdateManual(participant.Sequence, name.Text ?? string.Empty, maximum, current, modifier))
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

    private void ShowAllInitiativesDialog()
    {
        int padding = (int)(24 * Resources!.DisplayMetrics!.Density);
        var fields = new Dictionary<int, EditText>();
        var rows = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical };
        rows.SetPadding(padding, 0, padding, 0);
        var rollAll = new Button(this) { Text = GetString(Resource.String.roll_all_initiative) };
        rows.AddView(rollAll, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));
        foreach (CombatParticipant participant in _combatRoster.Participants.OrderBy(item => item.Sequence))
        {
            var row = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Horizontal };
            row.SetGravity(GravityFlags.CenterVertical);
            string modifier = participant.InitiativeModifier >= 0 ? "+" + participant.InitiativeModifier : participant.InitiativeModifier.ToString();
            var label = new TextView(this) { Text = participant.DisplayName + "  (" + modifier + ")" };
            row.AddView(label, new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 1));
            var input = new EditText(this)
            {
                InputType = global::Android.Text.InputTypes.ClassNumber | global::Android.Text.InputTypes.NumberFlagSigned,
                Text = participant.Initiative?.ToString() ?? string.Empty,
                Gravity = GravityFlags.Center
            };
            input.SetSelectAllOnFocus(true);
            row.AddView(input, new LinearLayout.LayoutParams((int)(88 * Resources.DisplayMetrics.Density), LinearLayout.LayoutParams.WrapContent));
            rows.AddView(row);
            fields[participant.Sequence] = input;
        }
        rollAll.Click += (_, _) =>
        {
            int rolled = _combatRoster.RollInitiatives(() => Random.Shared.Next(1, 21));
            foreach (CombatParticipant participant in _combatRoster.Participants)
                fields[participant.Sequence].Text = participant.Initiative?.ToString() ?? string.Empty;
            CommitCombatChange();
            Toast.MakeText(this, $"Rolled initiative for {rolled} combatant{(rolled == 1 ? string.Empty : "s")}.", ToastLength.Short)?.Show();
        };
        var scroll = new ScrollView(this);
        scroll.AddView(rows);
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.set_all_initiative_title);
        builder.SetMessage(Resource.String.set_all_initiative_prompt);
        builder.SetView(scroll);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) =>
        {
            var values = new Dictionary<int, int>();
            if (fields.All(pair => int.TryParse(pair.Value.Text, out int value) && AddInitiative(values, pair.Key, value)))
            {
                _combatRoster.SetInitiatives(values);
                CommitCombatChange();
            }
            else Toast.MakeText(this, Resource.String.invalid_initiatives, ToastLength.Short)?.Show();
        });
        builder.Show();
        fields.Values.FirstOrDefault()?.RequestFocus();
    }

    private static bool AddInitiative(Dictionary<int, int> values, int sequence, int initiative)
    {
        values[sequence] = initiative;
        return true;
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
            _combatRoster.ClearEncounter();
            SetActiveSavedEncounter(0);
            CommitCombatChange();
        });
        dialog.Show();
    }

    private void ConfirmResetTurns()
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.reset_turns_title);
        builder.SetMessage(Resource.String.reset_turns_message);
        builder.SetNegativeButton(global::Android.Resource.String.Cancel, (_, _) => { });
        builder.SetPositiveButton(Resource.String.reset_turns, (_, _) =>
        {
            _combatRoster.ResetTurns();
            CommitCombatChange();
        });
        builder.Show();
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

    private SavedCharacterLibrary LoadSavedCharacters()
    {
        try
        {
            if (!(FileList()?.Contains(SavedCharactersFileName) ?? false)) return new SavedCharacterLibrary();
            using Stream stream = OpenFileInput(SavedCharactersFileName)!;
            if (SavedCharacterLibrary.TryLoad(stream, out SavedCharacterLibrary library)) return library;
            DeleteFile(SavedCharactersFileName);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to restore saved characters: " + exception);
        }
        return new SavedCharacterLibrary();
    }

    private void CommitSavedCharacters()
    {
        try
        {
            using Stream stream = OpenFileOutput(SavedCharactersFileName, global::Android.Content.FileCreationMode.Private)!;
            _savedCharacters.Save(stream);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to save characters: " + exception);
        }
        RefreshSavedCharacters();
    }

    private SavedMonsterLibrary LoadSavedMonsters()
    {
        try
        {
            if (!(FileList()?.Contains(SavedMonstersFileName) ?? false)) return new SavedMonsterLibrary();
            using Stream stream = OpenFileInput(SavedMonstersFileName)!;
            if (SavedMonsterLibrary.TryLoad(stream, out SavedMonsterLibrary library)) return library;
            DeleteFile(SavedMonstersFileName);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to restore saved monsters: " + exception);
        }
        return new SavedMonsterLibrary();
    }

    private void CommitSavedMonsters()
    {
        try
        {
            using Stream stream = OpenFileOutput(SavedMonstersFileName, global::Android.Content.FileCreationMode.Private)!;
            _savedMonsters.Save(stream);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to save monsters: " + exception);
        }
        RefreshSavedMonsters();
    }

    private SavedEncounterLibrary LoadSavedEncounters()
    {
        try
        {
            if (!(FileList()?.Contains(SavedEncountersFileName) ?? false)) return new SavedEncounterLibrary();
            using Stream stream = OpenFileInput(SavedEncountersFileName)!;
            if (SavedEncounterLibrary.TryLoad(stream, out SavedEncounterLibrary library)) return library;
            DeleteFile(SavedEncountersFileName);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to restore saved encounters: " + exception);
        }
        return new SavedEncounterLibrary();
    }

    private void CommitSavedEncounters()
    {
        try
        {
            using Stream stream = OpenFileOutput(SavedEncountersFileName, global::Android.Content.FileCreationMode.Private)!;
            _savedEncounters.Save(stream);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("CombatManager", "Unable to save encounters: " + exception);
        }
        RefreshSavedEncounters();
    }

    private void SetActiveSavedEncounter(int id)
    {
        _activeSavedEncounterId = id;
        GetSharedPreferences(PreferenceName, global::Android.Content.FileCreationMode.Private)!.Edit()!
            .PutInt(ActiveSavedEncounterIdKey, id)!.Apply();
    }

    private static string SerializeEncounter(CombatRoster roster)
    {
        using var stream = new MemoryStream();
        roster.Save(stream);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static bool TryDeserializeEncounter(string snapshot, out CombatRoster roster)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(snapshot ?? string.Empty));
        return CombatRoster.TryLoad(stream, out roster);
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

    private void ShareEncounter()
    {
        var intent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionSend);
        intent.SetType("text/plain");
        intent.PutExtra(global::Android.Content.Intent.ExtraSubject, GetString(Resource.String.share_encounter));
        intent.PutExtra(global::Android.Content.Intent.ExtraText, _combatRoster.ToSummaryText());
        StartActivity(global::Android.Content.Intent.CreateChooser(intent, GetString(Resource.String.share_encounter)));
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

    private async Task ShowCombatDetailsAsync(CombatParticipant participant)
    {
        await EnsureCreaturesLoadedAsync();
        CreatureSummary? creature = _creatures?.FirstOrDefault(item => item.Id == participant.CreatureId);
        if (creature is null)
        {
            Toast.MakeText(this, Resource.String.details_not_found, ToastLength.Long)?.Show();
            return;
        }

        var dialog = new AlertDialog.Builder(this);
        dialog.SetTitle(participant.DisplayName);
        dialog.SetMessage(FormatCombatDetails(creature));
        dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        dialog.Show();
    }

    private async Task PopulateMonsterQuickDetailsAsync(TextView view, CombatParticipant participant, string conditionCount)
    {
        await EnsureCreaturesLoadedAsync();
        CreatureSummary? creature = _creatures?.FirstOrDefault(item => item.Id == participant.CreatureId);
        if (creature is null || IsDestroyed) return;

        string ac = FirstNumber(creature.AC);
        string touch = NumberAfter(creature.AC, "touch");
        string flatFooted = NumberAfter(creature.AC, "flat-footed");
        string statistics = $"AC {ac}  •  FF {flatFooted}\n" +
            $"Touch {touch}  •  CMD {ValueOrDash(creature.CMD)}\n" +
            $"CMB {ValueOrDash(creature.CMB)}";
        SetQuickDetails(view, statistics, conditionCount);
    }

    private void ShowSavedMonsterCombatDetails(CombatParticipant participant)
    {
        var dialog = new AlertDialog.Builder(this);
        dialog.SetTitle(participant.DisplayName);
        dialog.SetMessage(FormatSavedMonsterQuickStats(participant) + "\n\n" +
            (string.IsNullOrWhiteSpace(participant.Notes) ? "No notes." : participant.Notes));
        dialog.SetPositiveButton(global::Android.Resource.String.Ok, (_, _) => { });
        dialog.Show();
    }

    private static string FormatSavedMonsterQuickStats(CombatParticipant participant) =>
        $"AC {participant.ArmorClass}  •  FF {participant.FlatFootedArmorClass}\n" +
        $"Touch {participant.TouchArmorClass}  •  CMD {participant.CMD}\n" +
        $"CMB {Signed(participant.CMB)}";

    private static void SetQuickDetails(TextView view, string statistics, string conditionCount)
    {
        string conditionLine = "Conditions: " + conditionCount;
        var text = new global::Android.Text.SpannableString(statistics + "\n" + conditionLine);
        int conditionStart = statistics.Length + 1;
        text.SetSpan(new global::Android.Text.Style.StyleSpan(global::Android.Graphics.TypefaceStyle.Bold),
            conditionStart, text.Length(), global::Android.Text.SpanTypes.ExclusiveExclusive);
        view.SetText(text, TextView.BufferType.Spannable);
    }

    private static string FirstNumber(string value)
    {
        global::System.Text.RegularExpressions.Match match =
            global::System.Text.RegularExpressions.Regex.Match(value ?? string.Empty, @"-?\d+");
        return match.Success ? match.Value : "—";
    }

    private static string NumberAfter(string value, string label)
    {
        global::System.Text.RegularExpressions.Match match = global::System.Text.RegularExpressions.Regex.Match(
            value ?? string.Empty, global::System.Text.RegularExpressions.Regex.Escape(label) + @"\s+(-?\d+)",
            global::System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "—";
    }

    private static string FormatCombatDetails(CreatureSummary creature)
    {
        string type = string.Join(" ", new[] { creature.Size, creature.Type, creature.SubType }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return $"CR {creature.CR}  •  XP {creature.XP}\n{creature.Alignment} {type}\n\n" +
            $"HP {creature.HP} {creature.HD}\nAC {creature.AC}\nSaves {creature.Saves}\nSpeed {creature.Speed}\n\n" +
            $"Melee: {ValueOrDash(creature.Melee)}\nRanged: {ValueOrDash(creature.Ranged)}\n\n" +
            $"Senses: {ValueOrDash(creature.Senses)}\nSource: {ValueOrDash(creature.Source)}";
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
