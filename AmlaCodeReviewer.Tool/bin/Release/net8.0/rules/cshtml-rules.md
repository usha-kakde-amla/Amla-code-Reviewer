# CSHTML Code Quality Rules

A reference guide for Razor View (.cshtml) code quality and security rules.

---

## CSHTML001 — Avoid Inline JavaScript in Views
**Severity:** MAJOR

**Description:** Embedding JavaScript directly inside .cshtml files reduces maintainability, breaks separation of concerns, and increases debugging complexity.

**Message:** Avoid inline JavaScript in Razor views.

**Detection:** Detect `<script>` tags or inline JavaScript inside .cshtml files.

**Fix:** Move JavaScript logic to external `.js` files and reference them via script tags.

---

## CSHTML002 — Avoid Inline CSS Styling
**Severity:** MINOR

**Description:** Inline CSS leads to poor maintainability and prevents reuse of styles across the application.

**Message:** Avoid inline CSS styling.

**Detection:** Detect `style` attributes or `<style>` blocks in .cshtml files.

**Fix:** Use external CSS files or shared stylesheets.

---

## CSHTML003 — Business Logic in View
**Severity:** CRITICAL

**Description:** Embedding business logic in Razor views violates MVC architecture and makes code difficult to maintain.

**Message:** Do not include business logic in views.

**Detection:** Detect complex `@if`, loops with logic, or service/database calls in .cshtml.

**Fix:** Move logic to Controller or Service layer.

---

## CSHTML004 — Unencoded Output (XSS Risk)
**Severity:** BLOCKER

**Description:** Rendering raw HTML or unencoded user input can lead to Cross-Site Scripting (XSS) vulnerabilities.

**Message:** Avoid rendering unencoded user input.

**Detection:** Detect usage of `@Html.Raw` or unencoded output rendering.

**Fix:** Use default Razor encoding or sanitize input before rendering.

---

## CSHTML005 — Overuse of ViewBag/ViewData
**Severity:** MAJOR

**Description:** Using ViewBag/ViewData reduces type safety and increases runtime errors.

**Message:** Avoid ViewBag/ViewData for passing data.

**Detection:** Detect usage of `ViewBag` or `ViewData` instead of strongly typed models.

**Fix:** Use strongly typed models with `@model` directive.

---

## CSHTML006 — Large View File Size
**Severity:** MINOR

**Description:** Large CSHTML files are hard to maintain and indicate poor component separation.

**Message:** View file is too large.

**Detection:** Detect .cshtml files exceeding size/line threshold (e.g., >500 lines).

**Fix:** Split into partial views or components.

---

## CSHTML007 — Missing Anti-Forgery Token
**Severity:** BLOCKER

**Description:** Forms without anti-forgery tokens are vulnerable to CSRF attacks.

**Message:** Missing anti-forgery token.

**Detection:** Detect `<form>` elements without `@Html.AntiForgeryToken()`.

**Fix:** Add `@Html.AntiForgeryToken()` inside forms.

---

## CSHTML008 — Hardcoded URLs
**Severity:** MINOR

**Description:** Hardcoding URLs breaks routing flexibility and maintainability.

**Message:** Avoid hardcoded URLs.

**Detection:** Detect hardcoded links (e.g., `href="/something"`).

**Fix:** Use `Url.Action()` or tag helpers.

---

## CSHTML009 — Improper Use of Partial Views
**Severity:** MINOR

**Description:** Not using partial views for reusable UI leads to duplication and inconsistencies.

**Message:** Use partial views for reusable UI.

**Detection:** Detect repeated HTML blocks across views.

**Fix:** Create and reuse partial views.

---

## CSHTML010 — Client-Side Validation Missing
**Severity:** MAJOR

**Description:** Forms without client-side validation degrade user experience and increase server load.

**Message:** Missing client-side validation.

**Detection:** Detect forms without validation attributes or scripts.

**Fix:** Enable validation scripts and attributes.

---

## CSHTML011 — Improper Script Loading
**Severity:** MINOR

**Description:** Loading scripts in the head instead of footer affects page performance.

**Message:** Scripts should be loaded at the bottom.

**Detection:** Detect script tags in `<head>` instead of bottom.

**Fix:** Move scripts before closing `</body>`.

---

## CSHTML012 — Mixed Concerns in Razor Blocks
**Severity:** MAJOR

**Description:** Mixing UI rendering and data processing inside Razor blocks leads to poor readability.

**Message:** Avoid mixing logic with UI.

**Detection:** Detect heavy logic inside `@{ }` blocks.

**Fix:** Move logic to backend.

---

## CSHTML013 — Improper Model Null Handling
**Severity:** CRITICAL

**Description:** Accessing model properties without null checks can cause runtime exceptions.

**Message:** Model may be null.

**Detection:** Detect direct model property access without null check.

**Fix:** Add null checks before accessing properties.

---

## CSHTML014 — Unoptimized Image Loading
**Severity:** MINOR

**Description:** Large or unoptimized images impact page performance.

**Message:** Images are not optimized.

**Detection:** Detect large image sizes or missing lazy loading.

**Fix:** Use compressed images and lazy loading.

---

## CSHTML015 — Accessibility Violations
**Severity:** MAJOR

**Description:** Missing alt tags or improper semantic HTML affects accessibility compliance.

**Message:** Accessibility issues detected.

**Detection:** Detect missing `alt` attributes or improper HTML structure.

**Fix:** Add alt tags and use semantic HTML.
