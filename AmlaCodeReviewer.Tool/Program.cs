// ┌─────────────────────────────────────────────────────────────────────────┐
// │  AmlaCodeReviewer — .NET Global CLI Tool                                │
// │                                                                         │
// │  Install:                                                               │
// │    dotnet tool install -g AmlaCodeReviewer.Tool                         │
// │    (or from local: dotnet tool install -g --add-source ./nupkg ...)     │
// │                                                                         │
// │  Usage:                                                                 │
// │    amla-review [command] [options]                                      │
// │                                                                         │
// │  Commands:                                                              │
// │    review        Analyse a PR and post inline GitHub comments           │
// │    init          Scaffold a rules/ folder in the current directory      │
// │    rules list    List all loaded rules                                  │
// │    help          Show this help                                         │
// │                                                                         │
// │  Review Options:                                                        │
// │    --pr          <url>    GitHub PR URL (required)                      │
// │    --rules       <dir>    Rules directory (default: ./rules)            │
// │    --output      <path>   JSON report output (default: report.json)     │
// │    --suggest-rules <path> Write suggested rules to this file            │
// │    --token       <token>  GitHub PAT (or env: GITHUB_TOKEN)             │
// │    --post-comments        Post inline comments to the PR (default: true)│
// │    --no-post-comments     Dry run – skip posting comments               │
// │    --fail-on-error        Exit 1 if error-severity violations found     │
// │                                                                         │
// │  Environment Variables:                                                 │
// │    GEMINI_API_KEY    (required for review)                              │
// │    GITHUB_TOKEN      (alternative to --token)                           │
// │    GEMINI_MODEL      (default: gemini-2.5-flash)                        │
// └─────────────────────────────────────────────────────────────────────────┘

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

// ── Banner ─────────────────────────────────────────────────────────────────────
PrintBanner();

// ── Command dispatch ───────────────────────────────────────────────────────────
string command = args.Length > 0 ? args[0].ToLower() : "help";

switch (command)
{
    case "review":
        await RunReview(args[1..]);
        break;

    case "init":
        RunInit(args[1..]);
        break;

    case "rules":
        string sub = args.Length > 1 ? args[1].ToLower() : "list";
        if (sub == "list") RunRulesList(args[2..]);
        else PrintUsage();
        break;

    case "--help":
    case "-h":
    case "help":
    default:
        PrintUsage();
        break;
}

// ═════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═════════════════════════════════════════════════════════════════════════════

static async Task RunReview(string[] args)
{
    // ── Parse flags ────────────────────────────────────────────────────────
    string? prUrl            = GetArg(args, "--pr", null);
    string  outputFile       = GetArg(args, "--output", "report.json")!;
    string  rulesDir         = GetArg(args, "--rules",  DefaultRulesDir())!;
    string? suggestRulesFile = GetArg(args, "--suggest-rules", null);
    bool    postComments     = !HasFlag(args, "--no-post-comments");
    bool    failOnError      = HasFlag(args, "--fail-on-error");

    string? githubToken = GetArg(args, "--token", null)
                       ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

    string model  = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.5-flash";
    string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

    // ── Validate ───────────────────────────────────────────────────────────
    if (string.IsNullOrEmpty(prUrl))
    {
        Console.Error.WriteLine("❌  --pr <url> is required.");
        Console.Error.WriteLine("    Example: amla-review review --pr https://github.com/owner/repo/pull/42");
        Environment.Exit(1);
    }

    if (string.IsNullOrEmpty(apiKey))
    {
        Console.Error.WriteLine("❌  GEMINI_API_KEY environment variable is not set.");
        Console.Error.WriteLine("    export GEMINI_API_KEY=your_key_here");
        Environment.Exit(1);
    }

    Console.WriteLine($"📋  PR:     {prUrl}");
    Console.WriteLine($"📂  Rules:  {rulesDir}");
    Console.WriteLine($"💾  Output: {outputFile}");
    Console.WriteLine($"🤖  Model:  {model}");
    Console.WriteLine($"💬  Post inline comments: {postComments}");
    Console.WriteLine();

    // ── Load rules ─────────────────────────────────────────────────────────
    var rules = LoadRules(rulesDir);
    if (rules.Count == 0)
    {
        Console.WriteLine("⚠️  No rules found — writing empty report.");
        File.WriteAllText(outputFile, "[]");
        return;
    }

    // ── Fetch PR diff ──────────────────────────────────────────────────────
    var http = BuildHttpClient(githubToken);
    var addedLines = await FetchPRDiff(http, prUrl);

    if (addedLines.Count == 0)
    {
        Console.WriteLine("ℹ️  No added lines in PR — writing empty report.");
        File.WriteAllText(outputFile, "[]");
        return;
    }

    // ── Filter & chunk ─────────────────────────────────────────────────────
    var filteredLines = PreFilterLines(addedLines, rules);
    if (filteredLines.Count == 0)
    {
        Console.WriteLine("ℹ️  No lines matched any rule — writing empty report.");
        File.WriteAllText(outputFile, "[]");
        return;
    }

    const int ChunkSize = 300;
    var chunks = ChunkLines(filteredLines, ChunkSize);
    Console.WriteLine($"🔪  Split {filteredLines.Count} line(s) into {chunks.Count} chunk(s) of max {ChunkSize}\n");

    if (suggestRulesFile != null)
        File.WriteAllText(suggestRulesFile, "[]\n");

    var allIssues = new List<Violation>();
    bool includeSuggestions = suggestRulesFile != null;

    for (int i = 0; i < chunks.Count; i++)
    {
        Console.WriteLine($"⚙️  Chunk {i + 1}/{chunks.Count} ({chunks[i].Count} lines)...");

        string      prompt      = BuildPrompt(rules, chunks[i], includeSuggestions);
        JsonElement result      = await CallGemini(http, prompt, includeSuggestions, model, apiKey!);

        List<Violation>     rawIssues;
        List<SuggestedRule> suggestedRules;
        ParseGeminiResult(result, out rawIssues, out suggestedRules);

        var sanitized = SanitizeIssues(rawIssues, rules);
        allIssues.AddRange(sanitized);
        Console.WriteLine($"   → {sanitized.Count} violation(s), {suggestedRules.Count} suggestion(s)");

        if (suggestRulesFile != null)
            WriteSuggestedRules(suggestedRules, suggestRulesFile);
    }

    // ── Write report ───────────────────────────────────────────────────────
    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(outputFile, JsonSerializer.Serialize(allIssues, jsonOptions));

    // ── Post inline GitHub comments ────────────────────────────────────────
    if (postComments && allIssues.Count > 0)
    {
        if (string.IsNullOrEmpty(githubToken))
        {
            Console.WriteLine("⚠️  --post-comments requires GITHUB_TOKEN or --token. Skipping comment posting.");
        }
        else
        {
            await PostInlineComments(http, prUrl, githubToken!, allIssues, suggestRulesFile);
        }
    }

    // ── Summary ────────────────────────────────────────────────────────────
    PrintSummary(allIssues, suggestRulesFile, outputFile);

    // ── Exit code ──────────────────────────────────────────────────────────
    if (failOnError)
    {
        int errorCount = allIssues.Count(x => x.Severity?.ToLower() == "error");
        if (errorCount > 0)
        {
            Console.WriteLine($"\n❌  Exiting with code 1 ({errorCount} error-level violation(s)).");
            Environment.Exit(1);
        }
    }
}

static void RunInit(string[] args)
{
    string targetDir = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "rules");

    if (Directory.Exists(targetDir))
    {
        Console.WriteLine($"ℹ️  Directory already exists: {targetDir}");
        Console.WriteLine("    Add your own .md rule files following the rule format.");
        return;
    }

    Directory.CreateDirectory(targetDir);

    // Write a starter rule file
    string starterRule = @"# My Custom Rules

A starter rules file for AmlaCodeReviewer.

---

## CUSTOM001 — Example Rule
**Severity:** Warning

**Description:** This is an example rule. Replace with your own rules.

**Message:** Replace this with a helpful message for the developer.

**Detection:**
- Pattern or keyword to look for in code

**Fix:** Describe how the developer should fix the violation.

---
";
    File.WriteAllText(Path.Combine(targetDir, "my-rules.md"), starterRule);

    Console.WriteLine($"✅  Scaffolded rules directory: {targetDir}");
    Console.WriteLine("    Edit the .md files to define your own rules.");
    Console.WriteLine("    Rule format: ## RULEID — Title");
    Console.WriteLine("    Fields: Severity, Description, Message, Detection, Fix");
    Console.WriteLine();
    Console.WriteLine("    Then run:");
    Console.WriteLine($"    amla-review review --rules \"{targetDir}\" --pr <github-pr-url>");
}

static void RunRulesList(string[] args)
{
    string rulesDir = GetArg(args, "--rules", DefaultRulesDir())!;
    var rules = LoadRules(rulesDir);

    if (rules.Count == 0)
    {
        Console.WriteLine($"No rules found in: {rulesDir}");
        return;
    }

    Console.WriteLine($"{"RuleId",-20} {"Severity",-10} {"Title",-50}");
    Console.WriteLine(new string('-', 82));

    foreach (var r in rules.OrderBy(r => r.RuleId))
    {
        string sev = (r.Severity ?? "?").ToUpper();
        string marker = sev switch { "ERROR" => "🔴", "WARNING" => "🟡", _ => "🔵" };
        Console.WriteLine($"{r.RuleId,-20} {marker} {sev,-8} {r.Title,-50}");
    }

    Console.WriteLine($"\nTotal: {rules.Count} rule(s) from {rulesDir}");
}

// ═════════════════════════════════════════════════════════════════════════════
// INLINE COMMENT POSTING
// ═════════════════════════════════════════════════════════════════════════════

static async Task PostInlineComments(
    HttpClient http,
    string prUrl,
    string githubToken,
    List<Violation> issues,
    string? suggestRulesFile)
{
    Console.WriteLine("\n💬  Posting inline comments to GitHub PR...");

    // Parse owner/repo/number from PR URL
    var match = Regex.Match(prUrl, @"github\.com/([^/]+)/([^/]+)/pull/(\d+)");
    if (!match.Success)
    {
        Console.Error.WriteLine("❌  Could not parse owner/repo/number from PR URL.");
        return;
    }

    string owner  = match.Groups[1].Value;
    string repo   = match.Groups[2].Value;
    int    prNum  = int.Parse(match.Groups[3].Value);

    // Get PR head SHA
    string prApiUrl = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNum}";
    var prResp = await http.GetAsync(prApiUrl);
    if (!prResp.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"❌  GitHub API error fetching PR: {prResp.StatusCode}");
        return;
    }

    var prData   = await prResp.Content.ReadAsStringAsync();
    var prDoc    = JsonDocument.Parse(prData);
    string? sha  = prDoc.RootElement.GetProperty("head").GetProperty("sha").GetString();

    if (string.IsNullOrEmpty(sha))
    {
        Console.Error.WriteLine("❌  Could not get PR head SHA.");
        return;
    }

    // Group violations by file+line
    var grouped = new Dictionary<string, (string path, int line, List<Violation> items)>();
    foreach (var issue in issues)
    {
        if (string.IsNullOrEmpty(issue.File) || issue.FileLine == 0) continue;
        string key = $"{issue.File}::{issue.FileLine}";
        if (!grouped.ContainsKey(key))
            grouped[key] = (issue.File, issue.FileLine, new List<Violation>());
        grouped[key].items.Add(issue);
    }

    if (grouped.Count == 0)
    {
        Console.WriteLine("   No comments to post (all issues missing file/line info).");
        return;
    }

    // Build review comments
    var comments = grouped.Values.Select(g =>
    {
        string icon(string? sev) => (sev ?? "").ToLower() switch
        {
            "error"   => "🔴",
            "warning" => "🟡",
            "info"    => "🔵",
            _         => "⚪"
        };

        string body = string.Join("\n\n---\n\n", g.items.Select(issue =>
            $"{icon(issue.Severity)} **[{issue.RuleId}] {issue.Title}**\n" +
            $"**Issue:** {issue.Message}\n" +
            $"**Fix:** {issue.Fix}"));

        return new { path = g.path, line = g.line, side = "RIGHT", body };
    }).ToArray();

    // Build review body
    string reviewBody = "🛡️ **Amla Code Reviewer**";
    if (suggestRulesFile != null && File.Exists(suggestRulesFile))
    {
        try
        {
            var suggested = JsonSerializer.Deserialize<List<SuggestedRule>>(File.ReadAllText(suggestRulesFile));
            if (suggested?.Count > 0)
                reviewBody += $"\n\n---\n> 📬 **{suggested.Count} new rule suggestion(s)** identified.";
        }
        catch { }
    }

    // POST the review
    string reviewApiUrl = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNum}/reviews";
    var reviewPayload = new
    {
        commit_id = sha,
        @event    = "COMMENT",
        body      = reviewBody,
        comments
    };

    var content = new StringContent(
        JsonSerializer.Serialize(reviewPayload),
        Encoding.UTF8,
        "application/json");

    var resp = await http.PostAsync(reviewApiUrl, content);
    if (resp.IsSuccessStatusCode)
        Console.WriteLine($"   ✅  Posted {comments.Length} inline comment(s).");
    else
    {
        var errText = await resp.Content.ReadAsStringAsync();
        Console.Error.WriteLine($"❌  GitHub API error posting review: {resp.StatusCode}\n{errText}");
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// CORE LOGIC (same as original, cleaned up)
// ═════════════════════════════════════════════════════════════════════════════

static HttpClient BuildHttpClient(string? githubToken)
{
    var http = new HttpClient();
    http.DefaultRequestHeaders.Add("User-Agent", "AmlaCodeReviewer/1.0");
    if (!string.IsNullOrEmpty(githubToken))
    {
        http.DefaultRequestHeaders.Add("Authorization", "token " + githubToken);
        Console.WriteLine("🔑  GitHub token loaded: " + githubToken[..Math.Min(10, githubToken.Length)] + "...");
    }
    else
    {
        Console.WriteLine("⚠️  No GitHub token — public repos only.");
    }
    return http;
}

static async Task<List<DiffLine>> FetchPRDiff(HttpClient http, string prUrl)
{
    // Parse owner/repo/pull-number from PR URL
    var match = Regex.Match(prUrl, @"github\.com/([^/]+)/([^/]+)/pull/(\d+)");

    if (!match.Success)
    {
        Console.Error.WriteLine("❌ Invalid GitHub PR URL.");
        Environment.Exit(1);
    }

    string owner = match.Groups[1].Value;
    string repo = match.Groups[2].Value;
    string prNumber = match.Groups[3].Value;

    // GitHub API URL
    string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}";

    Console.WriteLine($"📥 Fetching PR diff via GitHub API:");
    Console.WriteLine($"    {apiUrl}");

    var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

    // Request diff format
    request.Headers.Accept.ParseAdd("application/vnd.github.v3.diff");

    var response = await http.SendAsync(request);

    if (!response.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"❌ GitHub API error: {(int)response.StatusCode} {response.ReasonPhrase}");

        if ((int)response.StatusCode == 404)
        {
            Console.Error.WriteLine("\nPossible reasons:");
            Console.Error.WriteLine("1. Repository is private");
            Console.Error.WriteLine("2. GITHUB_TOKEN lacks permissions");
            Console.Error.WriteLine("3. Pull request does not exist");
        }

        Environment.Exit(1);
    }

    var diffContent = await response.Content.ReadAsStringAsync();

    Console.WriteLine($"    ✅ Fetched {diffContent.Length} chars\n");

    return ParseDiff(diffContent);
}

static List<DiffLine> ParseDiff(string diffContent)
{
    var     lines    = diffContent.Split('\n');
    var     added    = new List<DiffLine>();
    string? curFile  = null;
    int     fileLine = 0;

    foreach (var line in lines)
    {
        if (line.StartsWith("diff --git "))
        {
            var m = Regex.Match(line, @"b\/(.+)$");
            curFile  = m.Success ? m.Groups[1].Value : null;
            fileLine = 0;
            continue;
        }
        if (line.StartsWith("@@"))
        {
            var m = Regex.Match(line, @"\+(\d+)");
            fileLine = m.Success ? int.Parse(m.Groups[1].Value) - 1 : 0;
            continue;
        }
        if (line.StartsWith("+++ ") || line.StartsWith("--- ")) continue;

        if (line.StartsWith("+"))
        {
            fileLine++;
            if (curFile != null)
                added.Add(new DiffLine(curFile, fileLine, line[1..]));
        }
        else if (!line.StartsWith("-"))
        {
            fileLine++;
        }
    }

    Console.WriteLine($"    Parsed {added.Count} added line(s)");
    return added;
}

static List<DiffLine> PreFilterLines(List<DiffLine> addedLines, List<Rule> rules)
{
    var relevant = new HashSet<string>();
    foreach (var dl in addedLines)
        foreach (var rule in rules)
            if (string.IsNullOrEmpty(rule.FilePattern) || Regex.IsMatch(dl.File, rule.FilePattern))
            {
                relevant.Add(dl.File);
                break;
            }
    return addedLines.Where(l => relevant.Contains(l.File)).ToList();
}

static List<List<DiffLine>> ChunkLines(List<DiffLine> lines, int size)
{
    var chunks = new List<List<DiffLine>>();
    for (int i = 0; i < lines.Count; i += size)
        chunks.Add(lines.Skip(i).Take(size).ToList());
    return chunks;
}

static List<Rule> LoadRules(string rulesDir)
{
    if (!Directory.Exists(rulesDir))
    {
        Console.Error.WriteLine($"❌  Rules directory not found: {rulesDir}");
        Console.Error.WriteLine("    Run `amla-review init` to scaffold a rules directory.");
        Environment.Exit(1);
    }

    var files = Directory.GetFiles(rulesDir, "*.md");
    if (files.Length == 0)
    {
        Console.WriteLine($"⚠️  No .md rule files in: {rulesDir}");
        return new List<Rule>();
    }

    var rules = new List<Rule>();
    foreach (var file in files)
    {
        try
        {
            var mdContent = File.ReadAllText(file);
            var list      = ParseMarkdownRules(mdContent, Path.GetFileName(file));
            foreach (var rule in list)
            {
                ValidateRule(rule, Path.GetFileName(file));
                rules.Add(rule);
            }
            Console.WriteLine($"    📄  {Path.GetFileName(file)}: {list.Count} rule(s)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"⚠️  Skipping {Path.GetFileName(file)}: {ex.Message}");
        }
    }

    Console.WriteLine($"    ✅  Total rules: {rules.Count}\n");
    return rules;
}

static List<Rule> ParseMarkdownRules(string content, string fileName)
{
    var rules = new List<Rule>();

    var blocks = content.Split(
        new[] { "\n---", "\r\n---" },
        StringSplitOptions.RemoveEmptyEntries);

    foreach (var block in blocks)
    {
        var lines = block.Split('\n');

        string? header = lines.FirstOrDefault(
            l => l.TrimStart().StartsWith("## "));

        if (header == null)
            continue;

        var headerText = header.TrimStart('#').Trim();

        string ruleId;
        string title;

        int dashIdx = headerText.IndexOf(" \u2014 ");

        if (dashIdx < 0)
            dashIdx = headerText.IndexOf(" - ");

        if (dashIdx >= 0)
        {
            ruleId = headerText[..dashIdx].Trim();
            title = headerText[(dashIdx + 3)..].Trim();
        }
        else
        {
            ruleId = headerText.Trim();
            title = headerText.Trim();
        }

        if (string.IsNullOrWhiteSpace(ruleId))
            continue;

        string severity = ExtractField(block, "Severity");
        string description = ExtractField(block, "Description");
        string message = ExtractField(block, "Message");
        string fix = ExtractField(block, "Fix");

        // NEW: Read FilePattern from markdown
        string filePattern = ExtractField(block, "FilePattern");

        var detection = ExtractBulletList(block, "Detection");

        rules.Add(new Rule
        {
            RuleId = ruleId,
            Title = title,
            Severity = NormalizeSeverity(severity),
            Description = description,
            Detection = detection.Count > 0
                ? detection
                : new List<string> { description },

            Message = message,
            Fix = fix,

            // NEW: assign file pattern
            FilePattern = filePattern
        });
    }

    return rules;
}

static string ExtractField(string block, string fieldName)
{
    var match = Regex.Match(block,
        @"\*\*" + Regex.Escape(fieldName) + @"\:\*\*\s*(.+?)(?=\n\*\*|\n---|\n##|$)",
        RegexOptions.Singleline);

    if (!match.Success) return string.Empty;

    return match.Groups[1].Value
        .Replace("`", "")
        .Trim()
        .Split('\n')[0]
        .Trim();
}

static List<string> ExtractBulletList(string block, string fieldName)
{
    var items        = new List<string>();
    var sectionMatch = Regex.Match(block, @"\*\*" + Regex.Escape(fieldName) + @"\:\*\*");
    if (!sectionMatch.Success) return items;

    int start    = sectionMatch.Index + sectionMatch.Length;
    var rest     = block[start..];
    var endMatch = Regex.Match(rest, @"\n\*\*|\n---|\n##");
    var section  = endMatch.Success ? rest[..endMatch.Index] : rest;

    foreach (var line in section.Split('\n'))
    {
        var trimmed = line.TrimStart(' ', '\r').TrimStart('-').Replace("`", "").Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
            items.Add(trimmed);
    }

    return items;
}

static string NormalizeSeverity(string raw) => raw.ToLower() switch
{
    "error"    => "error",
    "major"    => "error",
    "blocker"  => "error",
    "critical" => "error",
    "warning"  => "warning",
    "minor"    => "warning",
    "info"     => "info",
    _          => "warning"
};

static void ValidateRule(Rule rule, string sourceFile)
{
    var fields = new (string? Value, string Name)[]
    {
        (rule.RuleId,   "RuleId"),
        (rule.Title,    "Title"),
        (rule.Severity, "Severity"),
        (rule.Message,  "Message"),
        (rule.Fix,      "Fix"),
    };

    foreach (var (value, name) in fields)
        if (string.IsNullOrWhiteSpace(value))
            throw new Exception($"Rule in {sourceFile} is missing required field: {name}");

    string[] valid = { "error", "warning", "info" };
    if (!valid.Contains(rule.Severity!.ToLower()))
        throw new Exception($"Rule {rule.RuleId} Severity must be error | warning | info");
}

static string BuildPrompt(List<Rule> rules, List<DiffLine> addedLines, bool includeSuggestions)
{
    var    jsonOpts  = new JsonSerializerOptions { WriteIndented = true };
    string rulesJson = JsonSerializer.Serialize(rules, jsonOpts);
    string diffText  = string.Join("\n", addedLines.Select(l =>
        $"[FILE: {l.File}] [LINE: {l.FileLine}] {l.Content}"));

    string outputNote = includeSuggestions
        ? "Return ONLY a valid JSON OBJECT with keys \"violations\" and \"suggestedRules\". No markdown, no code fences."
        : "Return ONLY a valid JSON array. No markdown, no code fences. Empty array if no violations: []";

    string reminder = includeSuggestions
        ? "Return ONLY the raw JSON object with \"violations\" and \"suggestedRules\" keys. No extra text."
        : "Return ONLY the raw JSON array. No extra text.";

    string violationSchema =
        "{\n" +
        "  \"ruleId\":   \"<RuleId from the matching rule>\",\n" +
        "  \"title\":    \"<Title from the matching rule>\",\n" +
        "  \"severity\": \"<Severity from the matching rule>\",\n" +
        "  \"message\":  \"<Message from the matching rule>\",\n" +
        "  \"fix\":      \"<Fix from the matching rule>\",\n" +
        "  \"file\":     \"<filename from the diff line>\",\n" +
        "  \"fileLine\": <line number as integer>\n" +
        "}";

    string suggestionBlock = includeSuggestions
        ? "\n================================\nRULE SUGGESTION TASK (Part 2):\n================================\n" +
          "After identifying violations, also look at the code holistically.\n" +
          "If you spot recurring anti-patterns not covered by the existing rules, suggest them.\n\n" +
          "Add a \"suggestedRules\" array to your JSON response.\n" +
          "Each item: { \"RuleId\": \"SUGGESTED-SLUG\", \"Title\": \"...\", \"Description\": \"...\", " +
          "\"Severity\": \"error|warning|info\", \"Detection\": [...], \"Message\": \"...\", \"Fix\": \"...\" }\n" +
          "Return \"suggestedRules\": [] if no new rules are warranted.\n"
        : string.Empty;

    var sb = new StringBuilder();
    sb.AppendLine("You are a strict code review enforcement engine.");
    sb.AppendLine();
    sb.AppendLine("You will be given:");
    sb.AppendLine("1. A list of CUSTOM RULES defined by the team.");
    sb.AppendLine("2. Added lines from a GitHub Pull Request diff.");
    sb.AppendLine();
    sb.AppendLine("YOUR ONLY JOB (Part 1 - Violations):");
    sb.AppendLine("- Use semantic understanding along with Detection hints to identify violations.");
    sb.AppendLine("- Match coding patterns, anti-patterns, loops, SQL usage, API calls, and performance issues even if syntax varies.");
    sb.AppendLine("- Report ONLY violations of the exact rules listed below.");
    sb.AppendLine("- Do NOT suggest improvements or issues not covered by the rules.");
    sb.AppendLine("- Do NOT invent new RuleIds not in the rules list.");
    sb.AppendLine("- If a line does not violate any rule, ignore it.");
    sb.AppendLine();
    sb.AppendLine("OUTPUT FORMAT:");
    sb.AppendLine(outputNote);
    sb.AppendLine();
    sb.AppendLine("Each violation object must have EXACTLY these fields:");
    sb.AppendLine(violationSchema);
    sb.AppendLine();
    sb.AppendLine("================================");
    sb.AppendLine("CUSTOM RULES (enforce ONLY these):");
    sb.AppendLine("================================");
    sb.AppendLine(rulesJson);
    sb.AppendLine();
    sb.AppendLine("================================");
    sb.AppendLine("PULL REQUEST DIFF (added lines only):");
    sb.AppendLine("================================");
    sb.AppendLine(diffText);
    sb.AppendLine(suggestionBlock);
    sb.AppendLine("Remember: " + reminder);

    return sb.ToString();
}

static async Task<JsonElement> CallGemini(
    HttpClient http, string prompt, bool includeSuggestions, string model, string apiKey)
{
    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

    string systemText = includeSuggestions
        ? "You are a code review enforcement engine. Report violations AND suggest new rules. Return raw JSON only."
        : "You are a code rule enforcement engine. ONLY report violations of the exact rules given. Return a raw JSON array only.";

    var requestBody = new
    {
        contents         = new[] { new { parts = new[] { new { text = prompt } } } },
        generationConfig = new
        {
            temperature      = 0,
            topP             = 1,
            topK             = 1,
            responseMimeType = "application/json",
            maxOutputTokens  = 8192
        },
        systemInstruction = new { parts = new[] { new { text = systemText } } }
    };

    Console.Write($"   🤖  Calling Gemini ({model})... ");

    var content = new StringContent(
        JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

    var response = await http.PostAsync(url, content);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Gemini API error {(int)response.StatusCode}: {error}");
    }

    Console.WriteLine("done");

    var data    = await response.Content.ReadAsStringAsync();
    var doc     = JsonDocument.Parse(data);
    var rawText = doc.RootElement
        .GetProperty("candidates")[0]
        .GetProperty("content")
        .GetProperty("parts")[0]
        .GetProperty("text")
        .GetString() ?? "[]";

    var cleaned = Regex.Replace(rawText.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
    cleaned     = Regex.Replace(cleaned, @"\s*```$", "").Trim();

    try   { return JsonSerializer.Deserialize<JsonElement>(cleaned); }
    catch { Console.Error.WriteLine("Failed to parse Gemini response:\n" + cleaned[..Math.Min(500, cleaned.Length)]); throw; }
}

static void ParseGeminiResult(JsonElement result, out List<Violation> violations, out List<SuggestedRule> suggestions)
{
    if (result.ValueKind == JsonValueKind.Array)
    {
        violations  = JsonSerializer.Deserialize<List<Violation>>(result.GetRawText()) ?? new();
        suggestions = new();
    }
    else if (result.ValueKind == JsonValueKind.Object)
    {
        violations  = result.TryGetProperty("violations", out var v)
            ? JsonSerializer.Deserialize<List<Violation>>(v.GetRawText()) ?? new()
            : new();
        suggestions = result.TryGetProperty("suggestedRules", out var s)
            ? JsonSerializer.Deserialize<List<SuggestedRule>>(s.GetRawText()) ?? new()
            : new();
    }
    else
    {
        violations  = new();
        suggestions = new();
    }
}

static List<Violation> SanitizeIssues(List<Violation> issues, List<Rule> rules)
{
    var validIds = new HashSet<string>(rules.Select(r => r.RuleId ?? string.Empty));
    return issues.Where(issue =>
    {
        if (!validIds.Contains(issue.RuleId ?? string.Empty))
        {
            Console.WriteLine($"   ⚠️  Discarding hallucinated rule \"{issue.RuleId}\"");
            return false;
        }
        return true;
    }).ToList();
}

static void WriteSuggestedRules(List<SuggestedRule> suggested, string outputPath)
{
    var valid = suggested.Where(r =>
        !string.IsNullOrEmpty(r.RuleId) &&
        !string.IsNullOrEmpty(r.Title)  &&
        !string.IsNullOrEmpty(r.Severity)).ToList();

    var existing = new List<SuggestedRule>();
    if (File.Exists(outputPath))
    {
        try
        {
            var raw = File.ReadAllText(outputPath).Trim();
            if (!string.IsNullOrEmpty(raw) && raw != "[]")
                existing = JsonSerializer.Deserialize<List<SuggestedRule>>(raw) ?? new();
        }
        catch { }
    }

    var seen   = new HashSet<string>();
    var merged = existing.Concat(valid)
        .Where(r => seen.Add(r.RuleId ?? string.Empty))
        .ToList();

    var opts = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(outputPath, JsonSerializer.Serialize(merged, opts) + "\n");
}

static void PrintSummary(List<Violation> issues, string? suggestRulesFile, string outputFile)
{
    int errors   = issues.Count(x => x.Severity?.ToLower() == "error");
    int warnings = issues.Count(x => x.Severity?.ToLower() == "warning");
    int info     = issues.Count(x => x.Severity?.ToLower() == "info");

    Console.WriteLine("\n===========================================");
    Console.WriteLine("   Results");
    Console.WriteLine("===========================================");
    Console.WriteLine($"   🔴  Errors:   {errors}");
    Console.WriteLine($"   🟡  Warnings: {warnings}");
    Console.WriteLine($"   🔵  Info:     {info}");
    Console.WriteLine($"   📊  Total:    {issues.Count}");

    if (suggestRulesFile != null && File.Exists(suggestRulesFile))
    {
        try
        {
            int count = JsonSerializer.Deserialize<List<SuggestedRule>>(File.ReadAllText(suggestRulesFile))?.Count ?? 0;
            Console.WriteLine($"   📬  Suggested rules: {count}");
        }
        catch { }
    }

    Console.WriteLine($"\n✅  Report written to {outputFile}");
    if (suggestRulesFile != null)
        Console.WriteLine($"✅  Suggested rules written to {suggestRulesFile}");
}

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔════════════════════════════════════════════╗");
    Console.WriteLine("║       🛡️  Amla Code Reviewer  v1.0.0       ║");
    Console.WriteLine("╚════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
}

static void PrintUsage()
{
    Console.WriteLine("Usage:  amla-review <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  review          Analyse a PR and post inline GitHub comments");
    Console.WriteLine("  init [dir]      Scaffold a rules/ folder");
    Console.WriteLine("  rules list      List all loaded rules");
    Console.WriteLine("  help            Show this help");
    Console.WriteLine();
    Console.WriteLine("Review options:");
    Console.WriteLine("  --pr <url>              GitHub PR URL (required)");
    Console.WriteLine("  --rules <dir>           Rules directory  (default: ./rules)");
    Console.WriteLine("  --output <file>         JSON report      (default: report.json)");
    Console.WriteLine("  --suggest-rules <file>  Write suggested rules to this file");
    Console.WriteLine("  --token <token>         GitHub PAT       (or env: GITHUB_TOKEN)");
    Console.WriteLine("  --no-post-comments      Dry run – skip posting to GitHub");
    Console.WriteLine("  --fail-on-error         Exit 1 if error violations found");
    Console.WriteLine();
    Console.WriteLine("Environment variables:");
    Console.WriteLine("  GEMINI_API_KEY    (required for review)");
    Console.WriteLine("  GITHUB_TOKEN      (required for private repos & posting comments)");
    Console.WriteLine("  GEMINI_MODEL      (default: gemini-2.5-flash)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  amla-review review --pr https://github.com/org/repo/pull/42");
    Console.WriteLine("  amla-review review --pr <url> --rules ./my-rules --fail-on-error");
    Console.WriteLine("  amla-review review --pr <url> --no-post-comments --output out.json");
    Console.WriteLine("  amla-review init");
    Console.WriteLine("  amla-review rules list --rules ./rules");
}

// ── Helper: default rules directory (next to tool exe or ./rules) ─────────────
static string DefaultRulesDir()
{
    // 1. ./rules relative to CWD (for project-local usage)
    string cwd = Path.Combine(Directory.GetCurrentDirectory(), "rules");
    if (Directory.Exists(cwd)) return cwd;

    // 2. rules/ bundled with the tool binary
    string exe = Path.Combine(AppContext.BaseDirectory, "rules");
    return exe;
}

static string? GetArg(string[] args, string flag, string? defaultValue)
{
    int idx = Array.IndexOf(args, flag);
    return (idx >= 0 && idx + 1 < args.Length) ? args[idx + 1] : defaultValue;
}

static bool HasFlag(string[] args, string flag) => Array.IndexOf(args, flag) >= 0;

// ═════════════════════════════════════════════════════════════════════════════
// DATA MODELS
// ═════════════════════════════════════════════════════════════════════════════

record DiffLine(string File, int FileLine, string Content);

class Rule
{
    [JsonPropertyName("RuleId")]      public string?       RuleId      { get; set; }
    [JsonPropertyName("Title")]       public string?       Title       { get; set; }
    [JsonPropertyName("Severity")]    public string?       Severity    { get; set; }
    [JsonPropertyName("Description")] public string?       Description { get; set; }
    [JsonPropertyName("Detection")]   public List<string>? Detection   { get; set; }
    [JsonPropertyName("Message")]     public string?       Message     { get; set; }
    [JsonPropertyName("Fix")]         public string?       Fix         { get; set; }
    [JsonPropertyName("filePattern")] public string?       FilePattern { get; set; }
}

class Violation
{
    [JsonPropertyName("ruleId")]   public string? RuleId   { get; set; }
    [JsonPropertyName("title")]    public string? Title    { get; set; }
    [JsonPropertyName("severity")] public string? Severity { get; set; }
    [JsonPropertyName("message")]  public string? Message  { get; set; }
    [JsonPropertyName("fix")]      public string? Fix      { get; set; }
    [JsonPropertyName("file")]     public string? File     { get; set; }
    [JsonPropertyName("fileLine")] public int     FileLine { get; set; }
}

class SuggestedRule
{
    [JsonPropertyName("RuleId")]      public string?       RuleId      { get; set; }
    [JsonPropertyName("Title")]       public string?       Title       { get; set; }
    [JsonPropertyName("Description")] public string?       Description { get; set; }
    [JsonPropertyName("Severity")]    public string?       Severity    { get; set; }
    [JsonPropertyName("Detection")]   public List<string>? Detection   { get; set; }
    [JsonPropertyName("Message")]     public string?       Message     { get; set; }
    [JsonPropertyName("Fix")]         public string?       Fix         { get; set; }
    [JsonPropertyName("example")]     public string?       Example     { get; set; }
    [JsonPropertyName("tags")]        public List<string>? Tags        { get; set; }
}
