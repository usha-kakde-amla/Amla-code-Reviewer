# Performance Code Quality Rules

A reference guide for performance-related code quality rules.

---

## PERF101 — Avoid Database Calls Inside Loops
**Severity:** Error

**Description:** Executing database calls inside loops (especially nested loops) leads to severe performance issues due to repeated I/O operations.

**Message:** Avoid database/API calls inside loops. Fetch data in bulk before the loop.

**Detection:**
- DB call inside `for`/`foreach` loop
- Nested loop with DB/API call

**Fix:** Move database call outside loop and use bulk fetch or join queries.

---

## PERF102 — Avoid Large Session Objects
**Severity:** Warning

**Description:** Storing large objects in session increases memory usage and slows down read/write operations, potentially causing request state issues.

**Message:** Avoid storing large data in Session. Keep session lightweight.

**Detection:**
- Large object stored in Session
- Session storing collections or heavy objects

**Fix:** Store only necessary identifiers (e.g., IDs) and fetch data when needed.

---

## PERF103 — Cache Frequent API Calls
**Severity:** Info

**Description:** Repeated API calls degrade performance. Frequently accessed data should be cached.

**Message:** Cache frequently used API responses to improve performance.

**Detection:**
- Repeated API call pattern
- Same API called multiple times

**Fix:** Use in-memory caching or distributed caching (e.g., `MemoryCache`).

---

## PERF104 — Optimize LINQ Query Order
**Severity:** Warning

**Description:** When querying large datasets, filtering should be applied before projection to reduce data processing.

**Message:** Apply `Where` before `Select` for better performance on large datasets.

**Detection:**
- `Select` before `Where` in LINQ on large dataset

**Fix:** Reorder LINQ query: `Where` → `Select`.

---

## PERF105 — Avoid Repeated API or Function Calls
**Severity:** Warning

**Description:** Calling the same API or function multiple times unnecessarily increases execution time.

**Message:** Avoid repeated calls. Store result and reuse.

**Detection:**
- Same method/API called multiple times with same parameters

**Fix:** Store result in a variable or cache.

---

## PERF106 — Improve User Interaction Responsiveness (INP)
**Severity:** Info

**Description:** User actions should provide immediate feedback (e.g., loader) to improve perceived performance and Interaction to Next Paint (INP).

**Message:** Provide immediate UI feedback (loader/spinner) on user actions.

**Detection:**
- Button click without UI feedback
- No loader/spinner on async action

**Fix:** Show loader/spinner before starting async operation.

---

## PERF107 — Avoid Deserialization Inside Loops
**Severity:** Warning

**Description:** Repeated deserialization inside loops causes high CPU and memory overhead.

**Message:** Avoid deserialization inside loops. Deserialize once and reuse.

**Detection:**
- Deserialization inside loop (e.g., JSON/XML parsing)

**Fix:** Move deserialization logic outside loop.

---

## PERF108 — Use Count Property Instead of Count()
**Severity:** Info

**Description:** Using `Count()` on collections enumerates them, while the `Count` property is faster.

**Message:** Use `Count` property instead of `Count()` method.

**Detection:**
- `Collection.Count()` used instead of `Count` property

**Fix:** Replace `Count()` with `Count`.

---

## PERF109 — Use Parallel.ForEach for Parallelizable Work
**Severity:** Info

**Description:** Tasks that can be executed in parallel should use `Parallel.ForEach` to improve performance on multi-core processors.

**Message:** Use `Parallel.ForEach` for work that can be done in parallel.

**Detection:**
- `foreach` loop can be parallelized
- Independent iterations detected

**Fix:** Replace `foreach` with `Parallel.ForEach` where iterations are independent.

---

## PERF110 — Use Task.Run for Fire-and-Forget API Calls
**Severity:** Info

**Description:** API calls or tasks that can run independently should be executed using `Task.Run` to avoid blocking the main thread.

**Message:** Run non-critical or independent tasks using `Task.Run` to improve responsiveness.

**Detection:**
- Long-running API or non-critical task executed synchronously

**Fix:** Wrap the call in `Task.Run` and handle exceptions appropriately.
