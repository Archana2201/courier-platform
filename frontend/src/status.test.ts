import { describe, expect, it } from 'vitest';
import { formatMoney, nextStatuses, statusLabel } from './status';

describe('status helpers', () => {
  it('offers only legal next statuses', () => {
    expect(nextStatuses('Created')).toEqual(['PickedUp', 'Cancelled']);
    expect(nextStatuses('Delivered')).toEqual([]);
    expect(nextStatuses('Unknown')).toEqual([]);
  });

  it('turns PascalCase statuses into labels', () => {
    expect(statusLabel('OutForDelivery')).toBe('Out For Delivery');
    expect(statusLabel('Created')).toBe('Created');
  });

  it('formats money in AUD', () => {
    expect(formatMoney(32.29)).toContain('32.29');
  });
});
