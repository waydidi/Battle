# Classes, Stats and Skills

## Seven stats

Implemented in `client-unity/Assets/Scripts/Core/CharacterStats.cs` (values clamped 1–999).

| Stat | Effects |
|---|---|
| STR | Physical attack, heavy weapon efficacy |
| AGI | Run/sprint speed, sprint & dodge SP efficiency, evasion |
| VIT | Max HP, HP/SP regen, stagger resistance, defense |
| LUCK | Bounded crit/proc chance, crafting luck |
| ELEMENT | Elemental damage/resistance, max SP |
| SENSE | Detection, counter window, weak-point perception, max SP |
| DEX | Weapon handling, precision, bounded cooldown/recovery reductions, some move speed |

## Derived values and caps

All formulas live in `Core/StatFormulas.cs`. Bonuses use diminishing returns:

```
bonus = cap * score / (score + halfPoint)
```

The bonus is half its cap at `score == halfPoint` and **can never reach the cap**.
The server must compute these from saved stats + buffs and ignore any client-sent speed or damage.

| Value | Base | Driven by | Cap | Half point |
|---|---|---|---|---|
| Run speed | 5.0 m/s | AGI + ½ DEX | +25 % | 150 |
| Walk speed | 2.2 m/s | AGI + ½ DEX | +25 % | 150 |
| Sprint speed | run × 1.6 | — | hard ceiling `MaxSprintSpeed` = 10 m/s | — |
| Sprint SP cost | 8 SP/s | AGI | −40 % | 120 |
| Dodge SP cost | 20 SP | AGI | −40 % | 120 |
| Dodge cooldown | 0.8 s | DEX | −30 % | 120 |
| Dodge | 4.5 m in 0.35 s, invulnerable for first 70 % | — | — | — |
| Crit chance | 5 % | LUCK | +30 % (max < 35 %) | 150 |
| Crit damage | ×1.5 | — | — | — |
| Skill cooldown reduction | 0 | DEX | 25 % | 200 |
| Max HP | 100 + 20·level + 12·VIT | | | |
| Max SP | 50 + 4·level + 2·ELEMENT + SENSE | | | |
| Physical attack | 2·STR + ½·DEX + level | | | |
| Damage taken | raw × 100 / (100 + defense) | | | |

Tests in `client-unity/Assets/Tests/EditMode/` verify every cap at max stats.

## Levels

- Character level (yellow bar) and job level (violet bar) use **separate EXP curves**
  (`Core/ExperienceTrack.cs`): character `100·L^2.2`, job `80·L^1.9`. Max 99 / 70 for now.
- Normal classes: Novice → Warrior, Mage, Ranger, Support (first launch: **Warrior only**).
- Class change near Lv.15, advanced near Lv.40, awakening near Lv.70 (tuning targets).
- Skill trees = class base skills + city-of-origin lineage skills + stat-based modifiers.
- Normal skills can be respecced for a reasonable Gold fee with a cooldown.

## Warrior birthplace skills (10 each)

**Sunspire / Desert Vanguard:** Desert Slash, Sand Step, Whirlwind Cut, Guard Stance,
Shield Bash, Desert Charge, Sunfire Strike, Sunfire Cleave, Sandstorm Aura, Blazing Domain.

**Frosthelm / Iceborn Vanguard:** Frost Slash, Ice Dash, Snowstep, Glacier Guard, Frozen Aegis,
Winter Counter, Shard Burst, Polar Charge, Cryo Roar, Absolute Zero Cleave.

**Emerald / Forestborn Guardian:** Thorn Strike, Vine Rush, Leafstep, Verdant Guard, Bark Aegis,
Root Counter, Wild Bloom, Hunter's Charge, Spirit Howl, Ancient Grove Cleave.

Stat investment unlocks distinct, capped effects inside the same skill — e.g. AGI shortens
Desert Slash recovery, STR improves stagger, ELEMENT strengthens burn, SENSE widens the
counter window. Every such effect must have a documented cap in this file.

### Implemented so far (Phase 1)

| Skill | Key | SP | Cooldown | Damage | Range | Arc | Cast / recovery |
|---|---|---|---|---|---|---|---|
| Attack (light) | LMB | 0 | 0.45 s | ×1.0 | 2.2 m | 120° | 0.10 / 0.25 s |
| Desert Slash | 1 | 15 | 4 s | ×2.4 | 3.0 m | 200° | 0.15 / 0.40 s |

## Secret classes

- **5–10 holders total per world** (not per class) unless revised — see [DECISIONS.md](DECISIONS.md).
- Slot allocation is a server-side atomic operation (no race can exceed the cap).
- **Two validated qualifying combat deaths** permanently remove the secret class; base class,
  level and inventory are kept.
- Safe duels, tutorials, crash-induced deaths and bugged battles never count. Keep logs and
  build dispute/recovery tooling.
- Discovery is not pure luck: hidden achievements, stat thresholds, birthplace, encounters, then
  a bounded chance once requirements are met.
- New secret classes ship only after: offline AI proposal → strict template validation →
  automated match simulations → exploit tests → human approval → staged patch.
- AI never writes executable code, never edits production data, never changes balance mid-battle.

```json
{
  "class_id": "eclipse_blade_dancer_v1",
  "base_class": "warrior",
  "origin_tags": ["sunspire", "emerald"],
  "requirements": {"agi_min": 70, "sense_min": 55, "quest_flag": "hidden_ritual"},
  "skill_template_ids": ["dash_combo_v2", "blindside_counter_v1"],
  "max_global_holders": 8,
  "max_qualifying_deaths": 2,
  "patch_version": "0.1.0"
}
```
