# Crab Mod Manager

<img src="Packaging/icon.png" width="160" align="right" alt="Crab Mod Manager icon" />

A small mod manager for *Everything is Crab*. Place the exe in your game folder and use it to install BepInEx and manage your mods.

Also available on [Nexus Mods](https://www.nexusmods.com/everythingiscrab/mods/2).

## Requirements

- Windows 10 or 11 (x64). No .NET install needed. The runtime is bundled.
- *Everything is Crab* installed.

## Install

1. Download `CrabModManager.exe` from the [Releases](../../releases) page (or from [Nexus](https://www.nexusmods.com/everythingiscrab/mods/2)).
2. Put it in your *Everything is Crab* install folder. That is the folder with `Everything is Crab.exe` in it.
3. Double-click it.

## Features

- Installs BepInEx with one click. Downloads the Unity 6 IL2CPP CoreCLR build (currently BE 755).
- Drag and drop install for any Thunderstore-style mod zip. Works with mods like [Skip Intro](https://github.com/rinpoche-peregrine/EverythingIsCrabMods) and [Past Player Characters as Mobs](https://github.com/rinpoche-peregrine/PastPlayerCharactersAsMobs).
- Enable and disable mods with a checkbox. Disabled mods move to `BepInEx/plugins-disabled/` so the loader skips them.
- Sort the mod list by name, install date, or enabled state.
- One-click uninstall.
- Launch Game button.

## Notes

This is a small Windows-only tool built specifically for *Everything is Crab*. If your game gets a Thunderstore community, [Gale](https://github.com/Kesomannen/gale) and r2modman will also work and have more features.

## Author

Bungus

## Source

<https://github.com/rinpoche-peregrine/CrabModManager>

## Support

If you find this useful, you can buy me a coffee:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z852YLV)
