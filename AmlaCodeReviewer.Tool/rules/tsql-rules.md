# T-SQL Code Quality Rules

A reference guide for T-SQL code quality, security, and performance rules.

---

## TSQL001 — Avoid SELECT *
**Severity:** Major

**Description:** Using `SELECT *` reduces readability, increases coupling to schema changes, and may impact performance.

**Message:** Avoid using `SELECT *`. Specify only required columns.

**Detection:** Detect queries using `SELECT *`.

**Fix:** Replace `SELECT *` with explicit column names.

---

## TSQL002 — Enforce Naming Conventions
**Severity:** Minor

**Description:** Stored procedures, variables, and parameters should follow standard naming conventions.

**Message:** Follow consistent naming conventions for database objects.

**Detection:** Detect object names not following standard naming conventions.

**Fix:** Rename objects to follow standards (e.g., `usp_GetOrdersByCustomer`).

---

## TSQL003 — High Cognitive Complexity
**Severity:** Major

**Description:** Procedures with deeply nested logic are difficult to maintain and understand.

**Message:** Reduce cognitive complexity by simplifying logic.

**Detection:** Detect stored procedures with nesting/complexity above threshold (e.g., >15).

**Fix:** Refactor procedure into smaller, simpler units.

---

## TSQL004 — Missing TRY CATCH Error Handling
**Severity:** Critical

**Description:** Data modification logic should be wrapped in `TRY...CATCH` blocks to handle runtime errors safely.

**Message:** Wrap DML operations inside `TRY...CATCH` blocks.

**Detection:** Detect DML statements not enclosed in `TRY...CATCH`.

**Fix:** Add `TRY...CATCH` around DML statements.

---

## TSQL005 — Check @@ROWCOUNT After DML
**Severity:** Major

**Description:** DML operations should verify affected row counts to avoid unexpected behavior.

**Message:** Validate affected rows using `@@ROWCOUNT`.

**Detection:** Detect DML operations without `@@ROWCOUNT` validation.

**Fix:** Add `IF @@ROWCOUNT` check after DML operations.

---

## TSQL006 — Avoid Dynamic SQL Without Parameters
**Severity:** Blocker

**Description:** Dynamic SQL concatenated with user input opens the door to SQL Injection attacks.

**Message:** Use parameterized queries instead of concatenated dynamic SQL.

**Detection:** Detect dynamic SQL using string concatenation with variables.

**Fix:** Use `sp_executesql` with parameters.

---

## TSQL007 — Unsafe EXEC With User Input
**Severity:** Blocker

**Description:** `EXEC` statements should never execute user-controlled input directly.

**Message:** Avoid executing user-controlled input directly.

**Detection:** Detect `EXEC` statements with user input.

**Fix:** Validate and parameterize inputs before execution.

---

## TSQL008 — Non-SARGable Predicates
**Severity:** Major

**Description:** Functions on indexed columns prevent efficient index use.

**Message:** Avoid non-SARGable predicates for better performance.

**Detection:** Detect functions applied on indexed columns in `WHERE` clause.

**Fix:** Rewrite predicates to use index-friendly conditions.

---

## TSQL009 — Avoid Cursor Usage
**Severity:** Major

**Description:** Set-based operations are preferred over cursors for performance and scalability.

**Message:** Avoid cursors; use set-based operations.

**Detection:** Detect usage of `CURSOR` in SQL code.

**Fix:** Refactor logic using set-based queries.

---

## TSQL010 — Use of Elevated Execution Context
**Severity:** Critical

**Description:** `EXECUTE AS` and elevated permissions must be manually reviewed.

**Message:** Review usage of elevated execution context.

**Detection:** Detect `EXECUTE AS` usage.

**Fix:** Ensure least privilege principle is followed.

---

## TSQL011 — Sensitive Data Access
**Severity:** Major

**Description:** Accessing sensitive columns should be reviewed for encryption and masking compliance.

**Message:** Sensitive data access should be secured.

**Detection:** Detect queries accessing sensitive columns (e.g., `CreditCardNumber`).

**Fix:** Apply encryption, masking, or access controls.
