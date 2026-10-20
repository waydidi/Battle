# Game Design — Kingdoms of the Eight Banners

Original persistent online fantasy MMORPG: third-person 3D exploration, skill-based real-time
combat, player-built cities, rentable shops and a simulated kingdom economy.
**PC first, mobile later**, with shared accounts and character progression.

> Reality check: this is a multi-year, commercial-scale vision. Every number in these docs is a
> *design target*, not a proven capacity or deadline. Build the small vertical slice first.
> All art, lore, class names and assets must be original or properly licensed — do not copy
> Battle Realms, Dragon Nest or any other game's IP.

## Core loop (the thing to prove first)

Walk through Sunspire City → accept a quest → fight desert slimes → earn Gold → rent a market
stall → sell an item → city owner receives rent → city treasury pays maintenance → the
settlement appears on the map.

Continents, wars and AI secret classes are built *on top of* that loop, never before it.

## MVP — the first playable game

One small district of **Sunspire City** plus **Amber Desert Field**:

1. Spawn as a male 3D Warrior, third-person camera (keyboard/mouse).
2. Move in 360°: walk, sprint, jump, dodge, attack.
3. Top-left HUD: portrait, name **Storm**, **Lv.68** (test character), red HP, blue SP,
   character EXP and job EXP.
4. Bottom-left chat panel (World, Party, Clan, System) with typing field — real chat later.
5. Top-right minimap, expandable to district, regional and world map.
6. One quest-giver, one merchant, one field, one slime enemy, three warrior skills, loot and Gold.
7. One claimable city plot, buildable town hall, five rentable market stalls, a city treasury
   with weekly maintenance.
8. Save/restore character and city data from an authoritative backend.
9. A 2–10 player online test before expanding server capacity.

**Definition of done:** two players can sign in, walk around, fight server-controlled monsters,
chat, see each other, rent different shops, and come back later with characters and shop
records intact. Transactions cannot be duplicated by reconnecting or clicking repeatedly.

## Pillars

| Pillar | Meaning |
|---|---|
| Fast, readable action combat | Dodge, counters, short skill windows; animations never hide server rules |
| A world players build | Cities, castles and shops are player-owned, persistent and visible on the map |
| An honest economy | Every Gold movement is ledgered, idempotent and auditable |
| Identity through origin | Birthplace changes how the same class plays |
| Fair, server-authoritative | The client displays; the server decides |

## Movement & combat summary

See [CLASSES_AND_SKILLS.md](CLASSES_AND_SKILLS.md) for stats and skills,
[NETWORKING.md](NETWORKING.md) for authority rules and [UI_SPEC.md](UI_SPEC.md) for controls.

- Over-the-shoulder camera, adjustable distance and pitch, 360° rotation.
- Camera-relative movement with collision, slopes, ledges and blend-tree locomotion.
- Sprint and dodge cost SP; dodge has a cooldown and brief invulnerability.
- Light attack, heavy attack, block/counter, dash and three test skills.
- Each skill has cast time, recovery, range, hitbox, target validation, cost, cooldown, status effects.
- Animation-cancel rules are explicit design, not accidental bugs.
- Start with 1 slime → a second enemy → one miniboss. No huge enemy populations early.
- Test PvP feel at 80–200 ms simulated latency.

## Beginner origins (tutorial areas, not extra continents)

1. **Emerald Valley / Verdant Grove** — gathering, farming, building, starter militia.
2. **Wolfwood Frontier** (forest province in Emerald) — scouting, survival, bandit camp.
3. **Jade Mountain Pass** (province near Frosthelm) — mining, defense, hill fortress.

Tutorial flow: spawn → meet elder → movement tutorial → collect resources → talk to tradesperson
→ defeat enemy → train first skill → acquire land deed → choose a province → build first settlement.

## Settlements

Land Claim → Village → Town → Castle City → Capital. Details in
[WORLD_MAP.md](WORLD_MAP.md) and [ECONOMY.md](ECONOMY.md).

## Scope rules

- Five continents can be concepted early; only one small zone is built first.
- PC prototype first. Mobile is a separate production effort, not a checkbox.
- AI stays outside the combat critical path; it may propose content, never change live rules.
- No selling Gold or combat advantages for real money during MVP.
