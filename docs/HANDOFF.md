# HANDOFF — read this first (for Claude or any other AI model)

**Game design lives in the Obsidian vault `obsidian_zombie_game/`** (in Russian; start from `Welcome.md`, ideas in `00 Инбокс/Инбокс.md`, rules in `99 Мета/Как работать с vault.md`). At the start of a session triage the Inbox, at the end update `04 Статус/Roadmap.md`.

This file is the single source of truth on the project's state. **Update it at the end of every work session**
(the "Status", "Next" and "Session log" sections). Design: `docs/GDD.md`. Code rules: `docs/ARCHITECTURE.md`.
Art requests: `docs/ART_ASSETS.md`.

The user communicates in Russian — reply in Russian; code, comments, commits and docs are in English.

## 0. Coordination between AIs (this file is shared)

Several AI assistants (Claude, other models, different chats) may work on the project. To avoid getting in each other's way:

1. **Before starting:** `git pull` (or ask the user to), read this file in full, check the "In progress" table.
2. **Claim the task:** add a row to "In progress" (who, task, files/folders, date) and commit
   this change **before** writing code. Don't take a task whose files are already claimed by someone else.
3. **Work in small steps:** one task = one commit; touch only the files of your task.
   If you need to change someone else's area — write it under "Requests / questions" instead of editing it yourself.
4. **At the end of a session (always, even if the task isn't finished):** remove/update your row in "In progress",
   update "Status", "Next", add a line to the "Session log", commit.
5. If information here contradicts the code — the code is right; fix this file.
6. Don't rewrite other people's log entries; only append to the end.

### In progress
| Who (model / chat) | Task | Files / folders | Since |
|---|---|---|---|
| — | — | — | — |

### Requests / questions between AIs
- (empty)

---

## 1. Project in brief

- **Project Breach** (working title): single-player roguelike survival FPS, PC, offline. Original IP: no names,
  maps or assets from Call of Duty or other games.
- Loop: waves of mutants → Scrap for kills → spending (weapons, doors, upgrades) → puzzles open
  arena sectors → objective (restore power, holdout) → extraction. Meta currency Cores between runs.
- MVP: one arena with 4 sectors, 3 weapons, 2–3 enemy types, 10 waves, implant draft every 3 waves, meta progression with saves.
  Details and accepted defaults are in `docs/GDD.md`.

## 2. Environment

| What | Value |
|---|---|
| Repo (user's machine) | `F:\claude_ai\zombie_game` |
| GitHub | `https://github.com/Bars725/zombie_game`, branch `main`, Git LFS enabled |
| Unity project | `F:\claude_ai\zombie_game\Zombie_game` (folder name is **Zombie_game**) |
| Unity | **6000.3.28f1 (Unity 6.3 LTS)**, installed in `D:\Unity\6000.3.28f1\Editor\Unity.exe` |
| Render / input | URP 17.3, Input System 1.20 (Input System only), AI Navigation 2.0.16, ProBuilder 6.1.4, Test Framework 1.6 |
| Also installed | `com.unity.ai.assistant` (pre), `com.unity.ai.inference`, `com.unity.pipeline` (exp) — the user experiments with Unity AI/MCP, **do not remove** |
| Removed | collab-proxy, multiplayer.center, visualscripting |
| OS | Windows, ultrawide monitor 3136×1312 |
| Art | ComfyUI (Tripo nodes) on the user's side, **generation tokens are very limited**, no budget for paid services |

### How the AI works with this machine (important)
- The session runs in a cloud container linked to the user's computer. **There is no shell on the user's machine.**
- Files: write into the container (`/mnt/user-data/outputs/...`) → `device_commit_files` to `F:\...`.
  **After copying, check sizes with `device_list_dir`** — a copy once silently did not apply.
  The tool adds service bytes to PNG files (pixels are identical) — that's fine.
- Reading files: `device_stage_files` → they appear in `/mnt/user-data/uploads/...`.
- Unity / Unity Hub: computer-use (screenshots, clicks). Granted for `d:\unity\6000.3.28f1\editor\unity.exe` and Unity Hub.
  Access expires after ~30 min of inactivity → `computer_resolve_access` + `computer_request_access` again.
  Terminals/Explorer allow **clicks only**, no typing — that's why everything goes through `.bat` scripts that the **user** double-clicks.
- At the end of a computer-use turn call `computer_release_lock`.

### Scripts (`tools\`)
| Script | Purpose |
|---|---|
| `run_tests.bat [EditMode|PlayMode]` | batchmode tests → `_out\TestResults`, `_out\Logs` (editor must be closed) |
| `build.bat [dev]` | Windows build → `_out\Builds\Windows\ZombieGame.exe` (not run yet) |
| `commit.bat` | `git add -A` + safety checks + commit with the message from **`_out\commit_msg.txt`** + push |
| `find_unity.bat` | finds Unity.exe from ProjectVersion.txt (Hub folder, `D:\Unity`, Program Files) |
| `cleanup_template.bat`, `git_first_commit.bat` | one-off, already done |

**Commit workflow:** the AI writes `_out\commit_msg.txt` → asks the user to run `run_tests.bat` and `commit.bat`
→ reads `_out\Logs\commit.log` / `tests_EditMode.log` / `TestResults\EditMode.xml`.
If `_out\commit_msg.txt` exists, the previous step **is not committed yet** (after the commit it becomes `commit_msg.last.txt`).

### Unity via computer-use (coordinates in the 1568×656 frame of a 0.5-scale screenshot, editor maximized)
- Menu: File (12,14) → Save Project (31,113), Exit (21,152). Tools (168,14) → Art (190,39) / Build / Sandbox (194,60) →
  "Rebuild Test Range" (273,59). Window (221,14) → General (250,87) → Test Runner (382,180).
- Play/Stop: button (790,26). Test Runner: Run All bottom right (~1555,631); expand tree — select node + `alt+Right`.
- Recompile after changing files on disk: click into the console area (600,520) + `ctrl+r`, wait ~20 s.
- A cyan model in Play = shaders still compiling, wait 10 s.
- A click into the Game view moves the mouse → the camera turns (real input).
- The Unity Hub data-consent dialog: answer "Remind me later" (do not accept on the user's behalf).

## 3. Code (Assets/_Project)

Assemblies: `Game.Core` → `Game.Gameplay` (+InputSystem, AI.Navigation) → `Game.EditorTools` (Editor), `Game.Tests.EditMode`.
`Game.Gameplay` has `InternalsVisibleTo` for tests and EditorTools (the `internal Initialize(...)` methods of SOs).

| Area | Key files |
|---|---|
| Core | `EventBus<T>` (struct events, reset on Play), `Services`, `GameRandom` (xorshift64*, `Fork("name")`) |
| Combat | `DamageInfo`, `IDamageable`, `HealthModel` (pure; segmented regen), `Health`, `HitZone` (head ×2), `DamageTakenEvent`/`EntityDiedEvent` |
| Player | `PlayerInputActions` (bindings in code), `PlayerInputReader` (cursor, Blocked, DebugHeldKeys), `PlayerLook`, `PlayerMotor` (CharacterController, sprint/stamina, crouch, jump with coyote+buffer), `StaminaModel`, `PlayerMovementConfig` SO |
| Weapons | `WeaponDefinition` SO (falloff, pellets, spread, Auto/Semi, Price, ViewModel), `WeaponInstance` (ammo/reload), `PlayerWeaponController` (hitscan, 1-3/wheel, events Fired/Hit/WeaponChanged), `WeaponViewModel` (models + recoil) |
| Enemies | `EnemyDefinition` SO (stats, SpawnCost, ScrapOnKill), `EnemyController` (FSM Spawning→Chasing→Attacking→Dead, NavMeshAgent), `MeleeAttackCycle` (pure), `EnemyFactory` (ObjectPool), `PlayerAnchor` (target), `RuntimeNavMeshBaker`, `EnemyProceduralAnimator` |
| DevTools | `DebugHud` (IMGUI, temporary), `TargetDummy`, `DamageVolume`, `DevEnemySpawner` (F1/F2), `DevPlayerRespawn` |
| Editor | `BuildScript`, `SandboxBuilder` (Tools/Sandbox: layers, data, prefabs, `Sandbox_TestRange` scene), `ArtSetup` (Tools/Art/Apply Imported Art) |
| Audio | `SoundCue` SO, `SfxPlayer` (pool of 24 AudioSources), `PlayerAudio`; editor `AudioSetup` |
| Tests | 59 EditMode tests: EventBus, Services, GameRandom (golden values), HealthModel, Health/HitZone, Stamina, Weapons, MeleeAttackCycle |

Data: `Data/Weapons/WD_Pistol|SMG|Shotgun`, `Data/Enemies/ED_Husk|ED_Runner`, `Data/Player/PMC_Default`.
Prefabs: `Prefabs/Player/Player`, `Prefabs/Enemies/Enemy_Husk|Enemy_Runner`, `Prefabs/Weapons/VM_Pistol`.
Layers: 8 Player, 9 Enemy, 10 Interactable. Art: `Art/Characters/Husk/SK_Husk.fbx`, `Art/Weapons/Pistol/SM_Pistol.fbx` (+ textures).

## 4. Lessons learned (don't step on these again)

1. Namespaces: no `Game.Editor` (clashes with `UnityEditor.Editor`), `Game.Core.Random`, or a class named like its namespace.
2. In editor scripts, after `EditorSceneManager.NewScene` and after `SaveAndReimport`, **reload assets by path** —
   old references become null and get saved into the scene as `{fileID: 0}`.
3. Tests with float boundaries (`>= 30` with 0.3f×100) are fragile — step past the threshold.
4. A pooled NavMeshAgent must not activate off the NavMesh: create under an inactive root, `SetActive(false)`,
   position first, then activate; guard `isOnNavMesh` before `isStopped`.
5. `Awake` is not called in EditMode tests → `Health` initializes lazily.
6. `ProjectSettings` (layers, build scenes) only reach disk after File → Save Project.
7. Models from Tripo come normalized to a 1 m cube with arbitrary axes → `ArtSetup` fits them automatically.

## 5. Status (2026-11-04)

Done and verified in Play mode: movement, shooting (3 weapons), head hits, targets, Husk/Runner enemies
(spawn, chase, attack, death, pool), NavMesh, death/respawn on the range, Husk model, pistol model in hand.
Jump while sprinting: coyote time/buffer/ground probe added — **the user has not confirmed the fix yet**.
P2 audio assets have been imported and `Tools > Audio > Apply Imported Audio` has been applied in Unity; cue assets,
enemy definitions, the player prefab, and the test-range scene are modified locally. Gameplay audio still needs a Play
mode check.

Commits on GitHub: repo setup → Unity project → Core/Combat → player/weapons/range → enemies.
The art import (Husk, pistol, ArtSetup, WeaponViewModel, EnemyProceduralAnimator) is committed on `main`.
`_out\commit_msg.last.txt` records the completed import; there is no pending `_out\commit_msg.txt`.

## 6. Next (in priority order)

1. **Wave Director**: waves by budget (`EnemyDefinition.SpawnCost`), cap on simultaneous enemies, lulls of 20–40 s, scaling
   with wave number, events `WaveStarted/WaveCleared`, seed via `GameRandom.Fork("waves")`. Replaces `DevEnemySpawner`.
2. **Economy**: `Wallet` (pure, tests), Scrap for hits/kills (`ScrapOnKill`) via `EntityDiedEvent`, show in HUD.
3. **Interaction**: `IInteractable` + raycast from the camera (E), wall-buys (`WeaponDefinition.Price`), ammo station,
   Scrap doors, sectors (spawn points activate when a sector opens).
4. Death → results screen (instead of DevPlayerRespawn in real runs), real HUD on uGUI.
5. Puzzles (signal graph: Lever/Plate/KeySlot → AND/Sequence/Timer → Door), power chain of 3 nodes.
6. Forge (weapon tiers), StatSystem + implant draft, objective (holdout) and extraction, run seed.
7. Meta (Cores, unlocks), JSON save (`persistentDataPath`, version field, atomic write), hub.
8. Arena greybox in ProBuilder (4 sectors), editor-baked NavMesh, PlayMode smoke tests, audio, `build.bat`.

Sounds: P1 (12 sounds x2 variants) and P2 variants are imported. `Tools > Audio > Apply Imported Audio` creates cues and assigns footsteps, landing, hitmarker, weapon switch, enemy emerge, and looping facility ambience. The wave siren cue is created for the Wave Director. P2 gameplay audio has not yet been verified in Play mode. No AudioMixer yet.
Art (only when the user has generation tokens; don't push for it): Runner → SMG → Shotgun → props from `docs/ART_ASSETS.md`.
Animations: Mixamo (free, but needs an Adobe account) — the user postponed it; until then `EnemyProceduralAnimator`.
Balance: two Runners kill the player in 3–4 s — tune once waves and medkits exist.

## 7. Working rules (from the user)

- Agree on architecture first, then code; small complete files, with an explanation of where they go and how to verify them.
- Don't invent Unity APIs; warn when unsure. Data in ScriptableObjects, logic separate (pure classes + tests).
- In every reply: assumptions and risks. Use the Unity CLI for verification where it makes sense.
- The user asked to "do everything needed" on their own — act on your own, but **don't spend their money/tokens**
  and don't accept agreements/consents on their behalf.

## 8. Session log

Format: `YYYY-MM-DD, model: what was done; what's left`. Only append.

- 2026-11-04, Claude (Opus): from scratch to a playable range: repo/LFS, Unity 6.3 LTS, Core, Combat, player, weapons,
  enemies, NavMesh, first AI models (Husk, pistol), this file. 59 tests green. Left: commit the art (see Status), Wave Director.
- 2026-11-04, Claude (Opus): imported sounds (P1) and wired them up. Requires running Tools > Audio > Apply Imported Audio in Unity (if not done yet) and committing.
- 2026-11-04, Claude (Opus): design moved into the Obsidian vault (concept, systems, roadmap, Inbox, rules for AI).
- 2026-11-04, Codex: imported P2 audio variants, added player audio hooks and looping facility ambience, prepared the wave siren cue; Unity import/setup and Wave Director remain.
