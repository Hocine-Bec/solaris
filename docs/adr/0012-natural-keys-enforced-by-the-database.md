# 12. Duplicates refused by the database, through natural keys

**Status:** Accepted · **Last changed:** 2026-11-27

**Context:** BR-L1 allows one open lead (New or Contacted) per email. The first version checked it in the service:
look for an open lead with the email, insert if there is none. P3a measured that check (`measurements.md` §8): 50
copies of one form sent at the same moment pass it together, and in every run every round made more than one lead,
up to 50 in one round, each with its own two emails. The quote form sends nothing that identifies a submission, and
the devices' readings don't either: P4a counted 502 duplicate rows in 192,000 readings sent by devices retrying at
the same moment, through the same kind of check (`measurements.md` §9).

## Options
- **A.** The natural key, enforced by the database: a partial unique index on the lead's normalized email
  (`Emails.Normalize`: trimmed, upper-cased) over the open statuses only. A save that breaks it rolls back; the
  service then answers the open lead's id, with 200.
- **B.** An idempotency key: the client sends one per submission, and a table remembers what each key produced.
- **C.** Keep the check in the service, made safe with a lock (an advisory lock per email, or `SERIALIZABLE`).
- **D.** Keep the check in the service, unlocked (the version P3a measured).

## Decision
**A.** The business already says what a duplicate is, so the database can refuse one with no race and nothing from
the client. The lead, its address and its two emails are saved in one transaction: when the index refuses the lead,
nothing of it is kept, so a duplicate sends no email either (BR-N1). Correcting an open lead's email meets the same
index, and is refused with `lead.email_taken`. Closed leads are outside the index's filter, so a person who comes
back after a closed lead gets a new one.

The readings use the same decision with no filter: the installation and the slot are `energy_readings`' primary key,
and a batch is written with `ON CONFLICT DO NOTHING` (ADR 16), so a slot already stored is counted as a duplicate
and the first reading of a slot wins (BR-T1). P4a after: 0 duplicates.

## Trade-offs
The refused save costs a rolled-back transaction and one more query, to read the open lead's id; that only happens
on a duplicate. The rule lives partly in the index's filter (`status IN ('New', 'Contacted')`): a new open status
must be added there too, by a migration. The migration that made the index stops, naming the leads, if open leads
already share an email: someone must close all but one first. B is the general answer for requests without a
natural key, but needs every client to cooperate; C is correct but makes every submission wait on a lock for a rule
the database can hold; D is the race.

## Alternative that could also work
**B**, if a duplicate ever had no business definition, or a client already sent a key.
