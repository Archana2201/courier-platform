import { useState } from 'react';
import { api } from '../api';
import { formatMoney } from '../status';
import type { Address, ParcelInput, Quote, Shipment } from '../types';

const emptyAddress = (): Address => ({ name: '', line1: '', city: '', state: '', postcode: '' });

function AddressFields({ title, value, onChange }: { title: string; value: Address; onChange: (a: Address) => void }) {
  const set = (key: keyof Address) => (e: React.ChangeEvent<HTMLInputElement>) => onChange({ ...value, [key]: e.target.value });
  return (
    <fieldset>
      <legend>{title}</legend>
      <input placeholder="Name" value={value.name} onChange={set('name')} />
      <input placeholder="Address line" value={value.line1} onChange={set('line1')} />
      <div className="row">
        <input placeholder="City" value={value.city} onChange={set('city')} />
        <input placeholder="State" value={value.state} onChange={set('state')} />
        <input placeholder="Postcode" value={value.postcode} onChange={set('postcode')} />
      </div>
    </fieldset>
  );
}

export function BookShipment({ onBooked }: { onBooked: () => void }) {
  const [sender, setSender] = useState<Address>(emptyAddress());
  const [recipient, setRecipient] = useState<Address>(emptyAddress());
  const [parcel, setParcel] = useState<ParcelInput>({ weightKg: 1, lengthCm: 20, widthCm: 20, heightCm: 10, serviceLevel: 'Standard' });
  const [quote, setQuote] = useState<Quote | null>(null);
  const [booked, setBooked] = useState<Shipment | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const num = (key: keyof ParcelInput) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setParcel({ ...parcel, [key]: Number(e.target.value) });

  const run = async (action: () => Promise<void>) => {
    setBusy(true);
    setError(null);
    try {
      await action();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const getQuote = () => run(async () => {
    setBooked(null);
    setQuote(await api.quote(parcel, sender.postcode, recipient.postcode));
  });

  const book = () => run(async () => {
    const shipment = await api.createShipment(sender, recipient, parcel);
    setBooked(shipment);
    setQuote(null);
    onBooked();
  });

  return (
    <div className="card">
      <h2>Book a shipment</h2>
      <div className="grid-two">
        <AddressFields title="Sender" value={sender} onChange={setSender} />
        <AddressFields title="Recipient" value={recipient} onChange={setRecipient} />
      </div>
      <fieldset>
        <legend>Parcel</legend>
        <div className="row">
          <label>Weight (kg) <input type="number" min="0.1" step="0.1" value={parcel.weightKg} onChange={num('weightKg')} /></label>
          <label>Length (cm) <input type="number" min="1" value={parcel.lengthCm} onChange={num('lengthCm')} /></label>
          <label>Width (cm) <input type="number" min="1" value={parcel.widthCm} onChange={num('widthCm')} /></label>
          <label>Height (cm) <input type="number" min="1" value={parcel.heightCm} onChange={num('heightCm')} /></label>
          <label>
            Service
            <select value={parcel.serviceLevel} onChange={(e) => setParcel({ ...parcel, serviceLevel: e.target.value })}>
              <option>Standard</option>
              <option>Express</option>
              <option>Overnight</option>
            </select>
          </label>
        </div>
      </fieldset>

      <div className="actions">
        <button onClick={getQuote} disabled={busy}>Get quote</button>
        <button className="primary" onClick={book} disabled={busy}>Book shipment</button>
      </div>

      {error && <p className="error" role="alert">{error}</p>}

      {quote && (
        <div className="quote">
          <h3>Quote: {formatMoney(quote.total, quote.currency)}</h3>
          <p className="muted">
            {quote.serviceLevel} · zone {quote.zone} · chargeable {quote.chargeableWeightKg} kg · about {quote.estimatedDays} day(s)
          </p>
          <ul>
            {quote.lines.map((l) => (
              <li key={l.description}>{l.description}: {formatMoney(l.amount, quote.currency)}</li>
            ))}
          </ul>
        </div>
      )}

      {booked && (
        <div className="success" role="status">
          Booked! Tracking number <strong>{booked.trackingNumber}</strong> ({formatMoney(booked.price, booked.currency)}).
        </div>
      )}
    </div>
  );
}
