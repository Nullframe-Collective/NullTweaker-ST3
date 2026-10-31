# NullTweaker

One local Windows editor for Slendytubbies 3 content configurations: weapons, NPCs, player customization, maps, survival waves and trigger volumes. Written from scratch for the current CustomWeaponsFramework, CustomNPCFramework, CustomMapsFramework and CustomPlayerCustomization formats.

## Run

Open `dist/NullTweaker/NullTweaker.exe`. The portable Windows x64 build includes the .NET runtime; no separate installation is required.

## Use

- Pick Weapons, Maps, NPCs or Customization on the left. Files open in that category are listed below. **New weapon / New NPC / New customization / New map file** creates a template, **Open file** or drag-and-drop loads existing files.
- Edit fields, switches, vectors and lists. Problems are shown at the top of the page and the JSON pane updates as you type (hide it with **Hide JSON**). Hover over a field name for its exact JSON key. The search box (Ctrl+F) narrows settings by name, key or section. The app follows the Windows light/dark theme.
- NPCs contain exactly one behaviour type. Bot uses the verified `SUR/Newborn_Bot` template. Other behaviours require a matching native resource path entered by the author. Switching type preserves each type's draft during the editing session.
- Customization creates and edits `customization.json`: prefab name, optional pack ID, author, price, availability, and separate main-menu / in-game transforms. Each transform has world position, local position, local rotation in degrees and XYZ scale. The framework applies world position first, then local position. Signed scale is supported for mirrored models; a zero axis collapses the model. The bundle must contain `Name` and `Name (TPS)` prefabs. IDs beginning with `base:` are reserved.
- Maps use three separate files: `map.json`, optional `waves.json`, and optional `triggers.json`. Create all three from the same New menu. Waves and special NPC entries support add, duplicate, reorder and remove. Trigger volumes include water, damage and ambient settings.
- **⋯ > Save to pack folder** writes the required JSON filename into the selected pack directory. It does not build or copy asset bundles. Supply `bundle` and other assets separately, as required by the framework.
- Ctrl+O opens, Ctrl+S saves, Ctrl+Shift+S saves as, Ctrl+N creates a file of the selected type, Ctrl+W closes it. Unsaved files are marked with a dot in the file list. Closing prompts to save.

Legacy camelCase JSON, weapon aimPosX/Y/Z, NPC hp, old wave npc arrays and [CustomMap] INI are imported into the current field names. Conversion notes are shown; the original is untouched until explicitly saved. Multi-type NPC files require an explicit behaviour choice. Unknown extension fields are retained. Invalid values block saving; external file edits are detected before overwriting. Configuration files are bounded to 1 MB.

Configuration validation does not inspect Unity AssetBundles or verify native prefab existence. Game/framework DLLs are not required to run the editor. Custom network sync remains the responsibility of the frameworks and NullServers.

## Source and checks

`NullTweaker.Core`: schemas, migration, validation and file handling. `NullTweaker`: WPF UI. `Samples`: example configurations. `Tests`: export/import checks, including deserialization using the framework model source snapshots in `Tests/Contracts` (with their original MIT licenses). No neighbouring framework checkout or game installation is needed.

Build on Windows with the .NET 10 SDK. Open `NullTweaker.sln` or run:

```powershell
dotnet build NullTweaker.sln -c Release
dotnet run --project Tests/NullTweaker.Checks.csproj -c Release
```

The 83 checks cover current model compatibility, all four NPC payloads, customization models and vector conversion, the SuperPumpkin fixture, legacy migration, unknown fields, invalid numbers/IDs, duplicate keys and external-edit protection. Asset/prefab behaviour still needs an in-game test with the author's actual pack.

## Portable release

Run `./build.ps1` in PowerShell to publish the self-contained Windows x64 application to `dist/NullTweaker`. Runtime packs are restored from NuGet when they are not cached. Temporary publish files go to `artifacts/`; that directory can be deleted after building.

The source archive includes the application, core library, tests, contract snapshots, fixtures, sample JSON files and solution/build configuration. Generated binaries, intermediate files, research material and backups are excluded.
