# 7. Staff sessions: the token in an HttpOnly cookie

**Status:** Accepted · **Last changed:** 2026-11-14

**Context:** Staff sign in to the staff app with an email and a password and should stay signed in for a working
day, across closed windows and new tabs. The session is a JWT the API signs; the question is where the browser keeps
it.

## Options
- **A.** An HttpOnly cookie set by the API at sign-in (`Secure`, `SameSite=Strict`, `Path=/api`), cleared by a
  sign-out endpoint, with the app and the API on one site. The app asks `GET /api/auth/me` who is signed in.
  Cross-site requests that change data are refused by an `Origin` check, on top of `SameSite`.
- **B.** The token in `localStorage`, sent in an `Authorization` header by the app.
- **C.** The token in `sessionStorage`.
- **D.** The token in memory only.
- **E.** A short access token plus a rotating refresh token.

## Decision
**A.** A script injected into a page can't read an HttpOnly cookie, so a cross-site scripting bug can't steal the
session; OWASP advises against keeping session tokens in web storage. The cookie lasts as long as its token (8 hours),
so a closed window or a new tab keeps the session. The API also reads the `Authorization` header, which the API
reference, tests and scripts use.

## Trade-offs
A cookie is sent by the browser on its own, so cross-site request forgery has to be closed: `SameSite=Strict` and the
`Origin` check do it. The app and the API must be served from one site (one domain, or the API serving the app's
files), which the deployment has to keep; in return the app needs no CORS, and CORS never allows credentials (only
the public quote form answers another site, without cookies). One token can't be revoked on its own before it expires; an account's
tokens all can, together. Every protected request reads the account in the same query: it must exist and be active,
and the token must have been issued at or after the account's `sessions_valid_from`, which a new password moves to
now. So deactivating an account, or setting a new password for it, ends its sessions at their next request; changing
your own password ends the others and gives this session a new cookie. The comparison is in whole seconds, the
precision of a token's issue time, so a session started in the same second as the change keeps going.

## Alternative that could also work
**E**, which shortens what a stolen token is worth and allows revoking sessions; it is a larger change, kept for
after v1. B is the most common choice in single-page apps, but any script on the page can read the token. C signs
people out when the tab closes and is no safer than B; D signs them out at every reload.
