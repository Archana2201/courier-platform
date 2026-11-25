export interface Address {
  name: string;
  line1: string;
  city: string;
  state: string;
  postcode: string;
}

export interface QuoteLine { description: string; amount: number }

export interface Quote {
  serviceLevel: string;
  zone: string;
  chargeableWeightKg: number;
  estimatedDays: number;
  lines: QuoteLine[];
  total: number;
  currency: string;
}

export interface ParcelInput {
  weightKg: number;
  lengthCm: number;
  widthCm: number;
  heightCm: number;
  serviceLevel: string;
}

export interface StatusChange { status: string; location: string | null; atUtc: string }

export interface Shipment {
  id: string;
  trackingNumber: string;
  sender: Address;
  recipient: Address;
  weightKg: number;
  serviceLevel: string;
  price: number;
  currency: string;
  status: string;
  createdAtUtc: string;
  history: StatusChange[];
}

export interface TrackingEvent { status: string; location: string | null; description: string; occurredAtUtc: string }

export interface TrackingResult { trackingNumber: string; currentStatus: string; events: TrackingEvent[] }
