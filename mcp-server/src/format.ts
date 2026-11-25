const money = (n: number, currency = 'AUD') =>
  new Intl.NumberFormat('en-AU', { style: 'currency', currency }).format(n);

export function formatQuote(q: any): string {
  const lines = q.lines.map((l: any) => `  - ${l.description}: ${money(l.amount, q.currency)}`).join('\n');
  return [
    `${q.serviceLevel} quote: ${money(q.total, q.currency)} (zone ${q.zone}, chargeable ${q.chargeableWeightKg} kg, about ${q.estimatedDays} day(s))`,
    lines,
  ].join('\n');
}

export function formatShipment(s: any): string {
  return `${s.trackingNumber}: ${s.sender.city} -> ${s.recipient.city}, ${s.serviceLevel}, ${money(s.price, s.currency)}, status ${s.status} (id ${s.id})`;
}

export function formatTracking(t: any): string {
  const events = t.events
    .map((e: any) => `  - ${e.occurredAtUtc}: ${e.status}${e.location ? ` @ ${e.location}` : ''} - ${e.description}`)
    .join('\n');
  return `${t.trackingNumber} is ${t.currentStatus}\n${events}`;
}
