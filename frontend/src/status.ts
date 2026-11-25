/** Mirrors the state machine in Courier.Shipments (ShipmentState). Keep both in sync. */
const NEXT: Record<string, string[]> = {
  Created: ['PickedUp', 'Cancelled'],
  PickedUp: ['InTransit'],
  InTransit: ['InTransit', 'OutForDelivery'],
  OutForDelivery: ['Delivered', 'InTransit'],
  Delivered: [],
  Cancelled: [],
};

export function nextStatuses(current: string): string[] {
  return NEXT[current] ?? [];
}

export function statusLabel(status: string): string {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function formatMoney(amount: number, currency = 'AUD'): string {
  return new Intl.NumberFormat('en-AU', { style: 'currency', currency }).format(amount);
}
