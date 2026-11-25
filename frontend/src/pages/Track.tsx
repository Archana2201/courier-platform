import { useState } from 'react';
import { api } from '../api';
import { statusLabel } from '../status';
import type { TrackingResult } from '../types';

export function Track() {
  const [number, setNumber] = useState('');
  const [result, setResult] = useState<TrackingResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const search = async () => {
    setError(null);
    setResult(null);
    try {
      setResult(await api.track(number));
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <div className="card">
      <h2>Track a parcel</h2>
      <div className="row">
        <input placeholder="Tracking number, e.g. CP260101ABCDEF" value={number} onChange={(e) => setNumber(e.target.value)} />
        <button className="primary" onClick={search} disabled={!number.trim()}>Track</button>
      </div>
      {error && <p className="error" role="alert">{error}</p>}
      {result && (
        <div>
          <h3>{result.trackingNumber} — {statusLabel(result.currentStatus)}</h3>
          <ol className="timeline">
            {result.events.map((e, i) => (
              <li key={i}>
                <strong>{statusLabel(e.status)}</strong> {e.location && <span>· {e.location}</span>}
                <div className="muted">{e.description} · {new Date(e.occurredAtUtc).toLocaleString()}</div>
              </li>
            ))}
          </ol>
          <p className="muted small">Updates can take a couple of seconds to appear after a status change.</p>
        </div>
      )}
    </div>
  );
}
