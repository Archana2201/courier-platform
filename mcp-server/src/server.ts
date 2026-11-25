import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { CourierApi, CourierApiError } from './courierApi.js';
import { formatQuote, formatShipment, formatTracking } from './format.js';

const serviceLevel = z.enum(['Standard', 'Express', 'Overnight']).describe('Delivery speed');

const parcelShape = {
  weightKg: z.number().positive().max(70).describe('Parcel weight in kilograms (max 70)'),
  lengthCm: z.number().positive().describe('Length in cm'),
  widthCm: z.number().positive().describe('Width in cm'),
  heightCm: z.number().positive().describe('Height in cm'),
  serviceLevel,
};

const addressSchema = z.object({
  name: z.string().min(1),
  line1: z.string().min(1).describe('Street address'),
  city: z.string().min(1),
  state: z.string().min(1).describe('e.g. NSW'),
  postcode: z.string().min(1).describe('Australian postcode'),
});

type ToolResult = { content: { type: 'text'; text: string }[]; isError?: boolean };

const ok = (text: string): ToolResult => ({ content: [{ type: 'text', text }] });

/** Turns API failures into tool errors the model can read and recover from, instead of protocol errors. */
async function guard(action: () => Promise<string>): Promise<ToolResult> {
  try {
    return ok(await action());
  } catch (error) {
    const message =
      error instanceof CourierApiError
        ? `Courier API error (${error.status}): ${error.message}`
        : `Could not reach the Courier API: ${(error as Error).message}`;
    return { content: [{ type: 'text', text: message }], isError: true };
  }
}

export function createServer(api: CourierApi = new CourierApi()): McpServer {
  const server = new McpServer({ name: 'courier-mcp-server', version: '1.0.0' });

  server.registerTool(
    'get_quote',
    {
      title: 'Get a delivery quote',
      description: 'Price a parcel between two Australian postcodes. Read-only; does not book anything.',
      inputSchema: {
        ...parcelShape,
        originPostcode: z.string().min(1),
        destinationPostcode: z.string().min(1),
      },
      annotations: { readOnlyHint: true },
    },
    ({ originPostcode, destinationPostcode, ...parcel }) =>
      guard(async () => formatQuote(await api.quote(parcel, originPostcode, destinationPostcode))),
  );

  server.registerTool(
    'book_shipment',
    {
      title: 'Book a shipment',
      description:
        'Books a real shipment and returns its tracking number and price. This creates a record, so confirm the details with the user first.',
      inputSchema: { sender: addressSchema, recipient: addressSchema, ...parcelShape },
      annotations: { readOnlyHint: false, destructiveHint: false },
    },
    ({ sender, recipient, ...parcel }) =>
      guard(async () => `Booked. ${formatShipment(await api.book(sender, recipient, parcel))}`),
  );

  server.registerTool(
    'list_shipments',
    {
      title: 'List recent shipments',
      description: 'Lists the 50 most recent shipments with their current status.',
      inputSchema: {},
      annotations: { readOnlyHint: true },
    },
    () =>
      guard(async () => {
        const shipments = await api.listShipments();
        return shipments.length ? shipments.map(formatShipment).join('\n') : 'No shipments yet.';
      }),
  );

  server.registerTool(
    'track_parcel',
    {
      title: 'Track a parcel',
      description: 'Returns the full tracking timeline for a tracking number such as CP261125K7M2QA.',
      inputSchema: { trackingNumber: z.string().min(1) },
      annotations: { readOnlyHint: true },
    },
    ({ trackingNumber }) => guard(async () => formatTracking(await api.track(trackingNumber))),
  );

  server.registerTool(
    'update_shipment_status',
    {
      title: 'Update shipment status',
      description:
        'Operations action: moves a shipment to its next lifecycle status (PickedUp, InTransit, OutForDelivery, Delivered, Cancelled). Illegal transitions are rejected by the API.',
      inputSchema: {
        shipmentId: z.string().describe('Shipment id (GUID) from list_shipments or book_shipment'),
        status: z.enum(['PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Cancelled']),
        location: z.string().optional().describe('Where the scan happened'),
      },
      annotations: { readOnlyHint: false, destructiveHint: false, idempotentHint: false },
    },
    ({ shipmentId, status, location }) =>
      guard(async () => `Updated. ${formatShipment(await api.updateStatus(shipmentId, status, location))}`),
  );

  return server;
}
