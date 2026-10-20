# UI Specification

## In-game HUD layout

```
TOP LEFT     : Storm | Lv.68 | HP red | SP blue | Lv EXP yellow | Job EXP violet
TOP RIGHT    : Shop Event Skill Clan Menu + location + minimap
RIGHT        : Quest tracker, location marker, notifications
BOTTOM LEFT  : Chat tabs (World, Party, Clan, System), messages, input + Send
BOTTOM CENTER/RIGHT : Skills, dodge, potion, attack, quick actions
```

**Phase 1 status** (`Assets/Scripts/UI/PlayerHud.cs`, built in code, 1920×1080 reference):

- [x] Top-left character panel: portrait placeholder, name + level, HP/SP/EXP/Job EXP bars with numbers
- [x] Bottom-center skill bar: Attack, Desert Slash, Dodge with radial cooldown + timer
- [x] Bottom-left chat panel placeholder with tabs and system messages
- [x] Feedback line ("Not enough SP for Desert Slash")
- [ ] Minimap, quest tracker, top-right menu buttons (Phase 2)

## PC controls

| Action | Key |
|---|---|
| Move | W A S D |
| Camera | Mouse |
| Zoom | Mouse wheel |
| Sprint | Left Shift (hold) |
| Jump | Space |
| Dodge | Left Ctrl or Q (direction = movement keys; none = backstep) |
| Light attack | Left mouse |
| Skills | 1–6 |
| Map | M (Phase 2) |
| Skill panel | K (Phase 6) |
| Chat | Enter (Phase 3) |
| Free cursor | Esc (click to recapture) |

## Mobile (Phase 9)

Left-thumb virtual joystick, swipe camera, tap dodge and skills, clustered action buttons,
mobile keyboard for chat, collapsible HUD/quest panel, safe areas, accessible text sizes.
`PlayerInputReader` is the single place to add a touch input source.

## Panels in delivery order

1. Character creation / login
2. Main HUD with chat
3. NPC dialogue + quests
4. Inventory + equipment
5. Skill panel (Skills, Passive, Awaken, City Lineage)
6. Map panel (World / Continent / City)
7. Construction placement
8. Shop rental + storefront
9. City treasury and maintenance report
10. Clan / alliance controls
11. Secret class discovery / cost / lives
