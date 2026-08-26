# Android modernization progress

Last updated: 2026-08-26

## Current status

- Branch: `modernize/android`
- Active milestone: M2 - begin core-library migration
- Overall state: M1 complete; toolchain, emulator, build, installation, and launch verified
- Legacy source changes: none

## Completed

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

## In progress

- First usable vertical slice complete: the installed API 36 app can browse, search, and inspect summary details for 1,000 bundled creatures.

## Blockers and required actions

- No current implementation blocker. The next work is the first core-library migration slice.

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

1. Expand creature details using the full bestiary data without importing the legacy database graph.
2. Add CR/type filters and improve the list row presentation.
3. Decide whether to combine `BestiaryShort2.xml` for broader bundled coverage before introducing custom SQLite creatures.
4. Begin the next read-only destination after the Monsters slice is acceptance-tested.

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
