# Courier Platform

A starter platform for a **new courier / delivery start-up**: instant price quotes, shipment booking, an operations view to move parcels through their lifecycle, and public parcel tracking. Built as **.NET 8 microservices** with a **React + TypeScript** web app.

> Portfolio / reference project. It is a working foundation, not a production system: there is no authentication, payments, label printing or driver app yet (see the roadmap in the architecture doc).

## Services

| Service | Port | Responsibility | Database |
|---|---|---|---|
| Gateway (YARP) | 5100 | Single entry point; routes `/api/quotes`, `/api/shipments`, `/api/tracking` | none |
| Pricing | 5101 | Quotes from rate cards using pluggable pricing rules | `pricing_db` |
| Shipments | 5102 | Books shipments, owns the lifecycle state machine, publishes events | `shipments_db` |
| Tracking | 5103 | Builds the customer timeline from shipment events | `tracking_db` |

```
React UI ─► Gateway ─┬─► Pricing
                     ├─► Shipments ──(HTTP: quote)──► Pricing
                     │        │  outbox ─► event bus ─┐
                     └─► Tracking ◄────────────────────┘
```

## Run it
```bash
docker compose up --build        # UI on http://localhost:8080, gateway on :5100
```
Or run each service with `dotnet run --project backend/src/<Service>` (needs PostgreSQL; see `appsettings.json`) and `cd frontend && npm install && npm run dev`.

## Try the flow
1. **Book a shipment** → get a quote, then book (sender/recipient postcodes drive the zone).
2. **Shipments (ops)** → advance the status: Picked Up → In Transit → Out For Delivery → Delivered.
3. **Track a parcel** → paste the tracking number to see the timeline.

## MCP server
[`mcp-server/`](mcp-server) exposes the platform to AI assistants (quote, book, track) over the Model Context Protocol.

## Tests
```bash
dotnet test backend/tests/Courier.Tests     # pricing rules, state machine, outbox, idempotent tracking
cd frontend && npm test
```

Details: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). SQL reference scripts and seed data are in [`database/`](database).
