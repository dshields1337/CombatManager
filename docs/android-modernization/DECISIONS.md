# Android modernization decision log

## D001: Preserve the native Android UI model

- Date: 2026-08-26
- Status: accepted
- Decision: migrate to native .NET for Android rather than rewrite the application in .NET MAUI.
- Reason: the repository contains substantial Android-specific Activity, Fragment, adapter, dialog, and AXML code. Native .NET for Android offers the smallest behavioral change and allows incremental migration.

## D002: Use parallel projects

- Date: 2026-08-26
- Status: accepted
- Decision: create new SDK-style projects alongside the Xamarin projects instead of converting the legacy project files in place.
- Reason: this keeps the old implementation available for comparison and reduces the chance of losing build metadata or resources during early migration.

## D003: Preserve application identity

- Date: 2026-08-26
- Status: accepted
- Decision: retain package ID `com.kyleolson.combatmanager` unless a later distribution requirement forces a change.
- Reason: preserving identity is required for upgrade compatibility and continuity of application-owned data.

## D004: Track progress and decisions in the repository

- Date: 2026-08-26
- Status: accepted
- Decision: maintain the plan, progress history, baseline, and consequential decisions under `docs/android-modernization`.
- Reason: the user requested that all modernization work be tracked and understandable without specialist Android knowledge.

## D005: Target .NET 10 and Android API 36 initially

- Date: 2026-08-26
- Status: accepted
- Decision: pin SDK 10.0.400 and use `net10.0-android`, whose default Android target platform is API 36.
- Reason: .NET 10 is the current supported toolchain installed for this migration, and its blank Android template has been verified locally. The target may be revisited only if a concrete dependency incompatibility requires it.

## D006: Keep Android tooling user-local

- Date: 2026-08-26
- Status: accepted
- Decision: keep the Android SDK and JDK under `C:\Users\dave8\AppData\Local\Android` and pass their locations through reproducible build configuration.
- Reason: this avoids dependence on Visual Studio and supports terminal/VS Code builds without placing tool archives in the repository.

## D007: Make Debug APKs self-contained

- Date: 2026-08-26
- Status: accepted
- Decision: set `EmbedAssembliesIntoApk=true` in the modern Android application.
- Reason: Debug fast deployment assumes an IDE/MSBuild deployment target will copy assemblies separately. The tracked VS Code workflow uses ordinary `adb install`, so its APK must contain the managed assemblies and run independently.

## D008: Publish modernization work to the user's fork

- Date: 2026-08-26
- Status: accepted
- Decision: preserve `KyleADOlson/CombatManager` as the upstream reference and push modernization work only to a fork owned by `dshields1337`.
- Reason: the user explicitly requested that no modernization work be pushed to the original repository. A fork preserves project history and attribution while giving the user independent ownership of branches and releases.

## D009: Link legacy core source during incremental migration

- Date: 2026-08-26
- Status: accepted
- Decision: add existing `CombatManagerCore` files to the modern project as linked compile items in small dependency slices instead of copying them into a second source tree.
- Reason: linking prevents source divergence, preserves legacy-project comparability, and makes each compatibility change apply to one authoritative file. Nullable analysis is initially disabled for this pre-nullable source and will be introduced incrementally rather than obscuring functional migration with thousands of annotation warnings.

## D010: Do not pull Monster or XML persistence into the attack slice implicitly

- Date: 2026-08-26
- Status: accepted
- Decision: separate attack parsing/formatting helpers and weapon lookup boundaries before linking `Attack` and `AttackSet` into the modern core.
- Reason: `Attack` currently calls static helpers on the large `Monster` model and initializes XML-backed weapon/special-ability registries. Importing that graph merely to compile attacks would mix plain combat behavior with persistence and eventually SQLite, making failures harder to isolate and test.

## D011: Seed reference-data registries explicitly in modern builds

- Date: 2026-08-26
- Status: accepted
- Decision: modern core builds initialize weapon and weapon-special-ability registries empty and expose methods to seed them from an external loader; legacy builds retain their existing XML-backed static initialization.
- Reason: attack parsing needs reference data but should not own its storage mechanism. This keeps the model testable and platform-neutral while leaving the eventual Android asset/data loader as a separate migration concern.

## D012: Port closed condition-related models before the full condition catalog

- Date: 2026-08-26
- Status: accepted
- Decision: port `Affliction` and `InitiativeCount` first, using a source-name overload to keep affliction parsing independent of `Monster`; defer the full `Condition` model until its state is separated from spell/monster catalogs and XML favorites/recent persistence.
- Reason: the smaller models contain immediately testable combat behavior and require no storage system. Linking `Condition` as-is would pull multiple global catalogs and persistence mechanisms into a platform-neutral migration slice.

## D013: Prioritize a visible read-only vertical slice

- Date: 2026-08-26
- Status: accepted
- Decision: after establishing several tested core slices, prioritize an interactive home shell followed by a read-only Monsters list/details path instead of continuing broad core migration in isolation.
- Reason: installing visible, navigable increments in the emulator provides earlier usability, exposes current-Android UI issues such as edge-to-edge insets, and gives clearer acceptance checkpoints for a non-Android specialist.

## D014: Project bundled bestiary XML into lightweight read-only models

- Date: 2026-08-26
- Status: accepted
- Decision: load `BestiaryShort.xml` from the APK into a new `CreatureSummary` projection for the first Monsters screen rather than linking the legacy `Monster`, `BaseDBClass`, `MonsterDB`, and SQLite graph.
- Reason: the existing short bestiary already provides useful browse/search/detail fields for 1,000 creatures. A stream-based projection delivers an independently testable vertical slice now and leaves full detail storage and custom-creature persistence as explicit later decisions.

## D015: Stream full creature details on demand

- Date: 2026-08-26
- Status: accepted
- Decision: bundle the full `Bestiary.xml` and scan it with `XmlReader` for a selected creature ID, caching successful records for the session, rather than deserializing the 42 MB document or importing the legacy database model.
- Reason: users gain full descriptive and rules content for the current browser while startup memory remains tied to the small summary dataset. The APK-size and first-open latency costs are explicit and can later be replaced by indexed storage without changing the UI projection.

## D016: Use a read-only feat projection before database migration

- Date: 2026-08-26
- Status: accepted
- Decision: project bundled `Feats.xml` into `FeatSummary` records for search, type filtering, and details instead of porting the `BaseDBClass`-derived legacy `Feat` model.
- Reason: all fields required for a useful read-only Feats screen are already present in the 7.6 MB asset. The projection also provides a narrow compatibility point for legacy schema defects such as the consistently misspelled `<Prerequistites>` tag.

## D017: Load spell summaries eagerly and full spell records lazily

- Date: 2026-08-26
- Status: accepted
- Decision: project `SpellsShort.xml` into lightweight `SpellSummary` records for browsing and filtering, then stream the selected ID from `Spells.xml` into `SpellDetails` and cache successful lookups for the session.
- Reason: the short asset makes all 2,865 spells immediately useful without importing the database-backed legacy `Spell` model or holding the full document in memory. Streaming retains complete rules text while keeping startup work bounded and the storage choice replaceable later.

## D018: Combine both legacy bestiary summary partitions

- Date: 2026-08-26
- Status: accepted
- Decision: load and combine `BestiaryShort.xml` and `BestiaryShort2.xml` by unique monster ID for the modern Monsters browser.
- Reason: the original application split its browseable bestiary for memory management and loaded only the first 1,000 records in low-memory mode. Combining both partitions exposes its complete 2,837-record catalogue while continuing to use the existing full bestiary for on-demand details.

## D019: Extract focused rule details instead of shipping the legacy database

- Date: 2026-08-26
- Status: accepted
- Decision: export the 591 rows from the `Rules` table in `Details.db` into `RuleDetails.xml`, bundle that focused asset with `RuleShort.xml`, and stream descriptions by ID on demand.
- Reason: the legacy database is approximately 32 MB and couples multiple catalogues to an obsolete SQLite layer. The focused 4.6 MB XML preserves the complete Rules content, keeps the current screen platform-neutral and testable, and avoids adding a database provider solely for read-only reference text.

## D020: Extract focused magic-item details for the Treasure browser

- Date: 2026-08-26
- Status: accepted
- Decision: export the 2,241 matching rows and read-only display fields from the `MagicItems` table in `Details.db` into `MagicItemDetails.xml`, pairing it with the existing `MagicItemsShort.xml` index.
- Reason: this completes a useful offline Treasure reference while keeping the APK substantially smaller than bundling the shared database. Streaming and session caching preserve full item descriptions and uncommon artifact/intelligent-item fields without coupling the UI to legacy persistence.

## D021: Start combat with a minimal in-memory encounter roster

- Date: 2026-08-26
- Status: accepted
- Decision: introduce a platform-neutral `CombatRoster` populated from `CreatureSummary`, and initially support add, duplicate naming, HP display, remove, and clear without loading the legacy combat graph or persisting encounters.
- Reason: this creates a testable interactive vertical slice using the complete modern Monsters browser. Deferring persistence, initiative ordering, conditions, and spell state keeps their data requirements explicit and avoids locking storage around an untested interaction model.

## D022: Use explicit deterministic initiative order and turn state

- Date: 2026-08-26
- Status: accepted
- Decision: store a whole-number initiative result on each modern combat participant, sort descending with insertion sequence as the stable tie-breaker, and track the active participant and round inside `CombatRoster`.
- Reason: deterministic ties make behavior testable and predictable without requiring Dexterity and random tie-break inputs from the legacy `InitiativeCount` UI. The roster owns navigation semantics while Android remains a thin display/input layer.

## D023: Model HP changes without importing legacy conditions

- Date: 2026-08-26
- Status: accepted
- Decision: allow damage to reduce current HP below zero, cap healing at maximum HP, and expose a derived defeated state when HP is zero or lower. Do not automatically create legacy condition records yet.
- Reason: negative HP and recovery are required for useful Pathfinder combat tracking, while automatic unconscious/dying/dead conditions depend on character-specific rules not present in the lightweight creature summary. A derived visual state is accurate without pretending the full condition system has been migrated.

## D024: Persist active encounters as explicit versioned XML

- Date: 2026-08-26
- Status: accepted
- Decision: save the lightweight combat roster after every mutation as a versioned XML document in Android application-private storage, using explicit field reading/writing and safe empty-roster fallback for corrupt or unsupported data.
- Reason: active encounters must survive activity and process recreation without requiring storage permissions or importing the legacy database. Explicit serialization is deterministic, testable, and safe under Release trimming, unlike reflection-based `XmlSerializer` use that generated trimming warnings during validation.

## D025: Represent players and ad-hoc NPCs as manual roster participants

- Date: 2026-08-27
- Status: accepted
- Decision: allow a named participant with user-supplied maximum HP to enter the same lightweight `CombatRoster`, marked by a non-bestiary creature ID and persisted in the existing version-1 snapshot.
- Reason: useful encounters require player characters and NPCs that are not bestiary records. Reusing the tested roster behavior immediately provides initiative, turns, damage, healing, removal, and restart recovery without importing the legacy character database prematurely.

## D026: Restrict identity/stat editing to manual participants

- Date: 2026-08-27
- Status: accepted
- Decision: allow name, maximum HP, and direct current-HP editing only for manual participants, while providing a safe Full HP reset for every participant.
- Reason: bestiary identity and base HP should continue reflecting the bundled source record; unrestricted editing would blur source data and encounter state. Manual entries are user-owned and need full correction, while monster health remains adjustable through damage, healing, and reset.

## D027: Add free-form combat notes before structured conditions

- Date: 2026-08-27
- Status: accepted
- Decision: attach optional free-form notes to each lightweight combat participant, display them in the roster, and persist them with the active encounter before migrating the legacy condition catalogue.
- Reason: common table states such as prone, poisoned, concentration, and reminders become usable immediately. This avoids importing global condition/spell registries until duration and turn-expiry behavior can be designed as a focused slice.

## D028: Duplicate participants as fresh initiative entries

- Date: 2026-08-27
- Status: accepted
- Decision: duplicate either manual or bestiary participants with a new sequence/instance name, full HP, unset initiative, and copied notes.
- Reason: groups and recurring NPCs become quick to assemble while each copy remains independent. Clearing initiative prevents a copied roll from silently changing encounter order; retaining notes preserves configuration that commonly applies to the group.

## D029: Expire timed conditions on completed participant turns

- Date: 2026-08-27
- Status: accepted
- Decision: model a timed condition as a name plus remaining participant turns, decrementing it when Next moves away from that participant and removing it at zero. Previous navigation does not rewind durations.
- Reason: tying duration to completed turns makes expiry deterministic and avoids double-decrementing before a combatant first acts. Previous is a navigation correction, not time travel, so reversing condition state would require a broader undo system.

## D030: Share a human-readable summary, not the persistence document

- Date: 2026-08-27
- Status: accepted
- Decision: generate plain encounter text from the roster and share it with Android's `ACTION_SEND` chooser; do not expose the versioned private XML snapshot as the user-facing export.
- Reason: the snapshot is an implementation contract intended for exact restoration, while a shared encounter should be readable in messages and session notes. Keeping formats separate allows either to evolve without breaking the other.

## D031: Apply bulk initiative as one validated roster operation

- Date: 2026-08-27
- Status: accepted
- Decision: collect initiative for every encounter participant in one scrollable dialog and apply the values through a single roster operation only after all entries are valid.
- Reason: setting initiative participant by participant becomes tedious in a real encounter. Atomic application avoids leaving a partially reordered roster when one value is missing or invalid, while retaining deterministic initiative tie ordering.

## D032: Project the legacy condition catalogue without its runtime graph

- Date: 2026-08-27
- Status: accepted
- Decision: load the 34 standard names and rules descriptions directly from the existing `Condition.xml` into lightweight `ConditionReference` records and offer them as timed-condition presets while preserving custom entry.
- Reason: the source catalogue is useful and authoritative for this application, but the legacy `Condition` class also loads spells, monster afflictions, custom files, favourites, recents, and computed bonuses. A narrow projection makes the reference content immediately usable without taking on those unrelated persistence and model dependencies.
