import type { GatewayOption } from './types';

export const GATEWAYS: GatewayOption[] = [
  { id: 'gateway-alpha', label: 'Alpha Gateway', hint: 'Always succeeds' },
  { id: 'gateway-beta', label: 'Beta Gateway', hint: 'Fails if amount > $1,000' },
  { id: 'gateway-gamma', label: 'Gamma Gateway', hint: 'Randomly fails ~30% (retry demo)' },
];
