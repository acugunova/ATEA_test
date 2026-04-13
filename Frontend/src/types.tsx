export interface Order {
  orderNumber: string;
  userId: string;
  amount: number;
  gatewayId: string;
  description: string | null;
}

export interface Receipt {
  orderNumber: string;
  amount: number;
  timestamp: string;
  paymentConfirmation: string;
}

export interface OrderFormState {
  orderNumber: string;
  userId: string;
  amount: string;
  gatewayId: string;
  description: string;
}

export interface GatewayOption {
  id: string;
  label: string;
  hint: string;
}

export type FormErrors = Partial<Record<keyof OrderFormState, string>>;
