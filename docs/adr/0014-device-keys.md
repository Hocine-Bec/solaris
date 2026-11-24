# 14. Device keys: a prefix and a hashed secret, one per Active installation

**Status:** Accepted · **Last changed:** 2026-11-22

**Context:** A switched-on system sends its readings to the API (FR-T1). BR-T2: only Active installations accept
readings, only through that installation's key; keys are stored hashed; a revoked key stops working immediately. A
device can't sign in as a person, and it must never be able to write for another installation, or reach anything
else in the API.

## Options
- **A.** One key per installation, `<prefix>.<secret>`, made by the API: the prefix finds the installation, the
  secret's SHA-256 is stored and compared in constant time. Its own authentication scheme, used only by the device
  endpoints.
- **B.** A staff-style account per device, signing in for a session token.
- **C.** One key shared by every device, the installation named in the request body.
- **D.** As A, with the secret hashed by a slow password hash (PBKDF2, as staff passwords are, ADR 8).

## Decision
**A.** The key is `<prefix>.<secret>`: 12 random characters of lowercase base32 that name it, and 32 random bytes in
base64url. The installation stores the prefix (unique index), the secret's SHA-256 and when it was made, never the
secret. SHA-256 is enough because the secret is random, not a password a person picks: no dictionary or guessing
helps against 256 random bits, and a slow hash (D) would only slow every upload.

The key is made at switch-on, in the same save as the move to Active, and is in that answer only
(`Cache-Control: no-store`); the page shows it once and never caches it. An Admin can replace it while the
installation is Active (with `If-Match`, ADR 13): the new one replaces the old in one save, so the old stops at once,
with no overlap. Deactivating deletes it in the same save. An installation switched on before keys existed has none
until an Admin makes one, through the same replacement.

The `DeviceKey` scheme reads `X-Device-Key`: one indexed read by prefix for an Active installation, the hash compared
with `FixedTimeEquals`, nothing cached, so a replacement or a deactivation counts from the next request. Its only claim
is the installation's id. The `Device` policy uses this scheme alone; the staff policies use the staff scheme alone, so
a key opens no staff endpoint and a staff session opens no device endpoint. Every refusal (none sent, not written as a
key, an unknown prefix, a wrong secret, an installation no longer Active) answers the same `401 device.invalid_key`.

## Trade-offs
A key lost before it was copied means a replacement and a visit to the device's settings. Only the office sees and
replaces keys: technicians get the key from the office. Replacing a key is in the API's log (who, when, never the key),
not in the installation's history. The prefix can be learned from the office's page, but on its own it lets nobody in.

## Alternative that could also work
**B** gives each device the same machinery as staff, with expiry and refresh, at the cost of sessions and refresh logic
on small embedded devices that can't keep them reliably. C can't limit a device to its own installation, or revoke one
without all.
