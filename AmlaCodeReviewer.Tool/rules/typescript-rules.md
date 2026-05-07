# TypeScript Code Quality Rules

A reference guide for TypeScript security and performance rules.

---

## Security Rules

### TS_SEC001 — Avoid Using any Type
**Severity:** Warning

**Description:** Using `any` bypasses TypeScript type safety.

**Message:** Avoid using `any` type.

**Detection:**
- `: any`
- `as any`

**Fix:** Use strict typing or generics.

---

### TS_SEC002 — Unsafe Non-Null Assertion
**Severity:** Warning

**Description:** Using `!` operator may cause runtime exceptions.

**Message:** Unsafe non-null assertion detected.

**Detection:**
- `!.`
- `!;`

**Fix:** Add proper null checks.

---

### TS_SEC003 — Unsafe Type Assertion
**Severity:** Warning

**Description:** Incorrect casting using `as` may hide bugs.

**Message:** Unsafe type assertion detected.

**Detection:**
- `as unknown as`

**Fix:** Use proper type guards.

---

### TS_SEC004 — Unvalidated External Data
**Severity:** Warning

**Description:** External API data should be validated before usage.

**Message:** External data used without validation.

**Detection:**
- `fetch(`
- `axios.get(`

**Fix:** Validate using Zod, io-ts, or schema validators.

---

## Performance Rules

### TS_PERF001 — Inefficient Interface Re-declaration
**Severity:** Info

**Description:** Repeated interface merging increases complexity.

**Message:** Multiple interface declarations detected.

**Detection:**
- Interface redeclared multiple times

**Fix:** Consolidate interface definitions.

---

### TS_PERF002 — Excessive Generic Constraints
**Severity:** Info

**Description:** Overly complex generics impact readability and performance.

**Message:** Complex generic constraint detected.

**Detection:**
- `<T extends`

**Fix:** Simplify generic usage.

---

### TS_PERF003 — Inefficient Object Cloning
**Severity:** Info

**Description:** Deep cloning using JSON serialization impacts performance.

**Message:** Inefficient deep clone detected.

**Detection:**
- `JSON.parse(JSON.stringify(`

**Fix:** Use `structuredClone`.

---

### TS_PERF004 — Unused Types or Interfaces
**Severity:** Info

**Description:** Unused type declarations increase bundle size.

**Message:** Unused type detected.

**Detection:**
- Unused interface
- Unused type

**Fix:** Remove unused types or interfaces.

---

### TS_PERF005 — Improper Async Return Type
**Severity:** Warning

**Description:** Async functions should explicitly return `Promise<T>`.

**Message:** Async function missing return type.

**Detection:**
- Async function without return type

**Fix:** Specify `Promise<T>` return type.
