# Project Breach (working title)

Single-player roguelike survival FPS for PC, built in Unity 6 LTS (URP).
Handcrafted arenas with puzzles, wave-based enemies, weapon/character upgrades,
meta-progression between runs. Original IP — no names or assets from other games.

## Repo layout

| Path | What |
|---|---|
| `ProjectBreach/` | Unity project (open this folder in Unity Hub) |
| `docs/GDD.md` | Agreed design and MVP scope |
| `tools/` | Windows CLI scripts (tests, builds) |
| `_out/` | Local tool output — logs, test results, builds (git-ignored) |

## First-time setup

1. Install Git LFS once: `git lfs install`
2. Install the Unity Editor version listed in `ProjectBreach/ProjectSettings/ProjectVersion.txt` via Unity Hub.
3. Open `ProjectBreach/` in Unity Hub.

## CLI

Close the Unity Editor first (batchmode cannot open a project that is already open).

```bat
tools\run_tests.bat            :: EditMode tests
tools\run_tests.bat PlayMode   :: PlayMode tests
```

The scripts find the editor from `ProjectVersion.txt` under
`%ProgramFiles%\Unity\Hub\Editor\<version>\`. If your editor lives elsewhere,
set `UNITY_EXE` to the full path of `Unity.exe` before running them.
