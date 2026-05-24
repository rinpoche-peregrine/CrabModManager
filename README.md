# Crab Mod Manager

<img src="Packaging/icon.png" width="160" align="right" alt="Crab Mod Manager icon" />

A tiny BepInEx mod manager for *Everything is Crab*. Drop the exe into your game folder, run it, install mods by dragging zips, toggle them with checkboxes.

## Description

Lightweight, single-file Windows app that lives inside your *Everything is Crab* install directory. No Steam / Thunderstore account, no profiles, no game-list dropdown — just one game, one folder, one manager. Built specifically because *Everything is Crab* doesn't have a Thunderstore community (yet).

## Installation instructions

1. Download `CrabModManager.exe` from the [Releases](../../releases) page.
2. Put it directly in your *Everything is Crab* install folder — the one with `Everything is Crab.exe` in it (typically `C:\Program Files (x86)\Steam\steamapps\common\Everything is Crab\` or your custom Steam library).
3. Double-click `CrabModManager.exe` to run.

## Main features

- **Installs BepInEx with one click.** Detects whether BepInEx is already present; if not, downloads the right Unity 6 IL2CPP CoreCLR build (currently BE 755) and extracts it.
- **Drag-and-drop mod install.** Drop any Thunderstore-style mod zip onto the window and it lands in `BepInEx/plugins/`.
- **Enable/disable per mod.** Toggle the checkbox to move a mod out of the loader's path without uninstalling.
- **Uninstall button.** One-click delete of any installed mod folder.
- **Launch Game button.** Saves alt-tabbing back to Steam.

## Requirements

- **Windows 10/11 x64.** No .NET install needed — the runtime is bundled.
- *Everything is Crab* installed somewhere on disk.

## How enable/disable actually works

A disabled mod's folder is moved from `BepInEx/plugins/<name>/` to `BepInEx/plugins-disabled/<name>/`. BepInEx only scans `plugins/`, so anything in `plugins-disabled/` is invisible to the loader. Re-enabling moves it back.

## Author

Bungus

## Source

<https://github.com/rinpoche-peregrine/CrabModManager>
