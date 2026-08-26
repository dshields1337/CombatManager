# Legacy Android baseline

Recorded: 2026-08-26

## Application identity and platform

- Package ID: `com.kyleolson.combatmanager`
- Version name: `1.27`
- Version code: `41`
- Minimum Android API: 17
- Target Android API: 28
- Project type: legacy Xamarin.Android, non-SDK-style project
- Target framework: `MonoAndroid v9.0`

## Repository size relevant to migration

- Android C# files: 47
- Approximate Android C# lines: 11,304
- Core C# files: 146
- Approximate core C# lines: 41,824
- AXML layout files: 61

## Declared permissions and features

- Read external storage
- Write external storage
- Internet
- Touchscreen is optional
- External storage permissions and access patterns require redesign for modern scoped storage.

## Main Android surface

- Startup: `LoadingActivity`
- Main navigation: `HomeActivity`
- Core fragments: combat, monster, feat, spell, rule, treasure, and generic lookup
- Editors: attacks, spells, and the multi-screen monster editor
- Dialogs: actions, about, conditions, file selection, feat/monster/skill selection, import/export, number/HD entry, settings, and shared text-selection utilities
- Adapters: initiative, character list, character actions, library lookup, and text selection

## Data and files identified

- `bestiary.db`
- `feats.db`
- `spells.db`
- `Details.db` and versioned `detail*.db` files
- Database backup/error files created by `DBLoader`
- Application-data, personal/Documents, and public external Documents paths are used.
- `Mono.Data.Sqlite` is the legacy SQLite provider.

## Services and networking

- The core contains an EmbedIO HTTP server and WebSocket notification server.
- The application declares Internet permission.
- Background execution requirements need to be established through legacy behavior testing.

## Dependencies requiring review

- Xamarin Android Support libraries 28.0.0.1
- Xamarin Architecture Components 1.1.1.1
- Xamarin.Essentials 1.3.0
- Newtonsoft.Json 12.0.2
- EmbedIO 3.3.3
- Swan.Lite 2.4.4
- Mono.Data.Sqlite
- Local Ionic.Zip assembly

## Signing baseline

- A keystore exists under the Android project.
- The project file contains a signing path tied to another Windows user.
- Signing passwords are present in tracked project configuration and must be treated as exposed.
- Do not change or discard the keystore until upgrade/distribution requirements are confirmed.

## Baseline still requiring user/device evidence

- Known-good screenshots for each major screen and layout size.
- A representative existing user database and import/export file, handled without committing private data.
- Confirmation of which workflows are essential for the first usable release.
- Confirmation of whether the embedded web service must continue running in the background.
- Confirmation of current distribution method and whether upgrade compatibility is required.
