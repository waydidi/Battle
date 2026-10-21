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
- [x] Top-right menu buttons (Shop, Event, **Skill**, Clan, Menu); Skill opens the skill window
- [x] Skill window (`Assets/Scripts/UI/SkillWindow.cs`): tabs, three branches, locks, details, next-level preview, Level Up
- [ ] Minimap and quest tracker in Unity (both already work in the web prototype)

## Bird's-eye view (default camera of the web prototype)

Reference: top-down MMO layout at a fixed 45° angle, the character small in the centre.

- Camera looks down at Storm (~53° pitch), follows him, and **zooms straight towards / away from him**
  (mouse wheel, pinch, or the **+ / −** buttons; 7–60 m). **View 45°** (or R) rotates the camera in
  45° steps, right-drag rotates freely, **↺** resets. **V** switches to the close follow camera.
- The ground is a **grid of 1 m squares** (drawn in the ground shader, G toggles it). Hovering the
  mouse highlights the square under it (gold = walkable, red = blocked).
- **Click to move:** clicking a square walks Storm there along an A* path on the same grid
  (walls, houses, stalls and palms are blocked squares). Holding the button keeps steering.
  The destination square stays highlighted in cyan and the route shows on the minimap.
- Clicking a slime targets it (red ring) and attacks; clicking an NPC walks over and talks.
- **AUTO** hunts the nearest slime, walking out to the desert if needed, and uses ready skills.
- HUD layout: portrait + HP/SP/XP top-left, pill menus top-centre (Bag, Party, Quests, Collection,
  Guide, Settings / Daily Quests, Ranking, Guild, Skills), world-boss panel left, location +
  channel + day/night clock + minimap + FPS top-right, camera tools beside the minimap, quest
  tracker right (with "walk to" link), chat bottom-left, six-slot skill bar bottom-centre,
  HP/SP potions (Q/E), attack button and AUTO bottom-right, name tag above the character.

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
| Skill panel | K |
| Sit / stand | X (HP/SP regenerate 2× faster) |
| Sleep / wake | Z (HP/SP regenerate 3× faster) |
| Chat | Enter (Phase 3) |
| Free cursor | Esc (click to recapture) |
| Walk / run toggle | C (web prototype) |
| Web prototype (bird's-eye) | click ground = move, click slime = attack, F attack, Space dodge, 1–6 skills, Q/E potions, T talk, R rotate, V camera mode, G grid |

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
