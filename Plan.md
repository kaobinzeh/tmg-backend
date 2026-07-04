# TMG MVP Build Plan — Property Management Wedge

> Scope: the lean MVP "wedge" from the feasibility analysis — depth on the recurring, legally-weighted
> property-management workflow, **not** the full feature suite. Built on top of the existing
> auth / payments / notifications / stakeholders foundation.

## Product context

TMG is a multi-tenancy property-management SaaS for property managers in Nigeria (Lagos/Abuja).
`Client` = the manager organization (the SaaS customer); `Tenant` = the actual property renter.
Monetization: monthly subscription + optional take-rate on in-app rent.

MVP features (this plan): **(1)** Property & Unit registry, **(2)** Tenant onboarding by email,
**(3)** Rent cycle + auto-notifications, **(4)** Payment recording + auto-receipt, **(5)** Per-unit document vault.

Deferred to v2+: transfer-of-tenancy, complaints/maintenance, full reports & charts (MVP ships 3 numbers:
collected / outstanding / upcoming renewals), tenant-initiated termination, multi-branch staff roles.

## Confirmed decisions

1. **Rent reminders** — recurring scan job (`RentReminderProcessor` BackgroundService) that scans tenancies
   and enqueues due reminders idempotently. Robust to renewals/terminations.
2. **Payments** — lead with **manager-recorded offline payment** (matches annual-upfront/bank-transfer reality);
   in-app collection (Credo/SafeHaven via existing `InitiatePayment`) is optional.
3. **Tenant identity / onboarding** — **manager-provisioned accounts**. When a manager allocates a unit to a
   tenant email, the system **auto-creates the tenant account in a pending state** and emails an invitation.
   The tenant accepts the allocation, reads & accepts T&C, **sets their password**, and uploads documents —
   no self-registration step. This minimizes tenant friction.

## Architecture decisions (apply across all phases)

| Decision | Choice | Why |
|---|---|---|
| New schemas | `properties` (Property, Unit), `tenancies` (Tenancy, TenancyDocument) | Schema-per-domain convention in `SchemaNames.cs` |
| Aggregates | `Property`, `Unit`, `Tenancy` each an `IAggregateRoot`; Unit references `PropertyId` (not nested) | Units are queried/mutated independently |
| Multi-tenancy scoping | Every new entity carries `ClientId`; handlers scope by `ActorContext.ClientId` | `Client` = manager org |
| Identity | Manager & tenant are both `Stakeholder`s under a `Client`; stakeholder-type keys `manager`, `tenant` | Reuses auth/stakeholder model |
| Rent reminders | Recurring `RentReminderProcessor` BackgroundService in TMG.Jobs | Mirrors `PaymentReconciliationProcessor` |
| Rent payment | New `PaymentIntent.RentPayment`; manager-recorded offline first, in-app optional | Annual-upfront reality |
| Documents | Existing object storage (`AddObjectStorage`); entity holds storage key + signed-URL access | Vault = lock-in |

**Every slice follows the established pattern:** sealed-record `Command`/`Result`, sealed `Handler` with
`HandleAsync(command, ct)` injected directly (no mediator), `IRepository<T>` + `Specification` for data,
`IUnitOfWork.SaveChangesAsync`, domain events via `IEventPublisher`, FluentValidation + request DTO in the
WebAPI feature folder, routes in `EndpointUrl.cs`, handlers registered in `Application/ServiceCollectionExtensions.cs`.

---

## Phase 0 — Foundations (prerequisite)

- Add `Properties` and `Tenancies` constants to `src/TMG.Infrastructure/Persistence/SchemaNames.cs`.
- Add stakeholder-type keys `manager` and `tenant` to `StakeholderDefaults` + seed in
  `src/TMG.DatabaseMigrator/Scripts/PostDeploy/0001.template-seeder.postdeploy.sql`.
- Add enum values up-front to avoid churn:
  - `PaymentIntent.RentPayment`
  - `NotificationType.{UnitAllocationInvitation, TenancyAgreementReady, RentDueReminder3Months, RentDueReminder1Month, RentPaymentReceipt}`

---

## Phase 1 — Feature 1: Property & Unit Registry ⭐ (start here)

**Domain** (`src/TMG.Domain/Properties/`)
- `Entities/Property.cs` — `Create(clientId, ownerStakeholderId, name, address, description)`; encapsulated, soft-delete via `Entity` base.
- `Entities/Unit.cs` — `Create(propertyId, clientId, label, description, numberOfRooms, rentAmount, currencyId)`; `UpdateRent(amount)`, `MarkAvailable()`, `MarkOccupied()`, `MarkUnavailable()`; `UnitStatus` enum.
- `Specifications/` — `PropertyByIdSpecification`, `PropertiesByClientSpecification`, `UnitByIdSpecification`, `UnitsByPropertySpecification`.

**Persistence** (`src/TMG.Infrastructure/Persistence/`)
- `Configurations/PropertyConfiguration.cs`, `UnitConfiguration.cs` (`ToTable(..., SchemaNames.Properties)`, FK `Unit→Property` `OnDelete(Cascade)`, soft-delete filtered indexes).
- Add `DbSet<Property>`, `DbSet<Unit>` to `AppDbContextBase`.
- Migration: `dotnet ef migrations add AddPropertyAndUnitRegistry --project src/TMG.Infrastructure --startup-project src/TMG.WebAPI`.

**Application slices** (`src/TMG.Application/Properties/Features/`): `CreateProperty`, `ListProperties`, `GetProperty`, `AddUnit`, `ListUnits`, `UpdateUnitRent` (the "review rent" job), `SetUnitAvailability`.

**WebAPI** (`src/TMG.WebAPI/Features/Properties/`): `PropertiesController` + `UnitsController`, request DTOs + validators; add `EndpointUrl.Properties`/`Units`; `[Authorize]` + manager-role check.

**Tests:** domain unit tests, handler unit tests, one integration happy-path per endpoint.

---

## Phase 2 — Feature 2: Tenant Onboarding by Email (manager-provisioned)

- **Domain** `src/TMG.Domain/Tenancies/`: `Tenancy` aggregate — `Invite(unitId, clientId, invitedEmail, …)` → `Invited`; `Accept(...)`, `Reject()`. `TenancyStatus` enum.
- **Onboarding flow (manager-provisioned):**
  1. Manager `AllocateUnit` → creates `Tenancy{Invited}`, reserves Unit, **auto-creates a pending tenant `AppUser` + `Stakeholder`** (type `tenant`) under the manager's `Client`.
  2. Publishes `UnitAllocated` → Consumer sends `UnitAllocationInvitation` email with an accept/activation link + token.
  3. Tenant opens link → accepts allocation → **reads & accepts T&C** → **sets password** (activates the pending account) → uploads ID + passport photo to object storage.
  4. Because the tenant reached this flow via the link emailed to their address, **accepting also marks `EmailConfirmed = true`** on the AppUser (and the stakeholder verified) — no separate OTP confirmation step needed.
- **Auth integration:** add an admin-initiated "create pending account" path + a token-based "accept invitation & set password & confirm email" flow (model on the existing password-reset/OTP mechanics).
- **New:** `UnitAllocationInvitation` email template + Consumer handler `UnitAllocatedHandler`.
- **WebAPI:** `TenanciesController` (allocate / accept-invitation / reject / upload-document).

---

## Phase 3 — Feature 3: Rent Cycle + Auto-Notifications (the differentiator)

- **Domain:** cycle fields/methods on `Tenancy` — `Activate(startUtc, termMonths)` sets `CycleStartUtc/CycleEndUtc/NextRentDueUtc`; `RecordReminderSent(kind)` for idempotency.
- **Scheduling:** `RentReminderProcessor : BackgroundService` in `src/TMG.Jobs/RentReminders/` (model on `PaymentReconciliationProcessor`) scans active tenancies whose `NextRentDueUtc` is in the 3-month / 1-month window and not yet reminded, then sends `RentDueReminder3Months` / `RentDueReminder1Month`.
- **Legally-correct notice letter:** render notice HTML, email it, store a copy in the document vault.
- **New:** `RentDueReminder3Months` / `RentDueReminder1Month` templates.
- **WebAPI:** view a tenancy's cycle + "upcoming renewals" list.

---

## Phase 4 — Feature 4: Payment Recording + Auto-Receipt ✅ (offline path shipped)

- **Primary (offline):** ✅ `RecordRentPayment` slice — manager records a payment against a tenancy cycle → `Tenancy.RecordRentPayment` settles the term (see below) → a `RentPayment` ledger entry is created → an HTML receipt is rendered (`IRentReceiptRenderer`) and archived in the vault as a `TenancyDocument{Receipt}` (linked back via `RentPayment.ReceiptDocumentId`) → publishes `RentPaymentReceived` → Consumer (`RentPaymentReceivedHandler`) sends the `RentPaymentReceipt` email.
  - **Initial vs renewal:** the **first** recorded payment settles the current (initial) term the active cycle frames — the renewal date and armed reminders are unchanged. Every **subsequent** payment is a renewal that rolls the cycle forward one term and re-arms reminders. (`LastRentPaidAtUtc == null` distinguishes the first payment; no extra column.) `Activate` is unchanged.
  - Endpoint: `POST api/v1/tenancies/allocations/{tenancyId}/payments`.
  - Migration `AddRentPayments` (new `tenancies.RentPayments` table + `Tenancy.TermMonths`/`LastRentPaidAtUtc`).
- **In-app (tenant-initiated):** ✅ the tenant calls `POST /payments/initiate` with `PaymentIntent.RentPayment` + `TenancyId`; the amount is server-priced from the unit rent (client amount ignored) and the tenancy must be active and belong to the tenant. `PaymentTransaction` carries the `TenancyId`; on gateway confirmation the reconciliation raises `SuccessfulPaymentConfirmed` (now with `TenancyId`) → `SuccessfulPaymentConfirmedHandler` routes the rent intent to a `RecordRentPaymentCommand` → the `RecordRentPaymentHandler` consumer idempotently (per `PaymentTransactionId`) settles the cycle, writes the `RentPayment` ledger row (`Method = Online`, `RecordedByStakeholderId = null`), archives the receipt via the shared `IRentReceiptArchiver`, and raises `RentPaymentReceived` (same receipt email). Provider configs seeded for intent 3 (Credo + SafeHaven, NGN).
- **Shared receipt path:** `IRentReceiptArchiver` renders + stores the receipt document for both the offline and in-app handlers, so both produce an identical receipt.
- **New:** ✅ receipt generation + `RentPaymentReceipt` template (NotificationType 14, seeder row added).

---

## Phase 5 — Feature 5: Per-Unit Document Vault (the lock-in) ✅

- **Domain:** ✅ `TenancyDocument` (`Agreement`, `NationalId`, `PassportPhoto`, `Receipt`, `Notice`) already existed; added `TenancyDocumentsByTenancySpecification` + `TenancyDocumentByIdForClientSpecification`.
- **Storage:** ✅ added `IObjectStorageService.GetSignedDownloadUrlAsync(storageKey, expiresAtUtc)` — R2 recovers the object key from the stored private URL and returns an S3 presigned GET URL (~15-min validity); Noop returns a stub URL.
- **Agreement:** ✅ on accept, `AcceptTenancyInvitationHandler` archives the agreement via `ITenancyAgreementArchiver` (renderer + `TenancyDocumentType.Agreement`) and publishes `TenancyAgreementReady` → consumer emails the tenant. New `NotificationType.TenancyAgreementReady = 15` + template + seeder row.
- **Application:** ✅ `ListTenancyDocuments` + `GetTenancyDocumentDownloadUrl` slices, gated by `TenancyDocumentAccessGuard` (manager of the client sees all its tenancies' documents; a tenant sees only their own — closes the cross-tenant privacy gap). `UploadDocument` shipped in Phase 2.
- **WebAPI:** ✅ `DocumentsController` — `GET …/allocations/{tenancyId}/documents` (list) and `GET …/documents/{documentId}/download-url` (signed URL).
- **Receipt/agreement archival** share the same object-storage + `TenancyDocument` write path (`IRentReceiptArchiver` / `ITenancyAgreementArchiver`). No EF migration needed (no new columns).

---

## Cross-cutting (every phase)

- **Authorization:** manager-only vs tenant-only endpoints via stakeholder type/role; always scope queries by `ActorContext.ClientId`.
- **Testing (AGENTS.md):** NSubstitute + Shouldly, one case per file, `When_{Action}_With{Params}_Should`, integration = happy-path only with `IAsyncLifetime`. Run `dotnet build` then `dotnet test` **sequentially**.
- **Conventions:** `TimeProvider` for cycle math, `Guid.CreateVersion7()`, explicit `CancellationToken`, sealed records.
