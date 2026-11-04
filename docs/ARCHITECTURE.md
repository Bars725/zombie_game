# Architecture (v0.1)

Unity 6.3 LTS, URP. All game code lives in `Zombie_game/Assets/_Project/`.

## Assemblies

| Assembly | Folder | References | Contains |
|---|---|---|---|
| `Game.Core` | `Scripts/Core` | — | EventBus, Services, GameRandom. No gameplay knowledge |
| `Game.Gameplay` | `Scripts/Gameplay` | Core | Combat, player, weapons, enemies, waves, arena, puzzles, economy |
| `Game.UI` *(later)* | `Scripts/UI` | Core, Gameplay | HUD, menus. Listens to events, never drives gameplay |
| `Game.EditorTools` | `Scripts/Editor` | Core, Gameplay | BuildScript, validators, editor menus (Editor only) |
| `Game.Tests.EditMode` | `Tests/EditMode` | Core, Gameplay | Unit tests for pure logic |
| `Game.Tests.PlayMode` *(later)* | `Tests/PlayMode` | Core, Gameplay | Scene smoke tests |

Dependencies only point down: UI → Gameplay → Core. Core never references Gameplay.

## Namespaces

`Game.Core`, `Game.Core.Events`, `Game.Gameplay.<Feature>` (e.g. `Game.Gameplay.Combat`),
`Game.EditorTools`, `Game.Tests.EditMode`.
Avoid namespace segments that clash with Unity types: no `Game.Editor` (clashes with
`UnityEditor.Editor`), no `Game.Core.Random`, no class named like its namespace.

## Folder layout (created as needed — git does not keep empty folders)

```
Assets/_Project/
  Scripts/{Core, Gameplay/<Feature>, UI, Editor}
  Tests/{EditMode, PlayMode}
  Data/{Weapons, Enemies, Waves, Implants, Meta, Arenas, Audio}   ScriptableObject assets
  Prefabs/{Player, Weapons, Enemies, Interactables, Puzzles, UI}
  Scenes/{Boot, Hub, Run, Arena_01_Facility, Sandbox_TestRange}
  Art/  Audio/  Materials/  Settings/
```

## Rules

1. **Data vs logic.** ScriptableObjects hold design data only. Never write to an SO at runtime
   (in the Editor the change persists after Play mode). Runtime state lives in plain C# objects
   (e.g. `WeaponInstance` holding a reference to its `WeaponDefinition`).
2. **Pure logic in plain C# classes** (`HealthModel`, later `Wallet`, `WaveBudget`, `StatSheet`),
   MonoBehaviours are thin wrappers. This keeps logic EditMode-testable in milliseconds.
3. **Events.** `EventBus<T>` (struct events implementing `IGameEvent`) for global "many systems
   care" events: `EntityDiedEvent`, `DamageTakenEvent`, later `WaveStartedEvent`, `ScrapChangedEvent`.
   Local wiring uses direct references or C# events. Always unsubscribe in `OnDisable`/`OnDestroy`.
4. **Services.** `Services.Register/Get` filled by the Boot scene. Keep it small; register by
   interface when a fake is useful in tests.
5. **Randomness.** Never `UnityEngine.Random` in run generation. The run owns one `GameRandom(seed)`
   and each system takes its own stream: `run.Fork("waves")`, `run.Fork("loot")`.
6. **Statics reset** on play-mode start via `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`,
   so the project works with Domain Reload disabled.
7. **Damage flow.** Weapon raycast → `collider.GetComponent<IDamageable>()` → `HitZone` applies
   the multiplier → `Health.TakeDamage` → `HealthModel` → events.

## Verification

- `tools\run_tests.bat` — EditMode tests in batchmode (editor closed).
- `tools\build.bat [dev]` — Windows build to `_out\Builds\Windows\`.
- `tools\commit.bat` — commits with the message in `_out\commit_msg.txt`, with safety checks.
