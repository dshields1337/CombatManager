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
