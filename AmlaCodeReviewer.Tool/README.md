# 🛡️ Amla Code Reviewer — .NET Global CLI Tool

AI-powered PR code reviewer that reads custom rules from `.md` files and posts inline GitHub PR comments using Gemini.

---

## Install

### From NuGet (once published)
```bash
dotnet tool install -g AmlaCodeReviewer.Tool
```

### From local source (dev)
```bash
cd AmlaCodeReviewer.Tool
dotnet pack -c Release -o ./nupkg
dotnet tool install -g AmlaCodeReviewer.Tool --add-source ./nupkg --version 1.0.0
```

### Verify install
```bash
amla-review help
```

---

## Quick Start

```bash
# Set your credentials
export GEMINI_API_KEY=your_gemini_key
export GITHUB_TOKEN=ghp_your_pat

# Review a PR (posts inline comments automatically)
amla-review review --pr https://github.com/org/repo/pull/42

# Use custom rules from a folder
amla-review review --pr https://github.com/org/repo/pull/42 --rules ./my-rules

# Dry run (no comment posting)
amla-review review --pr <url> --no-post-comments --output report.json

# Fail CI if error-level violations found
amla-review review --pr <url> --fail-on-error
```

---

## Commands

| Command | Description |
|---|---|
| `amla-review review` | Analyse a PR and post inline GitHub comments |
| `amla-review init [dir]` | Scaffold a `rules/` folder with a starter rule |
| `amla-review rules list` | List all loaded rules |
| `amla-review help` | Show usage |

### `review` options

| Flag | Default | Description |
|---|---|---|
| `--pr <url>` | _(required)_ | GitHub PR URL |
| `--rules <dir>` | `./rules` | Directory with `.md` rule files |
| `--output <file>` | `report.json` | JSON report output path |
| `--suggest-rules <file>` | _(off)_ | Write AI-suggested rules to file |
| `--token <token>` | `$GITHUB_TOKEN` | GitHub Personal Access Token |
| `--no-post-comments` | _(off)_ | Dry run — skip posting to GitHub |
| `--fail-on-error` | _(off)_ | Exit code 1 if error violations found |

### Environment variables

| Variable | Required | Description |
|---|---|---|
| `GEMINI_API_KEY` | ✅ | Google Gemini API key |
| `GITHUB_TOKEN` | For private repos & posting comments | GitHub PAT |
| `GEMINI_MODEL` | ✅ (default: `gemini-2.5-flash`) | Gemini model name |

---

## Rule File Format

Rules live in `.md` files inside your `rules/` directory. Each rule is separated by `---`.

```markdown
## SEC001 — Avoid Hardcoded Credentials
**Severity:** Error

**Description:** Hardcoding API keys or passwords in code is a security risk.

**Message:** Do not hardcode sensitive credentials in code.

**Detection:**
- Hardcoded password
- API key in string literal

**Fix:** Use environment variables or Azure Key Vault.

---

## PERF001 — Avoid Select Star
**Severity:** Warning

**Description:** SELECT * fetches unnecessary columns.

**Message:** Specify only the columns you need instead of SELECT *.

**Detection:**
- SELECT * in query

**Fix:** Replace SELECT * with explicit column names.
```

### Severity levels
- `error` / `major` / `blocker` / `critical` → reported as **error** (can fail CI with `--fail-on-error`)
- `warning` / `minor` → reported as **warning**
- `info` → reported as **info**

---

## GitHub Actions Integration

Add this to your **target repo** (the repo whose PRs you want reviewed):

```yaml
# .github/workflows/amla-code-review.yml
name: 🛡️ Amla Code Reviewer

on:
  pull_request:
    types: [opened, synchronize, reopened]

permissions:
  pull-requests: write
  contents: read

jobs:
  amla-code-review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Install amla-review
        run: dotnet tool install -g AmlaCodeReviewer.Tool

      - name: Review PR
        env:
          GEMINI_API_KEY: ${{ secrets.GEMINI_API_KEY }}
          GITHUB_TOKEN:   ${{ secrets.GITHUB_TOKEN }}
        run: |
          amla-review review \
            --pr "https://github.com/${{ github.repository }}/pull/${{ github.event.pull_request.number }}" \
            --rules ./rules \
            --output report.json \
            --fail-on-error
```

**Required secrets in the target repo:**
- `GEMINI_API_KEY` — your Google Gemini API key
- `GITHUB_TOKEN` — auto-provided by GitHub Actions (no setup needed)

---

## Project Structure

```
AmlaCodeReviewer.Tool/
├── AmlaCodeReviewer.Tool.csproj   ← PackAsTool = true
├── Program.cs                     ← CLI entry point + all logic
├── rules/                         ← Default bundled rules
│   ├── security-rules.md
│   ├── cshtml-rules.md
│   ├── typescript-rules.md
│   ├── performance-rules.md
│   ├── tsql-rules.md
│   ├── js-rules.md
│   ├── vulnerability-rules.md
│   └── no-console-log-rule.md
└── .github/
    └── workflows/
        └── amla-code-review.yml
```

---

## Publish to NuGet

```bash
cd AmlaCodeReviewer.Tool
dotnet pack -c Release -o ./nupkg
dotnet nuget push ./nupkg/AmlaCodeReviewer.Tool.1.0.0.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```
