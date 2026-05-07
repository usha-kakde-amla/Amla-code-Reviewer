# Code Quality Rules

---

## NO_CONSOLE_LOG — Avoid console.log
**Severity:** Warning

**Description:** Remove console.log from production code.

**Message:** console.log should not be committed to production code.

**Detection:**
- console.log(
- console.debug(

**Fix:** Remove all console.log statements or replace with a proper logging framework before deploying to production.
