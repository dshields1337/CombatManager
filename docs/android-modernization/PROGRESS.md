# Android modernization progress

Last updated: 2026-08-27

## Current status

- Branch: `modernize/android`
- Active milestone: M4 - main navigation and read-only screens
- Overall state: M1 complete; Monsters, Feats, and Spells are usable read-only destinations in the emulator
- Legacy source changes: none

## Completed

### 2026-08-27 (continued)

- Reorganized the growing participant action dialog into a scrollable, grouped action sheet covering health, turn/status, and participant operations.
- Added initiative and timed-condition status to the action-sheet summary and confirmation before removing an individual combatant.
- Latest core result remains 37 passed, 0 failed, 0 skipped; a clean Debug Android build succeeded with 0 warnings and 0 errors.
- API 36 verification confirmed all three action groups, correct Android resource mapping after a clean package rebuild, and a live application process.
- Corrected the remaining modern UI encoding artifacts in Rules/Treasure row separators and Spell/Rule/Treasure loading messages; valid Unicode bullets and ellipses now render consistently.
- Added one-dialog initiative setup for the entire encounter, with atomic core validation and deterministic sorting after all values are accepted.
- Reworked the turn controls into a two-row layout so Previous, Initiative, Reset, and Next retain equal, usable touch targets.
- Latest core result: 38 passed, 0 failed, 0 skipped; Debug Android build succeeded with 0 warnings and 0 errors.
- API 36 verification entered initiative 18 through the bulk dialog, updated the participant row and Ready to start state, and confirmed the process remained alive.
- Added a platform-neutral projection of all 34 legacy Pathfinder conditions with their complete rules descriptions, without importing the legacy spell/favourites condition graph.
- Bundled the existing condition catalogue and added an alphabetical preset picker to timed-condition entry while retaining custom names.
- Latest core result: 39 passed, 0 failed, 0 skipped; Debug Android build succeeded with 0 warnings and 0 errors.
- API 36 verification confirmed the condition dialog, Custom condition option, and legacy presets including Bleed, Blinded, Broken, and Confused.
- Added active-condition action menus and full rules dialogs for standard catalogue conditions; custom conditions omit the unavailable rules action.
- Added validated in-place editing of an active condition's preset/custom name and remaining duration, including persistence coverage.
- Latest core result: 40 passed, 0 failed, 0 skipped; Debug Android build succeeded with 0 warnings and 0 errors.
- API 36 verification reopened persisted `Bleed (3)`, prefilled its standard name and duration, edited it to `Bleed (5)`, refreshed the combat row, and left the process alive.
- Added untimed conditions using duration `0`; they display without a countdown, survive completed turns and persistence, and remain until explicitly removed or edited.
- Latest core result: 41 passed, 0 failed, 0 skipped.
- API 36 verification added `Prone` with duration `0`, rendered it without `(0)`, persisted the encounter mutation, and left the process alive.
- Added confirmed participant-level clearing of all structured conditions without changing notes or other encounter state.
- Latest core result: 42 passed, 0 failed, 0 skipped; Debug Android build succeeded with 0 warnings and 0 errors.
- API 36 verification showed the new clear-all action for `Prone`, required confirmation naming the participant, removed the condition while retaining the participant, and left the process alive.
- Projected bestiary initiative modifiers into encounter participants and version-1 persistence, including negative modifiers and duplicate participants.
- Added one-tap d20 initiative rolling for every bestiary combatant from the bulk initiative dialog; manual players remain user-entered and monster modifier labels are visible beside their fields.
- Latest core result: 43 passed, 0 failed, 0 skipped.
- API 36 verification added a bundled Goblin, displayed its `+6` modifier in bulk initiative, rolled it to 22, left the manual participant blank, persisted the roll, and kept the process alive.
- Added persistent temporary HP, explicit row/action-sheet display, and a setter that accepts `0` to clear it. Damage consumes temporary HP before current HP; healing does not restore temporary HP.
- Encounter summaries now include temporary HP; latest core result is 44 passed, 0 failed, 0 skipped, and the Debug Android build succeeded with 0 warnings and 0 errors.
- API 36 verification gave Goblin 5 temporary HP, displayed `HP 6 / 6 + 5 temp`, applied 3 damage, retained normal HP at 6 / 6, reduced temporary HP to 2, and left the process alive.

### 2026-08-26

- Created the dedicated migration branch.
- Added the tracked modernization plan, progress log, decision log, and baseline.
- Confirmed the repository starts with a clean worktree.
- Confirmed this machine currently has no `dotnet`, MSBuild, Xamarin.Android targets, JDK/Android environment variables, or restored repository-level `packages` directory.
- Inventoried the principal Android UI, storage, database, dependency, permission, and embedded-server migration areas.
- Installed .NET SDK 10.0.400 and pinned it in `global.json` with latest-patch roll-forward.
- Installed Android workload 36.1.69 for .NET 10.
- Installed user-local Android SDK and JDK dependencies under `C:\Users\dave8\AppData\Local\Android`.
- Verified Microsoft OpenJDK 17.0.14, Android platform tools, and `adb`.
- Generated and built a temporary `net10.0-android` smoke-test application.
- Smoke build result: succeeded with zero warnings and zero errors; Debug APKs were produced.
- Checked `adb devices`; no emulator or physical device was connected.
- Created `CombatManagerModern.slnx` with parallel `CombatManager.Android` and `CombatManagerCore.Android` projects.
- Configured `CombatManager.Android` for `net10.0-android`, package ID `com.kyleolson.combatmanager`, version name 1.27, and version code 41.
- Added portable MSBuild discovery of the user-local Android SDK and JDK.
- Added initial VS Code tasks for restore, Debug APK build, and connected-device listing.
- Corrected the initial SDK/JDK property condition after the first solution build showed that `Directory.Build.props` is imported before the Android target framework can be inspected.
- Built `CombatManagerModern.slnx` successfully with zero warnings and zero errors.
- Produced `com.kyleolson.combatmanager.apk` and its Debug-signed counterpart from the parallel application shell.
- Completed the Phase 3 project-scaffolding deliverable.
- Installed the official Android Emulator and Android 16/API 36 Google APIs x86_64 system image.
- Created a Pixel 7 profile named `CombatManager_API_36`.
- Verified Windows Hypervisor Platform acceleration is installed and usable.
- Booted the AVD and confirmed it appears to `adb` as `emulator-5554`.
- Added VS Code tasks to start the emulator, build and install the Debug APK, launch Combat Manager, and stream process logs.
- Installed the first shell APK on emulator `emulator-5554`.
- Diagnosed an initial native abort caused by direct installation of a fast-deployment Debug APK without its external assembly payload.
- Configured `EmbedAssembliesIntoApk=true` so terminal-built Debug APKs are self-contained and installable with plain `adb`.
- Rebuilt with zero warnings and zero errors, reinstalled successfully, and launched the shell MainActivity.
- Verified the shell process remained alive as the foreground activity on Android 16/API 36.
- Completed milestone M1.
- Linked the first platform-neutral legacy core slice into `CombatManagerCore.Android` without duplicating source files.
- The first slice contains `CMMathUtilities`, `CMListUtilities`, `CMStringUtilities`, `Coin`, `InsensitiveComparer`, `RandomWeightChart`, `RomanNumbers`, `SizeMods`, `Stat`, `StringCapitalizer`, and `TitleValuePair`.
- Resolved the slice's only missing internal dependency by adding `CMListUtilities`, which supplies `WeaveList`.
- Added the full `DieRoll`, `DieStep`, and roll-result engine after removing an unused `System.ServiceModel` import.
- Added `CombatManagerCore.Android.Tests` to the modern solution.
- Added seven regression tests covering clamp behavior, decomma normalization, Roman numerals, coin parsing/value calculation, size boundaries, compound dice parsing, and dice-result bounds.
- Latest core result: 7 passed, 0 failed, 0 skipped, 0 warnings.
- Added a VS Code task named `Core: run migration tests`.
- Full `CombatManagerModern.slnx` Debug build after the first core slice: succeeded with 0 warnings and 0 errors.
- Added the next closed model slice: `ConditionBonus`, `SkillValue`, `SpecialAbility`, `CharacterClass`, `CreatureTypeInfo`, `PropertyConverters`, and `SourceInfo`.
- Added six regression tests for condition-bonus cloning, skill parsing/formatting, special-ability type mapping/cloning, class-name mapping, creature BAB/save rules, and source aliases.
- The new skill test exposed a legacy greedy-regex bug that discarded parenthesized skill subtypes; anchored/non-greedy parsing now preserves values such as `Knowledge (Arcana)`.
- Latest core result: 13 passed, 0 failed, 0 skipped, 0 warnings.
- Mapped the attack boundary: `Attack` currently depends on the large `Monster` class for text/parsing helpers and on XML-backed `Weapon`/`WeaponSpecialAbility` registries. These concerns must be separated before attacks can join the platform-neutral model cleanly.
- Full modern Android solution after the second model slice: succeeded with 0 warnings and 0 errors.
- Extracted attack parsing and formatting dependencies from `Monster` into the platform-neutral `CombatText` helper.
- Added load-independent, explicitly seedable registries to `Weapon` and `WeaponSpecialAbility`; modern builds start with empty registries while the legacy build retains XML loading.
- Linked `Attack`, `AttackSet`, `Weapon`, and `WeaponSpecialAbility` into the modern core without linking `Monster`, `XMLLoader`, SQLite, or Android code.
- Added attack regression tests covering regex parsing, text round-tripping, seeded weapon resolution, hand counting, and cloning.
- Latest core result: 15 passed, 0 failed, 0 skipped, 0 warnings.
- Full modern Android solution after the attack slice: succeeded with 0 warnings and 0 errors.
- Mapped the condition/effect boundary and confirmed that the full `Condition` model still combines plain state with spells, monsters, XML-backed favorites/recents, and persistence.
- Selected and linked the closed `Affliction` and `InitiativeCount` slice instead of importing that full dependency graph.
- Added a platform-neutral `Affliction.FromSpecialAbility(string sourceName, SpecialAbility)` entry point; legacy builds retain the existing `Monster` overload as a delegating compatibility API.
- Reused `CombatText` for affliction dice parsing, removing the modern slice's remaining `Monster` dependency.
- Fixed affliction duration-limit parsing so plural units normalize consistently, and fixed secondary-damage formatting to use the secondary die/type with the missing `and` separator.
- Added three regression tests for affliction parsing, limited duration, secondary damage, deep cloning, and initiative ordering.
- Latest core result: 18 passed, 0 failed, 0 skipped, 0 warnings.
- Full modern Android solution after the affliction/initiative slice: succeeded with 0 warnings and 0 errors.
- Pivoted from broad core expansion to the first visible vertical slice so emulator feedback arrives earlier.
- Replaced the placeholder `Hello, Android!` screen with an interactive Combat Manager home-navigation shell.
- Added working Combat, Monsters, Feats, Spells, Rules, and Treasure tabs, persisted last-tab selection, modernization status, and an About dialog.
- Added the initial modern resource palette, tab style, circular accent drawable, and user-facing strings while retaining the legacy teal colour direction.
- Fixed API 36 edge-to-edge system-bar overlap discovered during device interaction by applying system-window insets to the root layout.
- Modern Android application build: succeeded with 0 warnings and 0 errors.
- Installed the updated APK on `CombatManager_API_36`; verified tab switching, tab persistence after process restart, About dialog interaction, and a live foreground process.
- Added the platform-neutral `CreatureSummary` projection and stream-based bestiary loader instead of importing the legacy `Monster`/SQLite graph.
- Linked the existing 1.7 MB `BestiaryShort.xml` into the modern APK as a bundled Android asset.
- Implemented asynchronous bestiary loading, a searchable Monsters list, live result counts, and read-only creature summary dialogs.
- The initial bundled dataset exposes 1,000 creatures with CR, XP, alignment/type, HP/HD, AC, saves, speed, attacks, senses, and source where available.
- Added a regression test for XML projection, numeric parsing, list text, and alphabetical ordering; latest core result: 19 passed, 0 failed, 0 skipped.
- Full modern solution after the first usable vertical slice: succeeded with 0 warnings and 0 errors.
- Installed the final APK on `CombatManager_API_36`; verified 1,000-creature startup, search down to Aboleth, the Aboleth details dialog, and a live application process.
- Added platform-neutral combined bestiary filtering and numeric ordering for fractional/integer challenge ratings.
- Added native creature-type and CR filter controls that compose with the existing text search.
- Added a regression test for combined name/type/CR filtering and fractional CR ordering; latest core result: 20 passed, 0 failed, 0 skipped.
- Full modern solution and final Android APK build after filter work: succeeded with 0 warnings and 0 errors.
- Installed on `CombatManager_API_36`; verified 46 aberrations and a combined aberration/CR 7 result of Aboleth, Chuul, and Drider.
- Added the 42 MB full `Bestiary.xml` as a bundled asset and a streaming `CreatureDetails` reader that stops after finding the requested ID.
- Added on-demand full details for ability scores, feats, skills, languages, special attacks/abilities, ecology, visual description, and descriptive text, with an in-memory cache for repeat views.
- Added a focused streaming lookup regression test; latest core result: 21 passed, 0 failed, 0 skipped.
- Final Android APK size with full bestiary content: approximately 50 MB.
- Android build succeeded with 0 warnings and 0 errors; API 36 verification loaded Aboleth's full record successfully in approximately 2.9 seconds including UI polling.
- Replaced plain monster strings with structured rows showing the creature name, size/type/subtype, and a distinct CR badge.
- Persisted monster search text, creature type, and CR selections through tab changes and process restarts.
- Android build succeeded with 0 warnings and 0 errors; API 36 restart verification restored `aboleth` + `aberration` + `CR 7`, produced one result, and rendered the structured Aboleth row.
- Added the platform-neutral `FeatSummary` projection and bundled the existing 7.6 MB `Feats.xml` asset without linking the legacy `Feat`/database graph.
- Implemented the second read-only destination: 3,217 feats with structured name/type/summary rows, text search, comma-aware type filtering, and prerequisite/benefit/normal/special/source details.
- Device testing exposed the legacy XML tag spelling `<Prerequistites>` used by 2,982 records; the projection and regression test now preserve those prerequisite values.
- Latest core result: 22 passed, 0 failed, 0 skipped; full modern solution build succeeded with 0 warnings and 0 errors.
- API 36 verification passed for the full feat count, Combat type filter (1,196 records), Power Attack-family search, and a detail dialog containing prerequisites and benefit text.
- Added lightweight `SpellSummary` and streaming `SpellDetails` projections without importing the legacy database-backed `Spell` model.
- Bundled `SpellsShort.xml` for startup browsing and `Spells.xml` for lazy full-detail lookup, with successful detail records cached for the application session.
- Implemented the third read-only destination: 2,865 spells with structured rows, live text search, school filtering, summary dialogs, and full casting/rules descriptions on demand.
- Latest core result: 23 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors.
- Final signed Release APK is 19,249,276 bytes (approximately 18.4 MiB) with the bestiary, feat, and spell assets bundled.
- API 36 verification passed for the 2,865-spell count, an `Acid Arrow` one-result search, summary content, and lazy full details including casting time, range, resistance, and description; the app process remained alive.
- Added the second legacy bestiary summary bundle, `BestiaryShort2.xml`, expanding Monsters from the low-memory 1,000-record subset to the complete 2,837-record browseable catalogue used by the original app.
- Combined bestiary partitions by unique ID and alphabetical name, with regression coverage preventing accidental duplicate IDs when data partitions change.
- Latest core result: 24 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors. The signed APK is 19,663,043 bytes (approximately 18.8 MiB).
- API 36 verification passed for the 2,837-creature count and for `Globster`, a second-partition-only record, including its CR, statistics, source, and live application process.
- Persisted Feats search/type and Spells search/school selections using application preferences, completing state restoration across tab changes and process restarts for all three read-only browsers.
- Release Android build after browser-state persistence succeeded with 0 warnings and 0 errors.
- API 36 restart verification restored `Power Attack` + `Combat` to one feat and `Acid Arrow` + `conjuration` to one spell; the application process remained alive.
- Extracted the 591 Rules descriptions from the legacy 32 MB `Details.db` into a focused 4.6 MB `RuleDetails.xml` asset; the modern APK does not need the obsolete database layer for this destination.
- Added platform-neutral `RuleSummary` and streaming `RuleDetails` projections with normalization of legacy HTML fragments into readable plain text.
- Implemented the fourth read-only destination: 591 rules across 22 types with structured rows, live search, type filtering, persisted browser state, session-cached full descriptions, and rule-specific metadata.
- Latest core result: 25 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors. The signed APK is 20,707,732 bytes (approximately 19.7 MiB).
- API 36 verification passed for the full Rules count, a one-result `Grapple` search, its complete scrollable rules text without literal formatting tags, restored state, and a live application process.
- Extracted 2,241 focused magic-item detail records from the legacy shared database into a 3.2 MB `MagicItemDetails.xml` asset.
- Added platform-neutral `MagicItemSummary` and streaming `MagicItemDetails` projections, reusing legacy-markup normalization and avoiding the obsolete database runtime.
- Implemented the fifth read-only destination: 2,241 magic items with structured group/caster-level rows, live search, 12 group filters, persisted state, session-cached full descriptions, price/weight/construction data, artifacts, and intelligent-item fields where present.
- Latest core result: 26 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors. The signed APK is 21,850,742 bytes (approximately 20.8 MiB).
- API 36 verification passed for the full item count, `Dagger of Venom` search and complete details, and restart restoration of `Dagger of Venom` + `Weapon` to one result; the process remained alive.
- Added a platform-neutral in-memory `CombatRoster` with stable participant IDs, duplicate-creature numbering, starting/current HP, removal, and reset behavior.
- Replaced the Combat placeholder with the first interactive encounter screen. Monster summary dialogs can add combatants; the roster displays CR and HP, supports individual removal, and requires confirmation before clearing the encounter.
- Latest core result: 27 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors. The signed APK installed successfully on API 36.
- API 36 verification added Goblin twice as `Goblin` and `Goblin 2`, displayed both at HP 6 / 6, removed an individual participant, cleared the remainder through confirmation, restored the empty state, and left the process alive.
- Extended `CombatRoster` with initiative values, descending deterministic ordering, stable sequence-based tie handling, active-participant state, forward/backward turns, and round tracking.
- Added per-combatant initiative entry, disabled turn controls until the roster is ready, active-combatant marking, and Previous/Next controls with visible round status.
- Latest core result: 28 passed, 0 failed, 0 skipped; both Debug and Release Android builds succeeded with 0 warnings and 0 errors.
- API 36 verification set initiative in the participant workflow, enabled turn navigation, marked the active combatant, advanced through wraparound to Round 2, returned to Round 1 with Previous, and confirmed the process remained alive.
- Added platform-neutral damage and healing operations. Damage may reduce current HP below zero, while healing recovers defeated participants and caps at maximum HP.
- Reworked combatant actions into a focused dialog containing Damage, Heal, and Set Initiative controls while retaining removal and dismissal actions.
- Added explicit `DEFEATED` feedback for combatants at zero or fewer HP.
- Latest core result: 29 passed, 0 failed, 0 skipped; Debug and Release Android builds succeeded with 0 warnings and 0 errors.
- API 36 verification damaged a 6 HP Goblin by 8 to show `DEFEATED • HP -2 / 6`, healed it to 1 / 6, then verified excess healing capped at 6 / 6; the process remained alive.
- Added a versioned, explicit XML snapshot for the complete modern encounter: participant identity/naming, CR, maximum/current HP, initiative, active participant, and round.
- The Android app now saves every encounter mutation to application-private storage and restores it during activity creation. Invalid or unsupported snapshots safely fall back to an empty roster.
- Latest core result: 30 passed, 0 failed, 0 skipped, including full persistence round-trip and corrupt-data fallback coverage.
- API 36 Debug verification restored two Goblins with initiatives 20/12, Goblin at 4 / 6 HP, active marker, and Round 1 after a forced process stop.
- Replaced reflection-based serialization after Release trim analysis warned it could remove required members. The explicit XML reader/writer produced a clean Release build with 0 warnings and 0 errors.
- A stale Android packaging cache briefly produced `Invalid compressed assembly descriptor index 34`; `dotnet clean` followed by Release build corrected the package. The rebuilt signed Release APK launched successfully and restored a newly added Goblin after force-stop/relaunch.
- Added manual player/NPC combatants with required names and custom maximum HP, including case-insensitive duplicate naming and full encounter persistence.
- Latest core result: 31 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors.
- API 36 verification created `Valeros` with 24 HP and restored `Valeros`, manual CR marker, and HP 24 / 24 after force-stop/relaunch.
- Added editing for manual participant name, maximum HP, and current HP, plus a Full HP action available to both manual and bestiary combatants.
- Latest core result: 32 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors.
- API 36 verification edited the persisted Valeros entry to maximum HP 30/current HP 10, reset it to 30 / 30, and restored both edits after force-stop/relaunch.
- Added free-form per-combatant notes/conditions, displayed directly in combat rows and included in the versioned encounter snapshot.
- Latest core result: 33 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors.
- API 36 verification added `Prone poisoned` to ValerosPrime and restored the row annotation after force-stop/relaunch.
- Added participant duplication with full-health reset, unset initiative, copied notes, correct instance naming, and independent subsequent state.
- Latest core result: 34 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors.
- API 36 verification duplicated the configured manual participant as `ValerosPrime 2`, preserved notes and 30 / 30 HP, and restored both entries after force-stop/relaunch.
- Added structured timed conditions with per-participant turn durations. Conditions decrement when their participant completes a turn and automatically expire at zero.
- Timed conditions are displayed in roster rows, copied independently during duplication, and stored in the existing versioned encounter snapshot.
- Latest core result: 35 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors.
- API 36 verification added `Stunned (2)` and restored it after force-stop/relaunch; turn decrement/expiry behavior is covered by regression tests.
- Added a condition manager that lists active durations, opens additional-condition entry, and removes a selected condition through confirmation before natural expiry.
- Core remains 35 passed, 0 failed, 0 skipped with explicit removal and invalid-index coverage; Release Android build succeeded with 0 warnings and 0 errors.
- API 36 verification removed persisted `Stunned (2)` through the manager and confirmed it remained absent after force-stop/relaunch.
- Added a stable human-readable encounter summary containing round, active combatant, HP, initiative, notes, and timed conditions, plus Android text sharing through the system chooser.
- Latest core result: 36 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors.
- API 36 verification confirmed Add/Share/Clear fit the combat header and Share opens the installed system chooser targets.
- Added confirmed Reset Turns behavior that clears initiative, active participant, and round while preserving the roster, HP, notes, and timed conditions.
- Latest core result: 37 passed, 0 failed, 0 skipped; Release Android build succeeded with 0 warnings and 0 errors.
- API 36 verification reset a persisted initiative value and confirmed encounter notes/HP remained after force-stop/relaunch.
- Added distinct active-combatant and defeated-row backgrounds plus full accessibility descriptions covering identity, HP, initiative, defeated state, notes, and timed conditions.
- Release Android build remained clean with 0 warnings and 0 errors; core remains 37 passed, 0 failed, 0 skipped.
- API 36 visual/accessibility verification confirmed the pale-red defeated treatment, readable mixed roster layout, and descriptive accessibility nodes for both combatants.

## In progress

- Added optional encounter naming to the combat header, private version-1 snapshot, and shared summary while retaining backward compatibility for unnamed snapshots.
- Latest core result: 45 passed, 0 failed, 0 skipped; Debug Android build succeeded with 0 warnings and 0 errors.
- API 36 verification named the existing encounter `VaultAmbush`, displayed the name beside its combatant count, and restored it after a forced process stop; the process remained alive.

- All five planned read-only reference destinations are complete. Combat supports encounter assembly, damage/healing, defeated feedback, initiative/turn/round tracking, and restart-safe private persistence.

## Blockers and required actions

- No current implementation blocker.

## Repository ownership and publishing

- GitHub CLI authenticated as `dshields1337`.
- Commit identity uses the account-linked GitHub noreply address to keep the user's personal email private.
- The original `KyleADOlson/CombatManager` remote is retained only as the upstream source reference.
- Modernization branches are to be pushed to the user's `dshields1337` fork, never to the original repository.
- Created `https://github.com/dshields1337/CombatManager` as the user's fork.
- Configured the user's fork as `origin` and the original repository as `upstream`.
- Disabled the push URL for `upstream` as an additional safeguard.
- Pushed commit `d85029e` and branch `modernize/android` to the user's fork.

## Next actions

1. Continue vertical slices toward parity with legacy combat controls.

## Tracking convention

- Completed work is checked off in `PLAN.md` and summarized here.
- Significant choices are recorded in `DECISIONS.md` before they become difficult to reverse.
- Build/test commands and results will be recorded here, including failures that affect subsequent decisions.

## Environment record

- .NET SDK: 10.0.400
- Android workload: 36.1.69/10.0.100
- Android target platform supplied by workload: API 36
- Android SDK: `C:\Users\dave8\AppData\Local\Android\Sdk`
- Java SDK: `C:\Users\dave8\AppData\Local\Android\Jdk`
- Java version installed by dependency target: Microsoft OpenJDK 17.0.14
- Temporary smoke project: `C:\Users\dave8\AppData\Local\Temp\CombatManager.Android.Smoke`
- Smoke build command used explicit `AndroidSdkDirectory` and `JavaSdkDirectory` MSBuild properties.
