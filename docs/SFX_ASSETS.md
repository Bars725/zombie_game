# Sound requests v1 (ComfyUI, Mirelo Text → SFX, model sfx-2.0)

Graph: `Mirelo: text to SFX` → `Save audio (advanced)`.
- **format:** `wav` if available in the list (better for short sounds: mp3 adds silence at the start); otherwise `mp3`, quality `V0`.
- **filename_prefix:** the file name from the table, e.g. `audio/SFX_Pistol_Shot`.
- **seed:** any; for important sounds, run 2 variants (different seeds) — I'll pick.
- **negative_prompt** (shared): `music, speech, voice, singing, melody`
- Where to put them: `F:\claude_ai\zombie_game\_incoming_art\Audio\`

## P1 — required (12 sounds, ~170 credits)

| File | Duration | prompt |
|---|---|---|
| `SFX_Pistol_Shot` | 1.0 | `Single sharp pistol gunshot, close mic, short indoor reverb tail` |
| `SFX_SMG_Shot` | 0.5 | `Single short punchy submachine gun shot, close mic, tight and dry` |
| `SFX_Shotgun_Shot` | 1.5 | `Single heavy shotgun blast, deep boom, close mic, short concrete room echo` |
| `SFX_Reload` | 1.5 | `Gun magazine release and insert, metallic clicks, close mic` |
| `SFX_DryFire` | 0.5 | `Empty gun trigger click, single dry metal click, close mic` |
| `SFX_Hit_Flesh` | 0.5 | `Bullet impact into flesh, wet thud, close mic` |
| `SFX_Hit_Metal` | 0.5 | `Bullet ricochet off metal surface, sharp ping, close mic` |
| `SFX_Husk_Growl` | 2.0 | `Low raspy zombie groan, slow breathing growl, close mic` |
| `SFX_Runner_Screech` | 1.5 | `Short aggressive creature screech, harsh scream, medium distance` |
| `SFX_Enemy_Attack` | 1.0 | `Fast claw swipe whoosh then hit, close mic` |
| `SFX_Enemy_Death` | 1.5 | `Creature death gurgle and body falling on concrete floor` |
| `SFX_Player_Hurt` | 0.8 | `Male grunt of pain, short, close mic` *(for this one remove `speech, voice` from negative)* |

## P2 — when there are credits

| File | Duration | prompt |
|---|---|---|
| `SFX_Footstep_Concrete` | 2.0 | `Footsteps walking on concrete floor, boots, close mic, steady pace` |
| `SFX_Jump_Land` | 0.6 | `Boots landing on concrete after a jump, single thud` |
| `SFX_Enemy_Emerge` | 1.5 | `Concrete floor cracking and rubble shifting, creature rising, close` |
| `SFX_Weapon_Switch` | 0.6 | `Gun handling, cloth rustle and metal click, close mic` |
| `SFX_Hitmarker` | 0.3 | `Tiny crisp UI tick, short high click` |
| `SFX_Ambience_Facility` | 20 | `Empty industrial facility ambience, low machine hum, distant metal creaks, ventilation` |
| `SFX_Wave_Start` | 3.0 | `Distant industrial alarm siren, two pulses, large concrete hall` |

## P3 — for future systems
`SFX_Purchase` (cash register-like mechanical ka-ching, 1 s), `SFX_Door_Open` (heavy metal sliding door, hydraulic hiss, 2.5 s),
`SFX_Lever` (heavy metal lever pulled, clunk, 1 s), `SFX_Power_On` (generator power up hum rising, electric buzz, 3 s),
`SFX_Medkit` (medical spray hiss and bandage rustle, 1.5 s).

## Tips (from the guide)
- One sound per prompt; concrete nouns rather than "scary/epic".
- If it came out too long/noisy — shorten the duration, don't rewrite the prompt.
- Shots and hits — short (0.3–1.5 s). Ambience — long and even.
