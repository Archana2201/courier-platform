# Courier MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) server that lets an AI assistant (Claude Desktop, Claude Code, VS Code, ...) work with the **Courier Platform** API in this repo: get quotes, book shipments, track parcels and move shipments through their lifecycle.

It is a thin, typed adapter over the gateway (`/api/quotes`, `/api/shipments`, `/api/tracking`), so all business rules (pricing, the shipment state machine) stay in the .NET services.

## Tools

| Tool | Effect | What it does |
|---|---|---|
| `get_quote` | read-only | Price a parcel between two postcodes |
| `list_shipments` | read-only | Latest shipments and statuses |
| `track_parcel` | read-only | Tracking timeline for a tracking number |
| `book_shipment` | creates a booking | Books a shipment, returns tracking number and price |
| `update_shipment_status` | changes state | Ops action; illegal transitions are rejected by the API |

API failures are returned as tool errors (`isError: true`) with the API's message, so the assistant can explain or recover rather than crash. Inputs are validated with zod before any API call.

## Use it
```bash
cd mcp-server && npm install && npm run build
```
Start the platform first (`docker compose up --build` in the repo root; gateway on :5100), then register the server with your MCP client, for example in Claude Desktop's config:

```json
{
  "mcpServers": {
    "courier": {
      "command": "node",
      "args": ["/absolute/path/to/courier-platform/mcp-server/dist/index.js"],
      "env": { "COURIER_API_URL": "http://localhost:5100" }
    }
  }
}
```
Then ask things like *"Quote a 5 kg Express parcel from 2000 to 3000"* or *"Where is parcel CP261125K7M2QA?"*.

## Tests
`npm test` starts a fake gateway, connects a real MCP client to the server over an in-memory transport and exercises every tool, including error paths and input validation.

## Notes
- Uses stdio transport; logs go to stderr because stdout carries the protocol.
- No authentication yet (the platform has none). Add API keys at the gateway before exposing this beyond local use.
