# Functional requirements

What Solaris must do (`FR-…`) and the business rules behind it (`BR-…`). This file describes behaviour, not
endpoints: routes and field names are defined by the API itself. Terms are defined in [domain](domain.md).

---

## Requirements

### Leads
- **FR-L1** — The public quote form (three steps: contact, property, qualification) creates a lead. The property
  is in the United States: its state is one of the 50 states or the District of Columbia, as a two-letter code.
- **FR-L2** — A duplicate submission doesn't create a second lead or send the emails again (BR-L1).
- **FR-L3** — Admin can list leads (paginated, filtered by status) and view one.
- **FR-L4** — Admin can change a lead's status within the allowed transitions (BR-L2).
- **FR-L5** — Admin can convert a lead (BR-L3).
- **FR-L6** — A new lead triggers two notifications: a welcome email to the lead and a new-lead alert to sales.

### Customers
- **FR-C1** — Admin can list customers (paginated, searched by name or email) and view one with its installations.
- **FR-C2** — A customer's status follows its installations automatically (BR-C1); nobody sets it by hand.

### Installations
- **FR-I1** — Admin can list installations (paginated, filtered by status) and view one with its full status
  history and assignments.
- **FR-I2** — Admin can move an installation to the next stage, only through allowed transitions and only when
  the stage's conditions are met (BR-I1).
- **FR-I3** — Admin can cancel an installation before it is Active, with a reason, and deactivate an Active one
  (BR-I2).
- **FR-I4** — Admin can record the design: system size (kW), panel count, inverter type, and confirm the
  installation's time zone (BR-P4).
- **FR-I5** — Admin assigns technicians to an installation, each with a role: Surveyor, Installer or Inspector.
- **FR-I6** — A technician sees the installations they are assigned to and can mark their own assignment complete.
- **FR-I7** — Activation issues a device API key (shown once) and notifies the customer by email.

### Telemetry and production
- **FR-T1** — A device uploads a batch of up to 500 readings for its installation.
- **FR-T2** — Uploading a reading twice, or retrying a whole batch, never creates duplicates (BR-T1). The
  response says how many readings were accepted, how many were duplicates, and which were rejected and why.
- **FR-T3** — Late readings, up to 30 days old, are accepted and correct the affected days (BR-P3).
- **FR-P1** — Each installation has one production row per day: actual kWh, expected kWh, health (BR-P1, BR-P2).
- **FR-P2** — **Fleet dashboard:** for the last day that has ended in every time zone the fleet covers, the number of installations per health status,
  plus the 20 worst performers.
- **FR-P3** — **Month view:** daily actual vs expected production for one installation and one month.
- **FR-P4** — **Year view:** monthly totals, actual vs expected, for one installation and one year.

### Notifications
- **FR-N1** — Every notification (FR-L6, FR-I7) is delivered at least once, even if the mail server is down for
  hours or the API crashes (BR-N1, BR-N2).
- **FR-N2** — Admin can list the notifications that failed permanently and retry them.

### Users and security
- **FR-U1** — Staff log in with an email and a password and receive a token.
- **FR-U2** — Admin creates technician accounts and can deactivate users.
- **FR-U3** — The first Admin account is created from configuration at startup; there are no default passwords
  in the code.

---

## Business rules

### Leads
- **BR-L1 Duplicate leads.** A lead is *open* while its status is New or Contacted. Only one open lead may exist
  per email address (case-insensitive). A submission for an email that already has an open lead returns the
  existing lead (HTTP 200, same id): no new lead, no new emails. This holds even when the same form is submitted
  many times at the same moment. A new lead *is* allowed once the previous one is Converted, Disqualified or Lost.
- **BR-L2 Lead transitions.** New → Contacted; New or Contacted → Disqualified or Lost; Contacted → Converted
  (only through conversion). Converted, Disqualified and Lost are final. Each transition records its timestamp.
- **BR-L3 Conversion.** Converting a Contacted lead creates, in **one transaction**, a customer (status Prospect)
  and an installation (status Survey) at the lead's address, and links the lead to the customer. A lead can be
  converted once; a second attempt is rejected, even if both arrive at the same moment.

### Installations
- **BR-I1 Stage transitions and their conditions.**

  | From → To | Condition |
  |---|---|
  | Survey → Design | A Surveyor assignment exists and is completed |
  | Design → Permits | System size > 0 kW and panel count > 0 are recorded |
  | Permits → Installation | At least one Installer is assigned |
  | Installation → Inspection | All Installer assignments are completed |
  | Inspection → Active | An Inspector assignment exists and is completed |

  No skipping stages, no going backwards.
- **BR-I2 Cancel and deactivate.** Cancelled is allowed from any stage before Active and needs a reason.
  Deactivated is allowed only from Active and revokes the device API key. Both are final.
- **BR-I3 History.** Every status change is recorded: from, to, who, when, and an optional note. History is
  never edited.
- **BR-I4 Concurrent edits.** If two people change the same installation at the same time (two stage changes, or
  a stage change and a cancellation), exactly one succeeds. The other gets a conflict (HTTP 409) and must reload.
  An update is never silently overwritten.

### Customers
- **BR-C1 Customer status.** *Prospect*: created by conversion, no installation Active yet. *Active*: at least one
  installation is Active. *Inactive*: no installation is in progress or Active (all are Cancelled or
  Deactivated). The status is recalculated whenever one of the customer's installations changes status.

### Telemetry
- **BR-T1 One reading per slot.** A reading is the energy produced in one 15-minute slot; slots start at :00,
  :15, :30 and :45 (UTC). There is one reading per installation per slot: the first one wins, and later copies
  count as duplicates. A reading is rejected if:
  - its timestamp isn't aligned to a slot
  - it is in the future
  - it is older than 30 days
  - its value is negative
  - its value is above the physical maximum: system size (kW) × 0.25 h × 1.1
- **BR-T2 Who may upload.** Only Active installations accept readings, and only through that installation's API
  key. Keys are stored hashed. A revoked key stops working immediately.

### Production and health
- **BR-P1 Expected production.** Expected daily kWh = system size (kW) × peak sun hours for the month in the
  installation's state × 0.80.

  The peak sun hours come from a table of monthly averages per state, taken from NREL's published solar data
  for one representative city per state; the table and its exact source are added with the code that uses it.
  Weather isn't modelled.
- **BR-P2 Health.** Ratio = actual ÷ expected for the day.

  | Health | Rule |
  |---|---|
  | Excellent | ratio ≥ 0.90 |
  | Good | 0.80 ≤ ratio < 0.90 |
  | Warning | 0.60 ≤ ratio < 0.80 |
  | Critical | ratio < 0.60 and some readings exist |
  | Offline | no readings at all for that day |
- **BR-P3 Late data.** When readings arrive for a past day, that day's rollup is recalculated. Recalculating the
  same day twice gives the same result.
- **BR-P4 Local days.** Every installation has a time zone, proposed from its state when it is created and
  confirmed by Admin until the installation is Active; after that it can't change. Its readings are counted per
  local day in that zone, so a day has 92, 96 or 100 fifteen-minute slots (daylight-saving changes; zones without
  daylight saving always have 96). A day is checked for Offline once it has ended in that zone.

### Notifications
- **BR-N1 Never lost.** A notification is recorded in the **same database transaction** as the event that causes
  it (a lead created, an installation activated). If the event is saved, its notification exists; if the event
  fails, no notification exists.
- **BR-N2 Retries.** Failed sends are retried with growing delays for up to 24 hours. After that, the
  notification is marked Failed and shown to Admin (FR-N2). Delivery is *at least once*: a rare duplicate email
  after a crash is acceptable; a lost one is not.

### Security
- **BR-S1 Public form.** The public lead endpoint accepts at most 5 submissions per minute per IP address; beyond
  that it answers HTTP 429.
- **BR-S2 Everything else requires authentication.** Staff endpoints use roles, device endpoints use API keys,
  the background-jobs dashboard isn't exposed outside Development, and CORS allows only configured origins.

---

## Out of scope for v1

| Not in v1 | Why |
|---|---|
| Proposals, pricing, contracts, financing | A large domain of its own, and not what v1 is about |
| Payments, billing, invoices | Same |
| Documents and file upload (permits, photos) | Planned after v1: the permit, the inspection certificate and warranties kept as proof for each stage. v1 records each stage's status only |
| Customer portal, customer logins | A frontend product rather than backend depth |
| Weather data and weather APIs | Expected production uses fixed monthly sun hours (BR-P1) |
| Equipment inventory | Planned after v1 |
| Support tickets | Planned after v1: automatic tickets for unhealthy systems |
| Multiple companies (multi-tenant), mobile apps, SMS | Not needed |
