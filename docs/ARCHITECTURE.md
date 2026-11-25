# Architecture

## Goals
Give a courier start-up a clean starting point: independently deployable services, one database per service, reliable events between them, and a UI that exercises the whole flow.

## Services and boundaries
- **Pricing** is stateless apart from `rate_cards`. It never calls other services.
- **Shipments** is the system of record for parcels. It calls Pricing synchronously to price a booking, then stores the shipment.
- **Tracking** is a read model. It never calls Shipments; it only consumes events, so it keeps working if Shipments is down.
- **Gateway** hides internal addresses and gives the UI a single origin.
- **BuildingBlocks** is a small shared library: event contracts, `IEventBus`, the outbox and its dispatcher, database start-up retry. It contains no business logic.

## Design patterns

| Pattern | Where | Why |
|---|---|---|
| Strategy | `IPricingRule` (base rate, weight, zone surcharge, fuel surcharge) | New pricing concerns are added as classes; `QuoteCalculator` runs them in order. |
| State | `ShipmentState` and its concrete states | Illegal lifecycle jumps (e.g. Created → Delivered) are rejected in the domain. |
| Factory | `ShipmentFactory` | One place validates and builds a new shipment and its first history entry. |
| Transactional Outbox | `outbox_messages` + `OutboxDispatcher<T>` | The shipment change and its event commit atomically; events are relayed at-least-once. |
| Idempotent consumer | `TrackingEventHandler` + unique `SourceEventId` | Redelivered events do not create duplicate timeline rows. |
| Port / Adapter | `IPricingClient`, `IEventBus` | HTTP today; swap for Kafka/RabbitMQ or a test fake without touching callers. |
| API Gateway | YARP | Routing and a single public surface. |
| Database per service | three logical databases | Services own their schema and can evolve independently. |

## Shipment lifecycle
```
Created ─► PickedUp ─► InTransit ─► OutForDelivery ─► Delivered
   │                     ▲  │ (hub scans)    │
   └─► Cancelled         └──┘ ◄──────────────┘ (failed attempt)
```
`Delivered` and `Cancelled` are terminal.

## Pricing model
Chargeable weight = max(actual kg, L×W×H / 5000). Price = base rate for the service level (covers the first kg) + extra kg × per-kg rate + zone surcharge (Interstate $5, Remote $12) + 8% fuel surcharge on the subtotal. Zones come from the first digit of AU postcodes (remote: 0, 6, 7, 8). Rate cards live in `rate_cards` (seeded with Standard/Express/Overnight).

## Data
Table definitions and seed data for each database are in `database/*.sql`. At start-up each service creates its schema with EF Core `EnsureCreated`; for a real deployment replace this with EF migrations.

## Event flow
1. `POST /api/shipments` → Shipments prices the parcel, saves the shipment and a `ShipmentCreated` outbox row in one transaction.
2. `OutboxDispatcher` publishes unprocessed rows (oldest first) to the bus; failures increment `Attempts` and are retried.
3. The HTTP bus POSTs the envelope to Tracking's `/api/events`; Tracking writes a timeline row unless it has seen the `EventId`.
4. `POST /api/shipments/{id}/status` follows the same path with `ShipmentStatusChanged`.

## Known gaps / roadmap
Authentication and customer accounts, payments, driver/scanner app, proof of delivery, address validation, real message broker, EF migrations, observability (OpenTelemetry), retries/circuit breaker on the Pricing call.
