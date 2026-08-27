# Combat Manager Android modernization plan

## Objective

Migrate the legacy Xamarin.Android application to native .NET for Android while preserving its Activities, Fragments, dialogs, adapters, AXML layouts, application identity, user data, and behavior. Keep the legacy projects intact as a reference until the replacement reaches functional parity.

## Working rules

- Perform all work on `modernize/android` until the migration is ready to merge.
- Make incremental, reviewable changes and build after each feature slice.
- Update [PROGRESS.md](PROGRESS.md) whenever a task changes state.
- Record consequential technical and product choices in [DECISIONS.md](DECISIONS.md).
- Do not retire or destructively rewrite the Xamarin projects before parity is confirmed.
- Do not store signing passwords or other secrets in tracked files.

## Phase 1: Establish a baseline

- [x] Create the dedicated migration branch.
- [x] Keep the Xamarin projects unchanged as a reference.
- [x] Record application identity, versions, API levels, permissions, data files, screen structure, storage usage, and service behavior.
- [ ] Complete the manual smoke-test checklist against a runnable legacy build or known-good installation.
- [ ] Capture reference screenshots for important screens and layout variants.

Deliverable: a reference checklist and evidence against which the migrated application can be tested.

## Phase 2: Install the modern build environment

- [x] Install the current supported .NET SDK.
- [x] Install the Android workload with `dotnet workload install android`.
- [x] Verify `dotnet --info` and `dotnet workload list`.
- [x] Confirm the JDK, Android SDK, build tools, and `adb` are available.
- [x] Configure an emulator or a physical device with USB debugging.
- [x] Build a blank .NET Android application from the VS Code terminal.
- [x] Install the blank application on an emulator or physical device.

Deliverable: a working sample APK produced from VS Code.

## Phase 3: Introduce SDK-style projects

- [x] Create `CombatManagerCore.Android` alongside the legacy core project.
- [x] Create `CombatManager.Android` alongside `CombatManagerDroid`.
- [x] Select a currently supported `net*-android` target framework.
- [x] Preserve application ID `com.kyleolson.combatmanager`.
- [x] Confirm both empty projects restore and compile.

Deliverable: empty modern projects that build successfully.

## Phase 4: Port the core library

- [ ] Add the required `CombatManagerCore` source to the modern core project. (Four slices linked, including attacks, afflictions, initiative values, and load-independent registries; migration ongoing.)
- [ ] Replace `packages.config` with `PackageReference` entries.
- [ ] Remove obsolete compatibility packages supplied by modern .NET.
- [ ] Update Newtonsoft.Json, EmbedIO, Swan.Lite, SQLite, ZIP, and Xamarin.Essentials functionality.
- [ ] Resolve incompatibilities involving Mono.Data.Sqlite, serialization, System.Web.Services, System.ServiceModel, and platform-specific paths.
- [ ] Add focused tests for calculations, serialization, database queries, and import/export parsing. (34 utility/value/dice/attack/affliction/initiative/read-only-data/combat-roster tests added; broader database/import tests remain.)

Deliverable: the shared core project builds independently.

## Phase 5: Port Android resources

- [ ] Move or link AXML layouts, drawables, icons, XML resources, strings, colours, raw assets, and layout variants. (Modern home-navigation layout, palette, styles, and drawable added; feature resources remain.)
- [ ] Exclude the checked-in legacy `Resource.designer.cs`; let modern tooling generate resources.
- [ ] Correct resource filenames that violate current Android rules.
- [ ] Resolve generated resource-name differences.
- [ ] Introduce a compatible modern theme while initially preserving appearance. (Initial Material/legacy-colour navigation shell runs on API 36; feature styling remains.)

Deliverable: all Android resources compile.

## Phase 6: Modernize the manifest

- [ ] Preserve the existing application ID.
- [ ] Target the current required Android API level.
- [ ] Add explicit `android:exported` values where required.
- [ ] Review launcher activity attributes.
- [ ] Replace obsolete storage permissions.
- [ ] Configure `FileProvider` for file sharing.
- [ ] Review clear-text networking requirements for the local web server.
- [ ] Preserve labels, icons, versioning, and appropriate device support.

Deliverable: a valid manifest accepted by current Android tooling.

## Phase 7: Port the UI in feature slices

- [ ] Loading and startup activity. (Direct startup and asynchronous bundled-bestiary loading work; full legacy initialization remains.)
- [x] Home activity and navigation.
- [ ] Lookup and list fragments. (Searchable native lists are working for Monsters, Feats, and Spells.)
- [ ] Combat screen. (Monster/manual entry, duplication, manual editing, full-HP reset, notes/conditions, duplicate naming, damage/healing, defeated state, initiative/turn/round tracking, private persistence, removal, and confirmed clear are complete; structured conditions remain.)
- [ ] Character and initiative adapters. (Modern combat adapter displays initiative and active combatant; legacy character/condition behavior remains.)
- [x] Monster browsing and selection. (Searchable/filterable complete 2,837-entry legacy bestiary, structured rows, persisted browser state, lazy full record details, and encounter selection are complete.)
- [ ] Monster editor screens.
- [x] Spell and feat screens. (Searchable/filterable Feats and Spells screens with persisted browser state and read-only details are complete; Spells stream full records on demand.)
- [x] Treasure and rule screens. (Searchable/filterable Rules and 2,241-entry magic-item Treasure screens with persisted state and full details complete.)
- [ ] Remaining dialogs and utilities.
- [ ] Replace Android Support APIs with AndroidX throughout.
- [ ] Update obsolete lifecycle and dialog APIs.

Deliverable: all screens open and navigate without crashes.

## Phase 8: Modernize data and file handling

- [ ] Select and validate a modern SQLite provider.
- [ ] Preserve existing database compatibility wherever possible.
- [ ] Copy bundled databases into application-owned storage. (Modern active-encounter state now uses application-private storage; legacy database migration remains.)
- [ ] Test schema compatibility and upgrades.
- [ ] Replace direct external-storage access with app-private storage, the document picker, content URIs, and `FileProvider` as appropriate.
- [ ] Update import/export for scoped storage.
- [ ] Test files produced by the legacy application.
- [ ] Define a backup path before any user-data schema migration.

Deliverable: existing data loads and import/export works on current Android.

## Phase 9: Services, networking, and notifications

- [ ] Port the EmbedIO-based local HTTP service.
- [ ] Decide whether the service must operate while backgrounded.
- [ ] Implement a foreground service and persistent notification if required.
- [ ] Add notification channels.
- [ ] Replace obsolete network-state detection.
- [ ] Test foreground, background, locked-screen, and network-change behavior.

Deliverable: local web and remote-control functionality behaves predictably.

## Phase 10: Produce the first Debug APK

- [ ] Build or publish `CombatManager.Android` in Debug configuration.
- [ ] Install the generated APK with `adb`.
- [ ] Capture startup logs.
- [ ] Run the baseline smoke-test checklist.
- [ ] Fix startup/database, navigation, combat, editor, import/export, and service failures in that order.

Deliverable: a usable Debug APK installed on a real device.

## Phase 11: Android compatibility testing

- [ ] Test the minimum supported Android version.
- [ ] Test a current Android version.
- [ ] Test phone and tablet layouts.
- [ ] Test portrait and landscape layouts.
- [ ] Test clean installation and, if possible, upgrade installation.
- [ ] Test denied permissions, missing/corrupt databases, process termination, and state restoration.

Deliverable: documented results and a prioritized defect list.

## Phase 12: Release signing and packaging

- [ ] Remove signing passwords from project files.
- [ ] Rotate exposed credentials if they have been published.
- [ ] Store signing secrets outside tracked files.
- [ ] Determine whether the existing keystore must be retained for upgrades.
- [ ] Produce a signed APK and, if required, an AAB.
- [ ] Verify the signature and install the Release build on a clean device.

Deliverable: signed release packages.

## Phase 13: VS Code workflow and cleanup

- [ ] Add VS Code tasks for restore, build, publish, APK installation, launch, filtered `adb logcat`, and clean.
- [ ] Document the workflow in the repository README.
- [ ] Remove generated artifacts and secrets from source control.
- [ ] Retire legacy Xamarin projects only after regression approval.

Deliverable: a documented and repeatable VS Code development and release workflow.

## Milestones

- **M1:** Toolchain installed and blank APK builds.
- **M2:** Core library compiles.
- **M3:** Resources and startup screen compile.
- **M4:** Main navigation and read-only screens work.
- **M5:** Combat and editor workflows work.
- **M6:** Database and import/export are compatible.
- **M7:** Services and permissions work.
- **M8:** Signed release APK/AAB is produced.

The initial implementation target is M1 through M3.
