import type { Order, Receipt } from '../types';

const API_BASE = process.env.REACT_APP_API_URL ?? 'https://localhost:53386';

interface ApiErrorBody {
  error?: string;
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (res.ok) return res.json() as Promise<T>;

  let message = `HTTP ${res.status}`;
  try {
    const body = (await res.json()) as ApiErrorBody;
    if (body.error) message = body.error;
  } catch {
    // ignore JSON parse failure – keep the HTTP status message
  }
  throw new Error(message);
}

export async function submitOrder(payload: Order): Promise<Receipt> {
  const res = await fetch(`${API_BASE}/api/order`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  return handleResponse<Receipt>(res);
}

export async function fetchOrders(): Promise<Order[]> {
  const res = await fetch(`${API_BASE}/api/orders`);
  return handleResponse<Order[]>(res);
}
