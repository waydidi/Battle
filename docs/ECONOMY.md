# Economy

## Roles

Adventurer (loot), Gatherer (wood/ore/herbs), Crafter (equipment), Merchant (rents a shop),
Carrier (trades between cities), City Owner (landlord, treasury, maintenance),
Governor (delegated city manager).

## Currency

- One primary currency: **Gold**, stored as **integer units** (never floats).
- **Sources:** limited quests, rewards, approved NPC buy orders.
- **Sinks (destroyed):** repairs, crafting fees, paid fast travel, NPC administration and
  maintenance fees.
- **Transfers (not sinks):** player trades, merchant revenue, shop rent, city market tax.
- Track: currency created/destroyed, median prices, shop utilization, wealth concentration.
- No real-money Gold or combat advantages during MVP. No real-money cash-out.

## Ledger — mandatory before real users trade

- Immutable double-entry postings (or equivalent audit-safe balances).
- Every buy / rent / refund request carries a unique **idempotency key**; the database enforces
  uniqueness so a reconnect or double-click can never apply twice.
- Correct transaction isolation and server-side validation.
- Never implement a transfer as two unrelated client API calls.

Example rent transaction (one ledger transaction, two entries, sums to zero):

```
Merchant wallet  -300 Gold
City treasury    +300 Gold
```

An NPC fee goes to a system sink account and is counted as destroyed supply in analytics.

## Shop rental flow

1. City owner builds a marketplace with rentable units.
2. Owner sets rent within system-defined regional min/max bands.
3. Merchant picks an available unit on the city map and previews lease terms.
4. Merchant pays optional deposit + first rent into a server-controlled ledger/escrow.
5. Inventory and public listing go live **only after the payment commits**.
6. Sales credit the merchant minus transaction tax/fee.
7. Auto-renew only if the merchant opted in and has funds.
8. Nonpayment → grace period → new listings paused after expiry; items stay owned by the
   merchant and are returned via a safe claim system.
9. On city conquest, active leases are honored for their remaining term (or compensated by a
   defined rule). Merchant items are never confiscated.

### Starting rents (7 days, to be tuned)

| Space | Gold / 7 days |
|---|---:|
| Street stall | 100 |
| Small shop | 300 |
| Medium shop | 700 |
| Premium market shop | 1,500 |
| Trading house | 3,000 |

## City finances

```
revenue    = collected_rent + collected_city_sales_tax + service_fees + NPC_subsidies (if any)
net_change = revenue - guards - infrastructure - utilities - NPC_service_costs - repairs - administrative_fees
```

Example: 20 small × 300 + 10 medium × 700 + 3 premium × 1,500 = 17,500 Gold/week of rent,
plus sales tax from real trade volume. Never assume full occupancy.

**Safeguards**

- Maintenance is paid from the city treasury with an audit log. City treasury and the owner's
  personal wallet are separate accounts.
- Low funds first disable optional services and slow repairs. A short deficit never deletes
  merchant items or houses.

**Anti-monopoly**

Caps on tax/rent, escalating land maintenance, reclamation of long-abandoned cities, protected
starter plots, market competition, NPC baseline stores for essentials.

## Phase 5 exit test

At least 100 automated simultaneous rent/buy/refund scenarios pass with no duplicate charges
and no lost items.
