import type { Address, ParcelInput, Quote, Shipment, TrackingResult } from './types';

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });
  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try {
      const body = await response.json();
      if (body?.error) message = body.error;
    } catch {
      /* body was not JSON */
    }
    throw new Error(message);
  }
  return (await response.json()) as T;
}

export const api = {
  quote: (parcel: ParcelInput, originPostcode: string, destinationPostcode: string) =>
    request<Quote>('/api/quotes', {
      method: 'POST',
      body: JSON.stringify({ ...parcel, originPostcode, destinationPostcode }),
    }),
  createShipment: (sender: Address, recipient: Address, parcel: ParcelInput) =>
    request<Shipment>('/api/shipments', {
      method: 'POST',
      body: JSON.stringify({ sender, recipient, ...parcel }),
    }),
  shipments: () => request<Shipment[]>('/api/shipments'),
  updateStatus: (id: string, status: string, location: string) =>
    request<Shipment>(`/api/shipments/${id}/status`, {
      method: 'POST',
      body: JSON.stringify({ status, location }),
    }),
  track: (trackingNumber: string) =>
    request<TrackingResult>(`/api/tracking/${encodeURIComponent(trackingNumber.trim())}`),
};
