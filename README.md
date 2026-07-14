# hoki-hoki

**Hoki Hoki is the original work of Max Abernethy ([flecko.net](http://flecko.net)).** This repository is a public archive of his source code, rescued for preservation.

All game design, code, art, and content are Max's. This repo exists to preserve the game and revive it on modern platforms.

## Original source

The untouched 2000s-era source, exactly as rescued, is preserved at the git tag [`original-source`](../../tree/original-source). Everything after that tag is modernization work; the current tree has been reformatted and cleaned up, so diff against the tag to see the original code.

## Modernization

The original game was built on .NET Framework / Managed DirectX (Direct3D 9, DirectSound, FMOD) and ran only on Windows. This repository contains an ongoing effort to modernize it to run cross-platform on current .NET.

**AI tooling (Claude) was used extensively for the modernization and revival work.** The original creative and technical work remains entirely Max Abernethy's.

### Modifications and enhancements

- Ported the game and supporting libraries from XNA / Managed DirectX to **.NET 8 + MonoGame**; runs on macOS, Linux, and Windows.
- **New cross-platform level editor** (`src/Editor`, ImGui-based) with full editing toolset, keyboard shortcuts, trackpad navigation, curve tool, ghost playback, and one-click playtest.
- Resizable native-resolution game window and gameplay quality-of-life fixes.
- Assets (textures, maps, sounds, fonts) embedded in the game assembly; tutor ghost data embedded; null-safe sound playback and a repaired `heal.wav`.
- CI build matrix via GitHub Actions.
- Dead code removed (unused `Cryptography` project and orphaned classes); codebase reformatted to modern C# conventions (`.editorconfig` + `dotnet format`).

## Layout

Active (in `hoki-hoki.slnx`, .NET 8):

- `src/Hoki` — the game itself
- `src/Editor` — the new cross-platform level editor
- `src/SpriteUtilities`, `src/FloatMath` — supporting libraries

Legacy, kept for reference (original Windows-only code, not part of the build):

- `src/HokiEdit` — the original level editor (WinForms / DirectX)
- `src/HokiConfig`, `src/SpriteMenu`, `src/2DTest` — original config tool, menu library, and rendering testbed
- `src/HokiSetup`, `src/ManagedSetup` — original installer projects

## Building

```sh
dotnet build hoki-hoki.slnx    # or: dotnet run --project src/Hoki
```
