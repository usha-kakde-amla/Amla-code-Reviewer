# Security Code Quality Rules

A reference guide for application security rules and best practices.

---

## SEC001 — Avoid Hardcoded Credentials
**Severity:** Error

**Description:** Hardcoding usernames, passwords, API keys, or connection strings in code is a major security risk.

**Message:** Do not hardcode sensitive credentials in code.

**Detection:**
- Hardcoded password
- API key in string literal
- Connection string with credentials

**Fix:** Use environment variables or secure configuration (e.g., Azure Key Vault).

---

## SEC002 — SQL Injection Risk
**Severity:** Error

**Description:** Building SQL queries using string concatenation exposes the application to SQL injection attacks.

**Message:** Use parameterized queries to prevent SQL injection.

**Detection:**
- SQL query built using string concatenation

**Fix:** Use `SqlCommand` parameters or ORM (e.g., Entity Framework).

---

## SEC003 — Command Injection Risk
**Severity:** Error

**Description:** Passing user input directly into system commands can lead to command injection.

**Message:** Validate and sanitize input before executing system commands.

**Detection:**
- `Process.Start` with user input

**Fix:** Avoid direct command execution or use safe APIs.

---

## SEC004 — Cross-Site Scripting (XSS)
**Severity:** Error

**Description:** Unencoded user input rendered in UI can lead to XSS attacks.

**Message:** Encode user input before rendering.

**Detection:**
- Rendering raw user input in HTML

**Fix:** Use HTML encoding (e.g., `HttpUtility.HtmlEncode`).

---

## SEC005 — Cross-Site Request Forgery (CSRF) Protection Missing
**Severity:** Error

**Description:** Missing CSRF protection allows attackers to perform unauthorized actions.

**Message:** Enable CSRF protection.

**Detection:**
- POST actions without anti-forgery token

**Fix:** Use `AntiForgeryToken` in ASP.NET.

---

## SEC006 — Insecure Deserialization
**Severity:** Error

**Description:** Deserializing untrusted data can lead to remote code execution.

**Message:** Avoid deserializing untrusted data.

**Detection:**
- `BinaryFormatter.Deserialize`
- Untrusted JSON/XML deserialization

**Fix:** Use safe serializers or validate input.

---

## SEC007 — Weak Cryptographic Algorithm
**Severity:** Error

**Description:** Using weak hashing algorithms like MD5 or SHA1 is insecure.

**Message:** Use strong cryptographic algorithms.

**Detection:**
- `MD5`
- `SHA1`

**Fix:** Use `SHA256` or higher.

---

## SEC008 — Sensitive Data Logging
**Severity:** Error

**Description:** Logging sensitive data like passwords or tokens exposes them.

**Message:** Do not log sensitive data.

**Detection:**
- Logging password/token

**Fix:** Mask or remove sensitive fields.

---

## SEC009 — Open Redirect Vulnerability
**Severity:** Warning

**Description:** Redirecting users using unvalidated input can lead to phishing attacks.

**Message:** Validate redirect URLs.

**Detection:**
- Redirect with user-controlled URL

**Fix:** Allow only trusted domains.

---

## SEC010 — Path Traversal Vulnerability
**Severity:** Error

**Description:** Using user input in file paths can allow access to unauthorized files.

**Message:** Validate and sanitize file paths.

**Detection:**
- File path built from user input

**Fix:** Use safe path APIs and restrict directories.

---

## SEC011 — Missing HTTPS Enforcement
**Severity:** Error

**Description:** Serving sensitive data over HTTP is insecure.

**Message:** Enforce HTTPS.

**Detection:**
- No HTTPS redirection

**Fix:** Use HTTPS redirection middleware.

---

## SEC012 — Insecure Random Number Generation
**Severity:** Error

**Description:** Using `Random` for security-sensitive operations is insecure.

**Message:** Use cryptographic random generator.

**Detection:**
- `System.Random` used for tokens/passwords

**Fix:** Use `RNGCryptoServiceProvider` or `RandomNumberGenerator`.

---

## SEC013 — Hardcoded Encryption Keys
**Severity:** Error

**Description:** Hardcoding encryption keys makes encryption ineffective.

**Message:** Do not hardcode encryption keys.

**Detection:**
- Hardcoded encryption key

**Fix:** Store keys securely (e.g., Key Vault).

---

## SEC014 — Improper Exception Handling
**Severity:** Warning

**Description:** Exposing stack traces reveals sensitive system information.

**Message:** Do not expose internal exception details.

**Detection:**
- Exception details returned to user

**Fix:** Log internally and show a generic message to users.

---

## SEC015 — Missing Input Validation
**Severity:** Error

**Description:** Unvalidated input can lead to multiple injection vulnerabilities.

**Message:** Validate all user inputs.

**Detection:**
- User input used without validation

**Fix:** Use validation frameworks or manual validation.

---

## SEC016 — Insecure Cookie Configuration
**Severity:** Warning

**Description:** Cookies without `Secure`/`HttpOnly` flags are vulnerable.

**Message:** Set `Secure` and `HttpOnly` flags on cookies.

**Detection:**
- Cookie without `Secure` or `HttpOnly`

**Fix:** Configure cookie options properly.

---

## SEC017 — Exposed Sensitive Headers
**Severity:** Warning

**Description:** Missing security headers increases attack surface.

**Message:** Add security headers.

**Detection:**
- Missing headers like `X-Content-Type-Options`

**Fix:** Configure headers in middleware.

---

## SEC018 — Weak Password Policy
**Severity:** Warning

**Description:** Weak password requirements increase risk of compromise.

**Message:** Enforce strong password policies.

**Detection:**
- Short password length
- No complexity rules

**Fix:** Require length, complexity, and expiration.

---

## SEC019 — Unrestricted File Upload
**Severity:** Error

**Description:** Uploading files without validation can allow malicious files.

**Message:** Validate uploaded files.

**Detection:**
- File upload without type/size validation

**Fix:** Restrict file types and size.

---

## SEC020 — Sensitive Data in URL
**Severity:** Error

**Description:** Passing sensitive data in URL exposes it in logs and browser history.

**Message:** Do not send sensitive data in URL.

**Detection:**
- Password/token in query string

**Fix:** Use POST body or headers.

---

## SEC021 — Improper Authentication Handling
**Severity:** Error

**Description:** Weak authentication mechanisms increase risk of unauthorized access.

**Message:** Use secure authentication frameworks.

**Detection:**
- Custom auth logic without validation

**Fix:** Use built-in authentication libraries.

---

## SEC022 — Missing Authorization Checks
**Severity:** Error

**Description:** Access control not enforced properly.

**Message:** Enforce proper authorization.

**Detection:**
- Sensitive operation without role check

**Fix:** Use role-based or policy-based authorization.

---

## SEC023 — Use of Obsolete Security APIs
**Severity:** Warning

**Description:** Using deprecated APIs can introduce vulnerabilities.

**Message:** Avoid deprecated security APIs.

**Detection:**
- Obsolete security API usage

**Fix:** Use modern alternatives.

---

## SEC024 — Directory Listing Enabled
**Severity:** Warning

**Description:** Exposing directory listing reveals sensitive files.

**Message:** Disable directory listing.

**Detection:**
- Directory browsing enabled

**Fix:** Configure server to disable listing.

---

## SEC025 — Improper CORS Configuration
**Severity:** Error

**Description:** Allowing all origins can expose APIs to attacks.

**Message:** Restrict allowed origins.

**Detection:**
- `AllowAnyOrigin` in CORS

**Fix:** Specify trusted domains.

---

## SEC026 — Sensitive Data in Memory Without Protection
**Severity:** Warning

**Description:** Sensitive data stored in memory without encryption is risky.

**Message:** Protect sensitive data in memory.

**Detection:**
- Plain text sensitive data in variables

**Fix:** Use secure strings or encryption.

---

## SEC027 — Missing Rate Limiting
**Severity:** Warning

**Description:** No rate limiting allows brute force or DoS attacks.

**Message:** Implement rate limiting.

**Detection:**
- No rate limiting on endpoints

**Fix:** Use middleware for throttling.

---

## SEC028 — Unvalidated Redirect URL
**Severity:** Warning

**Description:** Redirect URLs should be validated to prevent abuse.

**Message:** Validate redirect URLs.

**Detection:**
- Redirect using query parameter

**Fix:** Whitelist trusted URLs.

---

## SEC029 — Exposure of Internal IP/Host Information
**Severity:** Warning

**Description:** Leaking internal infrastructure details can aid attackers.

**Message:** Do not expose internal infrastructure details.

**Detection:**
- Internal IP in response/log

**Fix:** Mask or remove sensitive info.

---

## SEC030 — Missing Secure Headers (HSTS)
**Severity:** Warning

**Description:** HSTS prevents downgrade attacks.

**Message:** Enable HSTS header.

**Detection:**
- Missing `Strict-Transport-Security` header

**Fix:** Add HSTS configuration.
