export interface Address {
  name: string;
  line1: string;
  city: string;
  state: string;
  postcode: string;
}

export interface Parcel {
  weightKg: number;
  lengthCm: number;
  widthCm: number;
  heightCm: number;
  serviceLevel: 'Standard' | 'Express' | 'Overnight';
}

export class CourierApiError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message);
  }
}

/** Thin typed client for the Courier Platform gateway (see ../README.md). */
export class CourierApi {
  constructor(
    private readonly baseUrl: string = process.env.COURIER_API_URL ?? 'http://localhost:5100',
    private readonly fetchFn: typeof fetch = fetch,
  ) {}

  private async request<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await this.fetchFn(`${this.baseUrl.replace(/\/$/, '')}${path}`, {
      headers: { 'Content-Type': 'application/json' },
      ...init,
    });
    const text = await response.text();
    const body = text ? JSON.parse(text) : null;
    if (!response.ok) {
      throw new CourierApiError(body?.error ?? `Courier API returned ${response.status}`, response.status);
    }
    return body as T;
  }

  quote(parcel: Parcel, originPostcode: string, destinationPostcode: string) {
    return this.request<any>('/api/quotes', {
      method: 'POST',
      body: JSON.stringify({ ...parcel, originPostcode, destinationPostcode }),
    });
  }

  book(sender: Address, recipient: Address, parcel: Parcel) {
    return this.request<any>('/api/shipments', {
      method: 'POST',
      body: JSON.stringify({ sender, recipient, ...parcel }),
    });
  }

  listShipments() {
    return this.request<any[]>('/api/shipments');
  }

  updateStatus(shipmentId: string, status: string, location?: string) {
    return this.request<any>(`/api/shipments/${encodeURIComponent(shipmentId)}/status`, {
      method: 'POST',
      body: JSON.stringify({ status, location }),
    });
  }

  track(trackingNumber: string) {
    return this.request<any>(`/api/tracking/${encodeURIComponent(trackingNumber.trim().toUpperCase())}`);
  }
}
