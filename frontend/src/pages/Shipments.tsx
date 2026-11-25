import { useCallback, useEffect, useState } from 'react';
import { api } from '../api';
import { formatMoney, nextStatuses, statusLabel } from '../status';
import type { Shipment } from '../types';

export function Shipments({ refreshKey }: { refreshKey: number }) {
  const [shipments, setShipments] = useState<Shipment[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    api.shipments().then(setShipments).catch((e: Error) => setError(e.message));
  }, []);

  useEffect(load, [load, refreshKey]);

  const advance = async (shipment: Shipment, status: string) => {
    setError(null);
    try {
      const location = status === 'Cancelled' ? '' : shipment.recipient.city;
      await api.updateStatus(shipment.id, status, location);
      load();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <div className="card">
      <h2>Shipments</h2>
      {error && <p className="error" role="alert">{error}</p>}
      {shipments.length === 0 ? (
        <p className="muted">No shipments yet. Book one first.</p>
      ) : (
        <table>
          <thead>
            <tr><th>Tracking</th><th>Route</th><th>Service</th><th>Price</th><th>Status</th><th>Actions</th></tr>
          </thead>
          <tbody>
            {shipments.map((s) => (
              <tr key={s.id}>
                <td><strong>{s.trackingNumber}</strong></td>
                <td>{s.sender.city} → {s.recipient.city}</td>
                <td>{s.serviceLevel}</td>
                <td>{formatMoney(s.price, s.currency)}</td>
                <td><span className={`pill ${s.status}`}>{statusLabel(s.status)}</span></td>
                <td className="actions">
                  {nextStatuses(s.status).map((n) => (
                    <button key={n} onClick={() => advance(s, n)}>{statusLabel(n)}</button>
                  ))}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
