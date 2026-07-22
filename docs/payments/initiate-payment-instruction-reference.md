# `POST /api/v1/payments/initiate` — PaymentInstruction reference

This documents the exact `PaymentInstruction` dictionary keys returned per provider, so the
frontend can build the BankTransfer account-details card and the PaymentLink redirect button
without guessing at key names. (Frontend Phase 5b, ask **P2a**.)

## Response envelope

`200 OK` → `InitiatePaymentResponse`:

| Field | Type | Notes |
|---|---|---|
| `merchantReference` | `string` | Our reference for the transaction; stable across the payment lifecycle. |
| `paymentStatus` | `string` | Always `"Initiated"` on a successful initiate. (Enum: `PendingInitiation`, `Initiated`, `Succeeded`, `Failed`, `Cancelled`, `Expired`.) |
| `paymentProviderId` | `Guid` | The provider row that handled initiation. |
| `paymentProviderName` | `string` | The provider's seeded display name. **Do not branch on this** — it's free-form config; branch on `paymentMethodType`. |
| `expiresAtUtc` | `DateTimeOffset?` | Set for BankTransfer (now + 15 min), `null` for PaymentLink. |
| `paymentMethodType` | `string` | `"PaymentLink"` or `"BankTransfer"`. **This is the field to switch the UI on.** |
| `paymentInstruction` | `Dictionary<string,string>` | Free-form; keys documented below, keyed per `paymentMethodType`. |

The provider is chosen server-side from `paymentProviderId` in the request; the caller does not
pick the method type directly. `paymentMethodType` tells you which instruction shape you got back.

---

## `paymentMethodType: "PaymentLink"` — Credo

Redirect/hosted-checkout flow. Render a button/redirect to `paymentLink`.

| Key | Value | Purpose |
|---|---|---|
| `paymentLink` | Credo authorization URL | Where to send the payer to complete payment. |
| `providerReference` | Credo's own reference | Provider-side id (Credo reference). |
| `merchantReference` | our reference | Same value as the top-level `merchantReference`. |

### Sample 200 (Credo / PaymentLink)

```json
{
  "merchantReference": "TMG-8f2c1a9b4e",
  "paymentStatus": "Initiated",
  "paymentProviderId": "0192a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b",
  "paymentProviderName": "Credo",
  "expiresAtUtc": null,
  "paymentMethodType": "PaymentLink",
  "paymentInstruction": {
    "paymentLink": "https://pay.credo.io/checkout/9f8e7d6c5b4a",
    "providerReference": "CRD-9f8e7d6c5b4a",
    "merchantReference": "TMG-8f2c1a9b4e"
  }
}
```

---

## `paymentMethodType: "BankTransfer"` — SafeHaven

Virtual-account flow. Render an account-details card the payer transfers into. The account is
valid for **15 minutes** (`expiresAtUtc`).

| Key | Value | Purpose |
|---|---|---|
| `accountNumber` | virtual account number | The number to transfer to. |
| `bankName` | **bank code** ⚠️ | See caveat below — this is a **bank code**, not a display name. |
| `accountName` | virtual account name | Account-holder name to show. |
| `providerReference` | SafeHaven virtual-account id | Provider-side id; also the top-level `paymentProviderId` correlation key. |

> ⚠️ **`bankName` caveat.** The `bankName` key is currently populated with the SafeHaven
> **`BankCode`** value (e.g. a numeric/short code), not a human-readable bank name. If the card
> needs a display name, the frontend must map the code → name, or we can rename/relabel the key
> backend-side in a follow-up — flag if this is a problem.

### Sample 200 (SafeHaven / BankTransfer)

```json
{
  "merchantReference": "TMG-1a2b3c4d5e",
  "paymentStatus": "Initiated",
  "paymentProviderId": "0192a1b2-c3d4-7e5f-8a9b-1122334455aa",
  "paymentProviderName": "SafeHaven",
  "expiresAtUtc": "2026-07-16T12:15:00+00:00",
  "paymentMethodType": "BankTransfer",
  "paymentInstruction": {
    "accountNumber": "9901234567",
    "bankName": "090286",
    "accountName": "TMG-1a2b3c4d5e",
    "providerReference": "0192a1b2-c3d4-7e5f-8a9b-1122334455aa"
  }
}
```

---

## Notes for the frontend

- **Switch on `paymentMethodType`**, never on `paymentProviderName` (the latter is free-form config).
- `expiresAtUtc` is only meaningful for BankTransfer; show a 15-minute countdown there.
- Confirmation still arrives via provider webhook (see ask **P2b**); `initiate` only returns
  `paymentStatus: "Initiated"`. There is no payment-status GET — observe confirmation by polling
  the tenancy cycle (`GET /tenancies/allocations/mine`, `.../{id}/cycle`).
- Key names are defined server-side in `CredoKnownKeys.PaymentInstruction` and
  `SafeHavenKnownKeys.PaymentInstruction`. If they ever change, this doc should change with them.
