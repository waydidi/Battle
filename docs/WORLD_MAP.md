# World Map

## Five continents

| Continent | NPC capital | Look and feel | Fields shown on maps | Trade specialty |
|---|---|---|---|---|
| Sunspire | Sunspire City | Sandstone, bazaars, oasis | Amber Desert, Shifting Dunes, Ruined Oasis, Ancient Sands | Spices, glass, caravans |
| Frosthelm | Frostpeak City | Snow, glaciers, mountain forts | Frozen Tundra, Crystal Valley, Winter Pass, Glacier Wastes | Iron, crystals |
| Emerald | Verdant Grove | Forest canopy, rivers, wooden cities | Whispering Woods, Greenheart Field, Mistwood, Ancient Roots | Herbs, wood, crops |
| Ashen | Emberfall City | Volcanoes, blacksmith towns | Cinder Highlands, Ashen Plains, Broken Caldera | Stone, metallurgy |
| Stormreach | Tempest Harbor | Islands, cliffs, ports | Thunder Coast, Stormcliff, Misty Reaches | Ships, trade, rare cargo |

Build order: Sunspire slice → Frosthelm + Emerald regions → Ashen + Stormreach only after
representative zones perform acceptably.

## Map privacy rule

World and regional maps reveal **field names** but never monster names until the player has
encountered them. A discovery journal remembers encountered enemies separately.
Hidden building locations, private treasuries, enemy inventories and unseen monsters are
**never sent to the client** — fog of war is enforced by the server.

## Server world targets (subject to load tests)

- Five continents, conceptual geography ≈ 80 × 80 km including water and unbuildable land.
- 200 kingdom owners per persistent world (not a 200-connection limit).
- 200 protected initial kingdom plots + 400 optional expansion sites = 600 major city/castle sites.
- Villages, houses, stalls, camps and watchtowers are separate from major city slots.
- NPC capitals are public social hubs. Player cities live on designated settlement sites in owned provinces.
- A kingdom may hold several major cities, gated by influence, upkeep and expansion limits.
- Continents are instanced / streamed in scene cells — never load all 80 × 80 km at once.

## First zone layout

```
Sunspire City Market Square
  -> South Gate [loading/stream boundary]
     -> Amber Desert Field
        -> Campfire / Safe Rest
        -> Slime spawn zone
        -> Ruined Outpost [quest]
        -> Claimable Settlement Plot A
        -> Road to next region [locked in MVP]
```

The Phase 1 greybox (`Kingdoms > Build Prototype Desert Town Scene`) already places the
market square, walls with a south gate, dunes, the ruined outpost and a marker for Plot A.

## Map UI hierarchy

1. **World view** — five continents, NPC capitals, player cities, ports, travel routes, war state.
2. **Continent view** — regions, cities, available castle plots, field names, clan borders, roads.
3. **City view** — streets, building footprints, residential districts, market lots, defenses, construction.

HUD minimap (top right): rotating terrain image, player arrow + facing, nearby merchants,
quest markers, allies, party, visible threats; city walls and building icons inside settlements.
Click/tap opens the full map.

## When a player city is built

1. Server validates claim, resources, boundaries, overlaps and ownership.
2. `city` and `city_plot` records are created in **one transaction**.
3. A `city_created` outbox event is published after commit.
4. Region map markers update for players allowed to see the city.
5. The city icon upgrades as the settlement reaches new stages.
6. Own city → Manage, Build, Treasury, Market, Defenses.
7. Another player's city → Owner, Clan, Public Shops, Visit, Diplomacy + only the defenses visibility allows.

```
┌──────────────────────────────┐
│ STORMHOLD · Castle City Lv.3 │
│ Owner: Storm / Blue Banner   │
│ Region: Sunspire / Amber     │
│ Market: 7 rented / 10 total  │
│ Treasury: 18,700 Gold        │
│ Weekly costs: 4,200 Gold     │
│ [Visit] [Manage] [Build]     │
└──────────────────────────────┘
```

## Settlement plot data

Every major plot has: province ID and location, owner player/kingdom ID, building footprint
and construction permissions, occupancy / power level / defensive status, warehouse links,
rentable commercial units, maintenance requirements.

Buildable prefabs: town hall, warehouse, housing, farms, workshop, market, barracks, walls,
gates, watchtowers — placed on a grid or snapped freeform plots.
