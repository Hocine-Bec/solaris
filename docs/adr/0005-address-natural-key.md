# 5. Addresses deduplicated by a normalized natural key

**Status:** Accepted, recorded retroactively; trimming and space collapsing were added on 2026-10-31 ·
**Last changed:** 2026-10-31

**Context:** The same property arrives many times: repeat quote requests, and later the lead's address reused
for its installation. It must be stored once, and "the same address" has to be defined in one place.

## Options
- **A.** A natural key: a SHA-256 hash of the normalized street, city, state, ZIP and country, stored as
  `UniqueAddressHash` with a unique index.
- **B.** A unique index over the five raw columns.
- **C.** No deduplication: one address row per lead and per installation.
- **D.** A canonical address from an external geocoding service (the form already uses an address
  autocomplete), keyed by its place id.

## Decision
**A.** The database enforces uniqueness, so concurrent requests can't create two rows for one address.
"Same address" is defined once, in `Address.BuildHash`: each field is trimmed, repeated spaces are collapsed,
and the result is upper-cased before hashing. One fixed-size indexed column replaces an index over five long
text columns.

## Trade-offs
The normalization is still basic: "St." and "Street" give different hashes. Changing the normalization means
recomputing every stored hash.

## Alternative that could also work
**D.** It handles abbreviations and typos, but puts an external service in the write path; it could later feed
A by hashing the canonical form. B has the same normalization problem with a heavier index; C duplicates data
and loses "this property already has a lead".
