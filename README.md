# hoki-hoki

**Hoki Hoki is the original work of Max Abernethy ([flecko.net](http://flecko.net)).** This repository is a public archive of his source code, rescued for preservation.

All game design, code, art, and content are Max's. This repo exists to preserve the game and revive it on modern platforms.

## Modernization

The original game was built on .NET Framework / Managed DirectX (Direct3D 9, DirectSound, FMOD) and ran only on Windows. This repository contains an ongoing effort to modernize it to run cross-platform on current .NET.

**AI tooling (Claude) was used extensively for the modernization and revival work.** The original creative and technical work remains entirely Max Abernethy's.

## Layout

- `src/Hoki` — the game itself
- `src/HokiEdit` — the original level editor (Windows-only, WinForms)
- `src/SpriteUtilities`, `src/FloatMath`, `src/Cryptography`, `src/HokiConfig`, `src/SpriteMenu` — supporting libraries
