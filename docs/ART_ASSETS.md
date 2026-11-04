# Art requests v1: models for the test range (ComfyUI)

Goal: replace the grey capsules and boxes with simple but readable models.
We need **readable greybox+**, not finished art: correct silhouette, scale and pivot.

## How to generate (recommended pipeline)

1. **Text → image (concept):** one object, plain light background, even lighting.
   For characters: **front view, T-pose**. For objects: **3/4 view** (or front + side if the 3D node accepts several views).
2. **Image → 3D:** whatever 3D node you have in ComfyUI (Hunyuan3D, TRELLIS, etc.; I can't see your setup, so check what supports textures).
3. **Cleanup (if you have Blender):** decimate to the triangle budget, check scale/pivot, export FBX.
   No Blender: send it as is (GLB/OBJ) and I'll handle the rest in Unity, but the result may be rougher.

Prompts are in English (models understand it better). Copy the **common style** + **common negative** into every request.

### Common style (append to every prompt)
```
stylized semi-realistic game asset, gritty abandoned sci-fi research facility theme,
chunky readable shapes, clean silhouette, hand-painted PBR textures, muted colors,
single object, centered, fully visible, isolated on plain light gray background,
soft even studio lighting, no cast shadows
```

### Common negative
```
text, watermark, logo, signature, multiple objects, cropped, cut off, background scene,
floor, motion blur, depth of field, dramatic lighting, heavy shadows, photo, blurry,
noisy, extra limbs, deformed
```

Do not reference existing games, brands or characters in prompts (to stay clear of IP issues).

## Technical requirements (important for Unity)

| Parameter | Requirement |
|---|---|
| Format | **FBX** (preferred) or OBJ. GLB also works: I'll add the glTFast package |
| Units / scale | 1 unit = 1 metre. Human-sized enemy ~ **1.8 m** |
| Axes | Y up, model's front faces **+Z** (if unsure, I'll fix it in Unity) |
| Pivot | Characters and props: **bottom centre** (at the feet). Weapons: at the grip |
| Triangles | Enemies ≤ **15k**, weapons ≤ **8k**, props ≤ **3k** |
| Textures | Base Color **1024–2048 px**; Normal / Roughness / Metallic are a bonus |
| Characters | **T-pose** or A-pose, arms away from the body, legs apart, **nothing in hands**, no cloaks or long hair |
| Names | As in the list: `SM_Crate_Metal.fbx`, `SK_Husk.fbx`, `T_Husk_BaseColor.png` |

Why T-pose: AI models come without a skeleton. I'll run the enemies through **Mixamo** auto-rig
(free, Adobe account) and fetch walk/run/attack/death animations there.
Mixamo only works with a symmetrical humanoid with separated limbs.
Check the Mixamo licence yourself (as far as I know it allows use in games).

**Where to put files:** `F:\claude_ai\zombie_game\_incoming_art\` (not committed to Git).
I'll import from there into `Assets/_Project/Art/...`, compress, and wire up to the prefabs.

---

## P1: biggest visual payoff (do first)

### 1. `SK_Husk`: slow mutant (enemy)
Height 1.8 m, hunched, heavy. Gaunt facility worker in a torn jumpsuit, greyish-green skin.
The head must read clearly (headshots ×2).
```
full body character, front view, T-pose, arms straight out to the sides, legs slightly apart,
symmetrical, gaunt mutated facility worker, torn grey-green work jumpsuit, pale greenish
sickly skin, sunken eyes, slightly hunched shoulders, bare hands, no weapon, no hair
```

### 2. `SK_Runner`: fast mutant (enemy)
Height 1.75 m, thin, long limbs, red/rust accents. Must look **faster and more dangerous** than the Husk at a glance.
```
full body character, front view, T-pose, arms straight out to the sides, legs slightly apart,
symmetrical, thin wiry mutated humanoid, elongated arms and fingers, lean muscular body,
torn dark red tank top and cargo pants, raw reddish skin patches, aggressive face,
bare hands, no weapon, no hair
```

### 3. `SM_Pistol`: pistol (first-person view)
~0.2 m. Compact, angular, polymer + metal.
```
compact sci-fi semi-automatic pistol, side view, angular polymer frame, metal slide,
small emissive blue sight, industrial design, game weapon asset
```

### 4. `SM_SMG`: submachine gun
~0.55 m, straight magazine, folding stock.
```
compact sci-fi submachine gun, side view, straight vertical magazine, short folding stock,
boxy receiver, rail on top, worn gunmetal and orange accents, game weapon asset
```

### 5. `SM_Shotgun`: shotgun
~0.9 m, massive, pump action.
```
heavy sci-fi pump-action shotgun, side view, thick barrel, chunky pump grip, short stock,
worn steel and dark polymer, hazard yellow stripe accent, game weapon asset
```

## P2: environment and the range

### 6. `SM_Crate_Metal`: metal crate (cover) — 1×1×1 m
```
heavy metal supply crate, cube shape, reinforced corners, hazard stripes on edges,
stenciled numbers, worn gray paint, 3/4 view
```

### 7. `SM_Barrier_Concrete`: concrete barrier — 2×0.6×1 m
```
concrete jersey barrier, long low road barrier block, chipped edges, faded yellow paint
stripe, 3/4 view
```

### 8. `SM_TargetDummy`: training dummy — 1.9 m, on a stand
```
training target mannequin on a metal stand, simple humanoid torso and round head,
padded orange fabric, bolts and straps, 3/4 view
```

### 9. Seamless textures (text → image, 1024×1024, **tileable**)
Plain textures, not 3D. Add `seamless tileable texture, top-down flat, no perspective` to the prompt.
- `T_Floor_Concrete`: `worn industrial concrete floor, cracks, faint painted lines`
- `T_Wall_MetalPanel`: `sci-fi metal wall panels, rivets, seams, grime`
- `T_HazardStripes`: `yellow and black diagonal hazard stripes, worn paint`

## P3: for upcoming systems (shop, puzzles, objective)

| Name | Size | Prompt (core) | Note |
|---|---|---|---|
| `SM_WallBuy_Panel` | 1.2×0.8 m | `wall-mounted weapon rack panel, empty gun silhouette outline, small price display screen` | wall-buy weapon spot |
| `SM_AmmoStation` | 1×0.6×1.8 m | `industrial ammo vending locker, glowing green screen, ammo box icons` | |
| `SM_Lever_Base` + `SM_Lever_Handle` | 0.5 m | `industrial wall lever switch, metal base plate` / `lever handle with red grip` | **two separate parts**, I'll animate the handle |
| `SM_PressurePlate` | 1×1×0.1 m | `metal floor pressure plate, square, grated, hazard border` | |
| `SM_PowerNode` | 1×1×2 m | `industrial power generator node, cables, glowing core, status lights` | objective: restore power |
| `SM_Door_Frame` + `SM_Door_Leaf` | 3×3 m | `heavy sci-fi security blast door frame` / `blast door panel, hazard stripes` | two parts: the door slides |
| `SM_Medkit` | 0.4 m | `rugged white medical kit case with a green plus symbol, handle on top` | **no red cross**: that's a protected emblem |
| `SM_Forge_Station` | 2×1×1.2 m | `industrial weapon upgrade workbench, robotic arm, tools, glowing panel` | upgrade station |
| `SM_ExtractionTerminal` | 1×1×1.6 m | `extraction control terminal console, big screen, antenna, beacon light` | run objective |
| `SK_Brute` | 2.3 m | `huge bulky mutated humanoid, armored plates fused to skin, T-pose` | stretch enemy |

---

## What to send me back

- Model files (+ textures if separate) in `_incoming_art\`.
- If possible, the concept images too (PNG): helpful for fixing colours.
- Notes, if any: "scale unknown", "Y and Z swapped", etc.

What I'll do: import, check scale/axes/pivot, set up materials (URP Lit) and compression,
put enemies through Mixamo (or ask you to, if an Adobe login is needed), and wire everything into the prefabs.
Head/body hitboxes will be refitted to the new models.
