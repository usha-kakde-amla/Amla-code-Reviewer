# JavaScript Code Quality Rules

A reference guide for JavaScript security and performance rules.

---

## Security Rules

### JS_SEC001 — Avoid Hardcoded Credentials
**Severity:** Error

**Description:** Hardcoding usernames, passwords, API keys, or tokens exposes sensitive data.

**Message:** Sensitive credentials must not be hardcoded.

**Detection:**
- API key in string literal
- Hardcoded token
- Secret inside source code

**Fix:** Use environment variables or secret managers.

---

### JS_SEC002 — Avoid eval() Usage
**Severity:** Error

**Description:** `eval()` executes arbitrary code and allows code injection attacks.

**Message:** Avoid dynamic code execution.

**Detection:**
- `eval(`
- `new Function(`

**Fix:** Use safe parsing or predefined logic.

---

### JS_SEC003 — Prevent XSS via innerHTML
**Severity:** Error

**Description:** Using `innerHTML` with unsanitized input can cause Cross-Site Scripting attacks.

**Message:** Unsanitized HTML assignment detected.

**Detection:**
- `innerHTML =`
- `dangerouslySetInnerHTML`

**Fix:** Use `textContent` or sanitize input.

---

### JS_SEC004 — Avoid SQL Injection
**Severity:** Error

**Description:** Dynamic SQL queries built using user input are vulnerable.

**Message:** Potential SQL Injection vulnerability.

**Detection:**
- `query + userInput`
- Raw SQL concatenation

**Fix:** Use parameterized queries or ORM prepared statements.

---

### JS_SEC005 — Avoid Command Injection
**Severity:** Error

**Description:** Passing user input into system commands may execute malicious instructions.

**Message:** Possible command injection detected.

**Detection:**
- `child_process.exec`
- `exec(`
- `spawn(`

**Fix:** Validate inputs or use argument arrays.

---

## Performance Rules

### JS_PERF001 — Avoid Blocking Loops
**Severity:** Warning

**Description:** Large synchronous loops block the event loop.

**Message:** Blocking operation detected.

**Detection:**
- `while(true)`
- `for(;;)`

**Fix:** Use async processing or batching.

---

### JS_PERF002 — Unawaited Promise
**Severity:** Warning

**Description:** Promises not awaited may cause race conditions.

**Message:** Promise execution not awaited.

**Detection:**
- Async function call without `await`

**Fix:** Use `await` or handle promise properly.

---

### JS_PERF003 — Memory Leak via Event Listeners
**Severity:** Warning

**Description:** Event listeners not removed cause memory leaks.

**Message:** Possible memory leak detected.

**Detection:**
- `addEventListener` without `removeEventListener`

**Fix:** Remove listeners during cleanup.

---

### JS_PERF004 — Console Logging in Production
**Severity:** Info

**Description:** Console logs reduce performance and expose internal data.

**Message:** Console logging detected.

**Detection:**
- `console.log(`
- `console.debug(`

**Fix:** Remove logs or use logging framework.

---

### JS_PERF005 — Repeated API Calls
**Severity:** Warning

**Description:** Multiple identical API requests waste resources.

**Message:** Repeated network calls detected.

**Detection:**
- `fetch` inside loop
- `axios` call inside loop

**Fix:** Cache responses or debounce requests.
