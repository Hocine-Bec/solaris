# 8. Passwords hashed with PBKDF2-HMAC-SHA512

**Status:** Accepted · **Last changed:** 2026-11-13

**Context:** Staff accounts sign in with a password, so the API stores a hash of each one. The hash must be slow to
compute and salted, and its parameters must be able to grow over time without asking anyone to change their password.

## Options
- **A.** ASP.NET Core's password hasher (`PasswordHasher<T>`, part of the shared framework): PBKDF2 with HMAC-SHA512,
  a 128-bit salt and a 256-bit subkey, each hash carrying its own parameters. Set to 220,000 iterations, the OWASP
  Password Storage Cheat Sheet's figure for PBKDF2-HMAC-SHA512 (the framework's default is 100,000).
- **B.** bcrypt, through a NuGet package (BCrypt.Net-Next).
- **C.** Argon2id, through a third-party package.

## Decision
**A.** No new dependency: the hasher ships with ASP.NET Core and is maintained with it. Its hashes record their
iteration count, and checking a password made with fewer iterations reports that it should be rehashed, which sign-in
does then, while it has the password: raising the count later upgrades every account as it signs in. PBKDF2 is also
the algorithm to use where FIPS-140 compliance is required.

## Trade-offs
PBKDF2 uses little memory, so it resists guessing on GPUs less than Argon2id or scrypt do; the iteration count
compensates, and every sign-in pays for it in server time. Raising the count is a one-line change, but an
account that never signs in again keeps its older hash.

## Alternative that could also work
**C**, OWASP's first choice, memory-hard against GPU attacks; .NET 10 has no Argon2 of its own, so it means a
third-party package to vet and keep updated. B is what the earlier documents named, but the same cheat sheet now
keeps bcrypt for legacy systems, and it reads only the first 72 bytes of a password.
