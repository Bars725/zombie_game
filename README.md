# Project Breach (working title)

Game folder: `Zombie_game/` (Unity project). Single-player roguelike survival FPS for PC, built in Unity 6.3 LTS (URP).
Handcrafted arenas with puzzles, wave-based enemies, weapon/character upgrades,
meta-progression between runs. Original IP — no names or assets from other games.

## Repo layout

| Path | What |
|---|---|
| `Zombie_game/` | Unity project (open this folder in Unity Hub) |
| `docs/GDD.md` | Agreed design and MVP scope |
| `tools/` | Windows CLI scripts (tests, builds) |
| `_out/` | Local tool output — logs, test results, builds (git-ignored) |

## First-time setup

1. Install Git LFS once: `git lfs install`
2. Install the Unity Editor version listed in `Zombie_game/ProjectSettings/ProjectVersion.txt` via Unity Hub.
3. Open `Zombie_game/` in Unity Hub.

## CLI

Close the Unity Editor first (batchmode cannot open a project that is already open).

```bat
tools\run_tests.bat            :: EditMode tests
tools\run_tests.bat PlayMode   :: PlayMode tests
tools\build.bat [dev]          :: Windows build -> _out\Builds\Windows\ZombieGame.exe
tools\commit.bat               :: commit + push using _out\commit_msg.txt (with safety checks)
```

Code layout and rules: `docs/ARCHITECTURE.md`. Design: `docs/GDD.md`.

The scripts read the editor version from `ProjectVersion.txt` and look for it in Unity Hub's
custom install folder, `D:\Unity\`, and `%ProgramFiles%\Unity\Hub\Editor\`. If your editor
lives elsewhere, set `UNITY_EXE` to the full path of `Unity.exe` before running them.

`tools\git_first_commit.bat` is a one-time script: it moves Unity Hub's nested repo aside
into `_to_delete\` and makes the first project commit.
