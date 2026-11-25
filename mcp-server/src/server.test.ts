import http from 'node:http';
import type { AddressInfo } from 'node:net';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { InMemoryTransport } from '@modelcontextprotocol/sdk/inMemory.js';
import { CourierApi } from './courierApi.js';
import { createServer } from './server.js';

const addr = (city: string, postcode: string) => ({ name: 'Test', line1: '1 St', city, state: 'NSW', postcode });
const shipment = {
  id: '11111111-1111-1111-1111-111111111111',
  trackingNumber: 'CP261125K7M2QA',
  sender: addr('Sydney', '2000'),
  recipient: addr('Melbourne', '3000'),
  serviceLevel: 'Express',
  price: 32.29,
  currency: 'AUD',
  status: 'Created',
};

let gateway: http.Server;
let client: Client;
const seen: { method?: string; url?: string; body?: any }[] = [];

beforeAll(async () => {
  gateway = http.createServer((req, res) => {
    let raw = '';
    req.on('data', (c) => (raw += c));
    req.on('end', () => {
      seen.push({ method: req.method, url: req.url, body: raw ? JSON.parse(raw) : undefined });
      const send = (code: number, body: unknown) => {
        res.writeHead(code, { 'content-type': 'application/json' });
        res.end(JSON.stringify(body));
      };
      if (req.url === '/api/quotes')
        return send(200, {
          serviceLevel: 'Express', zone: 'Interstate', chargeableWeightKg: 5, estimatedDays: 2, currency: 'AUD', total: 32.29,
          lines: [{ description: 'Express base rate (first kg)', amount: 14.5 }],
        });
      if (req.url === '/api/shipments' && req.method === 'POST') return send(201, shipment);
      if (req.url === '/api/shipments') return send(200, [shipment]);
      if (req.url?.endsWith('/status')) return send(409, { error: 'Cannot move a shipment from Created to Delivered.' });
      if (req.url === '/api/tracking/CP261125K7M2QA')
        return send(200, {
          trackingNumber: 'CP261125K7M2QA', currentStatus: 'PickedUp',
          events: [{ status: 'PickedUp', location: 'Sydney', description: 'Parcel picked up by courier', occurredAtUtc: '2026-11-25T01:00:00Z' }],
        });
      send(404, { error: 'No tracking information for that number.' });
    });
  });
  await new Promise<void>((r) => gateway.listen(0, r));
  const port = (gateway.address() as AddressInfo).port;

  const server = createServer(new CourierApi(`http://127.0.0.1:${port}`));
  const [clientTransport, serverTransport] = InMemoryTransport.createLinkedPair();
  client = new Client({ name: 'test-client', version: '1.0.0' });
  await Promise.all([server.connect(serverTransport), client.connect(clientTransport)]);
});

afterAll(async () => {
  await client.close();
  gateway.close();
});

const text = (result: any) => result.content[0].text as string;

describe('courier MCP server', () => {
  it('exposes the five tools with read-only hints where appropriate', async () => {
    const { tools } = await client.listTools();
    expect(tools.map((t) => t.name).sort()).toEqual(
      ['book_shipment', 'get_quote', 'list_shipments', 'track_parcel', 'update_shipment_status'],
    );
    expect(tools.find((t) => t.name === 'get_quote')?.annotations?.readOnlyHint).toBe(true);
    expect(tools.find((t) => t.name === 'book_shipment')?.annotations?.readOnlyHint).toBe(false);
  });

  it('get_quote forwards the parcel and postcodes and formats the answer', async () => {
    const result = await client.callTool({
      name: 'get_quote',
      arguments: { weightKg: 5, lengthCm: 20, widthCm: 20, heightCm: 20, serviceLevel: 'Express', originPostcode: '2000', destinationPostcode: '3000' },
    });
    expect(text(result)).toContain('$32.29');
    const call = seen.find((s) => s.url === '/api/quotes');
    expect(call?.body).toMatchObject({ weightKg: 5, originPostcode: '2000', destinationPostcode: '3000' });
  });

  it('book_shipment returns the tracking number', async () => {
    const result = await client.callTool({
      name: 'book_shipment',
      arguments: { sender: addr('Sydney', '2000'), recipient: addr('Melbourne', '3000'), weightKg: 5, lengthCm: 20, widthCm: 20, heightCm: 20, serviceLevel: 'Express' },
    });
    expect(text(result)).toContain('CP261125K7M2QA');
  });

  it('list_shipments and track_parcel return readable text', async () => {
    expect(text(await client.callTool({ name: 'list_shipments', arguments: {} }))).toContain('Sydney -> Melbourne');
    const tracked = await client.callTool({ name: 'track_parcel', arguments: { trackingNumber: ' cp261125k7m2qa ' } });
    expect(text(tracked)).toContain('PickedUp @ Sydney');
  });

  it('turns API failures into tool errors instead of crashing', async () => {
    const bad = await client.callTool({ name: 'track_parcel', arguments: { trackingNumber: 'NOPE' } });
    expect(bad.isError).toBe(true);
    expect(text(bad)).toContain('No tracking information');

    const illegal = await client.callTool({
      name: 'update_shipment_status',
      arguments: { shipmentId: shipment.id, status: 'Delivered' },
    });
    expect(illegal.isError).toBe(true);
    expect(text(illegal)).toContain('Cannot move');
  });

  it('rejects invalid input before calling the API', async () => {
    const before = seen.length;
    const result = await client
      .callTool({ name: 'get_quote', arguments: { weightKg: -1, lengthCm: 1, widthCm: 1, heightCm: 1, serviceLevel: 'Express', originPostcode: '2000', destinationPostcode: '3000' } })
      .catch((e) => ({ isError: true, content: [{ text: String(e) }] }));
    expect((result as any).isError).toBe(true);
    expect(seen.length).toBe(before);
  });
});
