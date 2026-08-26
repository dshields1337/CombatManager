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

## In progress

- Phase 4: three platform-neutral model slices complete; attack parsing and weapon lookup are now separated from Monster/XML loading.

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

1. Map the condition/effect model boundary and isolate its spell, affliction, and persistence dependencies.
2. Link the next closed condition/effect model slice with regression tests.
3. Decide how bundled weapon and special-ability XML assets will populate the modern registries when application data migration begins.
4. Continue expanding the core without pulling Android or SQLite concerns into model assemblies.

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
