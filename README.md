# Idle Framework Template

This repository contains a data-driven idle/clicker template built for Unity 2022+. All gameplay logic lives in `Assets/Scripts` and content is authored via JSON skin files under `Assets/Skins`.

## Getting Started

1. Open the project with Unity 2022 or newer.
2. Add the `Bootstrap` prefabless MonoBehaviour to an empty scene (or drop the script on an empty GameObject). At runtime the bootstrapper wires services, loads the active skin, and spawns a lightweight HUD if no canvas exists.

## Switching Skins

The active skin is stored in `PlayerPrefs` under the key `skinName`. You can change it from code by calling:

```csharp
PlayerPrefs.SetString("skinName", "Neon");
PlayerPrefs.Save();
```

The next launch will load JSON from `Assets/Skins/Neon`. Duplicate one of the skin folders to author your own theme.

## Content Authoring

Each skin provides five JSON files:

- `currencies.json` – list of currency ids, display names, and starting balances.
- `generators.json` – generator definitions including cost curves, base production, and unlock requirements.
- `upgrades.json` – upgrade metadata and effect payloads.
- `achievements.json` – presentation-only achievement definitions.
- `theme.json` – palette, font ids, prestige tuning, offline cap hours, and starting currency overrides.

Example generator definition:

```json
{
  "id": "gen_oven",
  "name": "Oven",
  "iconId": "oven",
  "currencyId": "soft",
  "baseCost": 15,
  "costCurve": { "type": "geometric", "growth": 1.15 },
  "baseRatePerSec": 0.2,
  "unlockReq": { "currencyId": "soft", "amount": 0 }
}
```

Add new generators/upgrades by appending entries to these arrays. Referential integrity is validated at load time so any missing ids will throw descriptive exceptions in the editor.

## Runtime Architecture

- **ServiceLocator** – minimal dependency container used by `Bootstrap` to register the `EconomyService`, `PrestigeService`, `SaveService`, and platform stubs.
- **TickManager** – fixed step simulation (default 10 Hz) that drives production and autosaving.
- **EconomyService** – tracks balances, generator levels, upgrade effects, and offline earnings.
- **PrestigeService** – converts lifetime production into meta currency and exposes a global multiplier hook.
- **SaveService** – JSON persistence with a pluggable cipher and simple migration pipeline.
- **ContentDB** – loads JSON definitions for the current skin and exposes lookups for runtime systems.

## Running Tests

The template ships with edit mode runtime tests validating cost curves, upgrade effects, offline earnings, and save/load round-tripping. Run them from Unity's Test Runner (Window → General → Test Runner) with the **Edit Mode** tab selected.

## Extending Numbers

`NumberFormatter` currently formats `double` values with human friendly suffixes. To integrate arbitrary precision types (e.g., `BigDouble`), update the formatter and economy calculations to use the new numeric type, ensuring tests cover edge cases.

## Autosave Location

Saves are written to `Application.persistentDataPath/idle_save.dat` (or a platform-specific equivalent). Replace `XorSaveCipher` with your own implementation to integrate encryption or cloud sync.

Enjoy reskinning and extending the template!
