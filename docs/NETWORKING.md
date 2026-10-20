# Networking and Data Architecture

Nothing here is built yet — multiplayer starts in **Phase 3**. This file fixes the rules early
so offline code does not paint us into a corner.

## Golden rule

**The server decides; the client displays.** The server is authoritative for movement speed,
damage, loot, inventory, currency, lease status and castle capture. The client sends *intent*
("pressed Desert Slash facing 37°"), never results ("dealt 900 damage").

The offline prototype already follows this shape: all numbers come from
`Assets/Scripts/Core`, an engine-free assembly the future server can reuse unchanged.
Scripts that currently resolve hits locally are marked `OFFLINE PROTOTYPE`.

## Logical architecture

```
Unity PC/Mobile Client
        |  secure login/API + real-time game protocol
        v
Authenticated Gateway
        +-- Zone Simulation Server (movement, monsters, combat)
        +-- World/City Service     (ownership, building, map markers)
        +-- Economy Service        (inventory, trades, rental, ledger)
        +-- Chat Service           (moderation, channels, rate limits)
        +-- Class/Quest Service    (stats, XP, unlocks)
        v
     PostgreSQL
```

Start as a **modular monolith + one dedicated zone server**. The diagram shows logical modules,
not microservices to deploy on day one. Add Redis, queues and regional instances only after
profiling shows a need.

## Technology

| Layer | Choice |
|---|---|
| Client | Unity 6 LTS + C#, URP |
| Real-time networking | Dedicated authoritative server. Compare Netcode for GameObjects, FishNet and alternatives with a small spike before committing |
| Backend API | ASP.NET Core (C#) |
| Database | PostgreSQL (transactions, inventories, ownership, leases, ledger) |
| Cache / messaging | Redis — only when actually needed |
| Hosting | One dev server first; managed game-server hosting after load tests |

Cloudflare Workers (or similar) can serve websites/APIs but are **not** a replacement for a
long-lived, low-latency authoritative 3D combat server.

## Movement networking

- Local input prediction + server reconciliation for responsive movement.
- Server validates speed against `StatFormulas` (+ buffs) and rejects impossible moves.
- PvP is server-authoritative; lag compensation applied selectively; test at 80–200 ms.

## Data entities

- accounts, characters, character_stats, jobs, skills, character_skills
- quests, quest_progress, inventories, items, item_instances
- continents, regions, provinces, settlement_sites, cities, city_buildings
- kingdoms, memberships, alliances, territory_claims, sieges
- shops, shop_leases, shop_listings, trades
- wallets, ledger_transactions, ledger_entries, maintenance_charges
- secret_class_versions, secret_class_allocations, qualifying_deaths
- audit_logs, support_actions, outbox_events

Use transactional writes plus **outbox events** for map/notification propagation. Test
simultaneous plot claims and simultaneous shop rentals.

## Security baseline

- Authenticated sessions, reconnect tokens, rate limits, chat abuse controls.
- Backups **and** practiced restore/rollback procedures.
- Exactly-once *business effects* via database uniqueness / idempotency keys — never rely on
  the network delivering a message exactly once.
- Never ship database credentials or privileged keys inside the Unity client.
- Collect only necessary account data; write a privacy policy before public release.
