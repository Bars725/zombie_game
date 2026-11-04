# Project Breach — GDD-lite (v0.1, agreed 2026-11-04)

Working title. Original IP: own names, characters, maps and assets. Mechanics are
inspired by the wave-survival genre; nothing is copied from existing games.

## Decisions (defaults accepted)

| Topic | Decision |
|---|---|
| Engine | Unity 6 LTS, URP, Input System, AI Navigation, ProBuilder, Unity Test Framework |
| Platform | PC, single-player, offline, keyboard + mouse (gamepad later) |
| Run structure | MVP: **one large arena per run** (25–35 min) ending in extraction. Architecture ready for a multi-arena route later |
| Puzzles | Mostly solved in **lulls between waves**; optional "under fire" bonus puzzles |
| Meta | Mixed, ~80% horizontal unlocks + small capped stat bonuses |
| Movement | Sprint (stamina), crouch, jump. No slide/mantle in MVP |
| Enemy count | Up to 30–40 alive at once (NavMeshAgent is fine) |
| Story | Minimal framing only |
| Saving | Meta-progression + settings only. No mid-run saves in MVP |
| Art | Greybox (ProBuilder + capsules) until week 3, then CC0 assets |
| Version control | Git + Git LFS, GitHub remote, Unity project in `ProjectBreach/` |

## Setting (minimal)

An isolated research complex after a containment failure. Mutated **Husks** fill the halls.
The player is a **Retriever** sent in to restore power, recover data and extract.

## Glossary (working names)

- **Scrap** — run currency (hits, kills, puzzles, caches)
- **Cores** — meta currency (elites, objective, extraction, partial on death)
- **Implants** — run perks, drafted 1-of-3 every 3 waves
- **Forge** — weapon upgrade station (tiers)
- **Enemies** — Husk (walker), Runner (fast), Brute (tank, stretch), Spitter (ranged, post-MVP)
- **Heat** — difficulty modifiers for meta (post-MVP)

## Loops

- **Core (seconds):** shoot → earn Scrap → spend (ammo, weapon, door, upgrade) → survive harder enemies
- **Session (one wave, 2–4 min):** wave → lull 20–40 s (shop, Forge, puzzles, explore) → next wave
- **Run (25–35 min):** waves 1–3 learn & open sectors → waves 4–8 restore power at 3 nodes via puzzles
  (unlocks Forge and the route to the objective) → holdout at the extraction terminal → **extract = win**
  (option: stay in endless for a Cores multiplier)
- **Meta (between runs):** spend Cores on new weapons/implants in pools, starting loadouts,
  capped stat bonuses; later new arenas and Heat

## Survival-lite

- HP regenerates only to the nearest 25% segment; more needs a medkit
- Stamina — sprint only
- Ammo reserve per weapon
- No hunger/thirst. Flashlight battery is post-MVP

## Death and rewards

Death: enemy damage, environmental hazards, resource starvation. A failed timer puzzle
triggers a **surge** (extra wave), not instant death.

| Reward | Lifetime | Source |
|---|---|---|
| Scrap | run | hits, kills, puzzles, caches |
| Implants | run | draft every 3 waves, rare caches |
| Weapon tiers | run | Forge |
| Keys / fuses | arena | puzzles, hidden passages |
| Cores | permanent | elites, objective, extraction (x2), death (partial) |

## Puzzles

Physical and readable in combat (levers, plates, fuses, lamp sequences, timers),
10–60 s to solve, variant picked per run from the seed, every puzzle grants something
concrete (sector, power, objective progress, cache).

## Map unlocking

Scrap doors (economy), puzzle doors (skill/attention), power gates (objective),
hidden passages (exploration → caches). Opening a sector activates its spawn points —
the map gets more dangerous as it grows.

## Handcrafted vs generated

| Handcrafted (scene) | Generated per run (seed) |
|---|---|
| Geometry, NavMesh, cover, sightlines | Active puzzle variant (of 2–3 authored) |
| Puzzle mechanisms and their wiring | Key/fuse location (of N authored candidates) |
| Spawn point placement | Which gates are Scrap vs puzzle (within allowed sets) |
| Shop / Forge / cache placement | Wall-buy contents from the unlocked pool |
| Sector graph | Wave composition, elites, implant draft, mutators |

One seeded `System.Random` per run, passed to systems. Same seed → same run.

## MVP scope

**In:** 1 arena with 4 sectors (entry, Scrap-gated, puzzle-gated, objective); player
(walk/sprint/crouch/jump, segmented HP, medkits); 3 weapons (pistol, SMG, shotgun),
wall-buys, ammo station, Forge with 2 tiers; Husk + Runner (Brute stretch); 10 waves with
a budget-based director and lulls; puzzle framework (lever, plate, key slot, sequence,
timer, door) with one 3-node power chain; implant draft (8–10 stat implants); holdout +
extraction; results screen; meta with Cores and 5–6 unlocks; JSON save; HUD; basic SFX
and hit feedback; run seed; pooling; CLI tests and build.

**Later:** multiple arenas and route, hidden passages (one is a stretch goal), Spitter and
bosses, flashlight, Heat, event-driven implants, Addressables, run saves, gamepad, full
settings, story.

## Priority order

1. Project setup, Git/LFS, asmdefs, CLI test run
2. Player + camera in Sandbox
3. Health/Damage + hitscan weapon from ScriptableObject — **milestone: shoot a target**
4. Enemy (NavMesh + FSM) + pooling
5. Wave director + spawn points
6. Wallet, interaction, wall-buys, Scrap doors, sectors
7. HUD, death, results — **milestone: survival loop**
8. Puzzle framework + power chain
9. Forge, implant draft, stat system
10. Objective, extraction, run seed — **milestone: full run**
11. Meta, save, hub
12. Polish, profiling, build, playtests
