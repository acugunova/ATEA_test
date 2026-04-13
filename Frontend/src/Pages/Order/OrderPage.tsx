import React, { memo, useCallback, useState } from 'react';
import { submitOrder } from 'services/api';

import { Field } from 'components/Field';

import { GATEWAYS } from '../../constants';
import type { FormErrors, OrderFormState, Receipt } from '../../types';

const INITIAL_STATE: OrderFormState = {
  orderNumber: '',
  userId: '',
  amount: '',
  gatewayId: 'gateway-alpha',
  description: '',
};

function validate(form: OrderFormState): FormErrors {
  const errors: FormErrors = {};

  if (!form.orderNumber.trim()) {
    errors.orderNumber = 'Order number is required.';
  }
  if (!form.userId.trim()) {
    errors.userId = 'User ID is required.';
  }
  const amt = parseFloat(form.amount);
  if (!form.amount || isNaN(amt) || amt <= 0) {
    errors.amount = 'Enter a valid positive amount.';
  }
  if (!form.gatewayId) {
    errors.gatewayId = 'Select a payment gateway.';
  }

  return errors;
}

export default memo(() => {
  const [form, setForm] = useState<OrderFormState>(INITIAL_STATE);
  const [errors, setErrors] = useState<FormErrors>({});
  const [loading, setLoading] = useState(false);

  const onResult = useCallback((r: Receipt | string) => {
    if ((r as Receipt).paymentConfirmation)
      alert(`Order ${(r as Receipt).orderNumber} is saved successfully. Payment confirmation: ${(r as Receipt).paymentConfirmation}`);
    else alert(r);
  }, []);

  function setField<K extends keyof OrderFormState>(field: K): React.ChangeEventHandler<HTMLInputElement | HTMLSelectElement> {
    return (e) => setForm((prev: any) => ({ ...prev, [field]: e.target.value }));
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>): Promise<void> {
    e.preventDefault();

    const errs = validate(form);
    if (Object.keys(errs).length > 0) {
      setErrors(errs);
      return;
    }
    setErrors({});
    setLoading(true);

    try {
      const receipt = await submitOrder({
        orderNumber: form.orderNumber.trim(),
        userId: form.userId.trim(),
        amount: parseFloat(form.amount),
        gatewayId: form.gatewayId,
        description: form.description.trim() || null,
      });
      onResult(receipt);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Payment failed.';
      onResult(message);
    } finally {
      setLoading(false);
    }
  }
  return (
    <>
      <form className="card order-form" onSubmit={handleSubmit} noValidate>
        <h2 className="card-title">New Order</h2>

        <div className="field-row">
          <Field label="Order Number *" error={errors.orderNumber}>
            <input
              value={form.orderNumber}
              onChange={setField('orderNumber')}
              placeholder="e.g. ORD-2024-001"
              className={errors.orderNumber ? 'error' : ''}
            />
          </Field>

          <Field label="User ID *" error={errors.userId}>
            <input value={form.userId} onChange={setField('userId')} placeholder="e.g. user-42" className={errors.userId ? 'error' : ''} />
          </Field>
        </div>

        <div className="field-row">
          <Field label="Amount (USD) *" error={errors.amount}>
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={form.amount}
              onChange={setField('amount')}
              placeholder="0.00"
              className={errors.amount ? 'error' : ''}
            />
          </Field>

          <Field label="Payment Gateway *" error={errors.gatewayId}>
            <select value={form.gatewayId} onChange={setField('gatewayId')}>
              {GATEWAYS.map((g: any) => (
                <option key={g.id} value={g.id}>
                  {g.label} – {g.hint}
                </option>
              ))}
            </select>
          </Field>
        </div>

        <Field label="Description (optional)">
          <input value={form.description} onChange={setField('description')} placeholder="Order description..." />
        </Field>

        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading && <span className="spinner" />}
          {loading ? 'Processing…' : 'Submit Order'}
        </button>
      </form>
    </>
  );
});
