using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;

namespace JobProcessorApi.Controllers;

[ApiController]
[Route("job/assistant")]
public class AssistantController : ControllerBase
{
    private static readonly HttpClient HttpClient = new();
    private static readonly string? OpenAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assistant-logs");

    static AssistantController()
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
        }
        catch { }
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobLink))
            return BadRequest("Job link is required.");

        var (jobText, companyText, profileText) = await GatherContext(request);

        var prompt = $"""
            Analyze this job posting and provide a structured summary.

            Include:
            - Job title
            - Company name
            - Key requirements (must-have)
            - Nice-to-have requirements
            - Experience level expected
            - Location / remote policy
            - Salary range (if mentioned)
            - Application deadline (if mentioned)
            - Brief summary of the role and responsibilities

            Job posting:
            {jobText}

            Company information:
            {companyText}
            """;

        var content = await CallLlm(prompt, request.Language, request.Tone, "gpt-4o-mini");
        await LogResponse("analyze", request, content);
        return Ok(new AssistantResponse { Content = content });
    }

    [HttpPost("application")]
    public async Task<IActionResult> GenerateApplication([FromBody] AssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobLink))
            return BadRequest("Job link is required.");

        var (jobText, companyText, profileText) = await GatherContext(request);

        var prompt = $"""
            Create a tailored job application for this candidate.

            Rules:
            - Only use the candidate's real experience from the profile below. Do not invent employers, dates, education, certifications, or technologies.
            - Write in {request.Language ?? "English"}.
            - Tone: {request.Tone ?? "professional"}.
            - Be specific and reference concrete projects and skills from the profile.
            - If a job requirement is not covered by the candidate's profile, mention it honestly.

            Include these sections:
            1. Short application email (ready to send)
            2. Cover letter
            3. Key selling points (bullet list)
            4. Skills matched to job requirements (bullet list)
            5. Potential gaps or areas to address (bullet list)

            Candidate profile:
            {profileText}

            Job posting:
            {jobText}

            Company information:
            {companyText}

            Extra notes from candidate:
            {request.Notes}
            """;

        var content = await CallLlm(prompt, request.Language, request.Tone, "gpt-4o");
        await LogResponse("application", request, content);
        var matchedSkills = ExtractBulletSection(content, "Skills matched", "Matched skills");
        var gaps = ExtractBulletSection(content, "Potential gaps", "gaps", "areas to address");

        return Ok(new AssistantResponse
        {
            Content = content,
            MatchedSkills = matchedSkills,
            PotentialGaps = gaps
        });
    }

    [HttpPost("cv-summary")]
    public async Task<IActionResult> GenerateCvSummary([FromBody] AssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobLink))
            return BadRequest("Job link is required.");

        var (jobText, companyText, profileText) = await GatherContext(request);

        var prompt = $"""
            Create a tailored CV/resume summary for this candidate, optimized for the job posting below.

            Rules:
            - Only use the candidate's real experience. Do not invent facts.
            - Write in {request.Language ?? "English"}.
            - Tone: {request.Tone ?? "professional"}.
            - Focus on the most relevant experience for this specific role.
            - Keep the summary to 3-5 sentences.
            - Then suggest which experience entries and skills to prioritize on the CV.

            Candidate profile:
            {profileText}

            Job posting:
            {jobText}

            Company information:
            {companyText}

            Extra notes from candidate:
            {request.Notes}
            """;

        var content = await CallLlm(prompt, request.Language, request.Tone, "gpt-4o-mini");
        await LogResponse("cv-summary", request, content);
        return Ok(new AssistantResponse { Content = content });
    }

    [HttpPost("cv-bullets")]
    public async Task<IActionResult> SuggestCvBullets([FromBody] AssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobLink))
            return BadRequest("Job link is required.");

        var (jobText, companyText, profileText) = await GatherContext(request);

        var prompt = $"""
            Suggest improved CV bullet points for this candidate's experience, tailored to the job posting below.

            Rules:
            - Only reframe the candidate's real experience. Do not invent new experience.
            - Write in {request.Language ?? "English"}.
            - Tone: {request.Tone ?? "professional"}.
            - Use strong action verbs.
            - Quantify impact where the profile provides enough information.
            - For each suggestion, show the original bullet and the improved version.

            Candidate profile:
            {profileText}

            Job posting:
            {jobText}

            Company information:
            {companyText}

            Extra notes from candidate:
            {request.Notes}
            """;

        var content = await CallLlm(prompt, request.Language, request.Tone, "gpt-4o-mini");
        await LogResponse("cv-bullets", request, content);
        return Ok(new AssistantResponse { Content = content });
    }

    [HttpPost("interview-prep")]
    public async Task<IActionResult> InterviewPrep([FromBody] AssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobLink))
            return BadRequest("Job link is required.");

        var (jobText, companyText, profileText) = await GatherContext(request);

        var prompt = $"""
            Create interview preparation notes for this candidate based on the job posting and their profile.

            Include:
            1. Likely interview questions for this role
            2. Suggested answers based on the candidate's real experience
            3. Questions the candidate should ask the interviewer
            4. Key points to emphasize during the interview
            5. Potential weak areas to prepare for
            6. Company-specific talking points based on the company information

            Rules:
            - Only reference the candidate's real experience. Do not invent facts.
            - Write in {request.Language ?? "English"}.
            - Be practical and specific.

            Candidate profile:
            {profileText}

            Job posting:
            {jobText}

            Company information:
            {companyText}

            Extra notes from candidate:
            {request.Notes}
            """;

        var content = await CallLlm(prompt, request.Language, request.Tone, "gpt-4o");
        await LogResponse("interview-prep", request, content);
        return Ok(new AssistantResponse { Content = content });
    }

    private async Task<(string jobText, string companyText, string profileText)> GatherContext(AssistantRequest request)
    {
        var jobTask = !string.IsNullOrWhiteSpace(request.JobLink)
            ? FetchPageText(request.JobLink)
            : Task.FromResult(string.Empty);

        var companyTask = !string.IsNullOrWhiteSpace(request.CompanyLink)
            ? FetchPageText(request.CompanyLink)
            : Task.FromResult(string.Empty);

        var profileTask = LoadProfileText();

        await Task.WhenAll(jobTask, companyTask, profileTask);

        return (jobTask.Result, companyTask.Result, profileTask.Result);
    }

    private static async Task<string> LoadProfileText()
    {
        var sb = new StringBuilder();

        var baseDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));

        var profilePath = Path.Combine(repoRoot, "lauritz.md");
        var cvPath = Path.Combine(repoRoot, "CV.md");

        if (System.IO.File.Exists(profilePath))
        {
            sb.AppendLine("=== Professional Profile ===");
            sb.AppendLine(await System.IO.File.ReadAllTextAsync(profilePath));
            sb.AppendLine();
        }

        if (System.IO.File.Exists(cvPath))
        {
            sb.AppendLine("=== CV ===");
            sb.AppendLine(await System.IO.File.ReadAllTextAsync(cvPath));
        }

        return sb.ToString();
    }

    private static async Task<string> FetchPageText(string url)
    {
        try
        {
            var response = await HttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return string.Empty;

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//noscript");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
            }

            var text = doc.DocumentNode.InnerText;
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            return text.Length > 12000 ? text[..12000] : text;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<string> CallLlm(string prompt, string? language, string? tone, string model = "gpt-4o-mini")
    {
        if (string.IsNullOrEmpty(OpenAiApiKey))
        {
            return "Error: OPENAI_API_KEY environment variable is not set. Please configure it to use the assistant.";
        }

        try
        {
            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $"You are a professional job application assistant. Write in {language ?? "English"}. Use a {tone ?? "professional"} tone. Only use the candidate's real experience. Never invent facts."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.4,
                max_tokens = 3000
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", OpenAiApiKey);

            var response = await HttpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"LLM API error ({response.StatusCode}): {error}";
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var completion = JsonSerializer.Deserialize<LlmChatResponse>(json, options);
            return completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? "No response from LLM.";
        }
        catch (Exception ex)
        {
            return $"Error calling LLM: {ex.Message}";
        }
    }

    private static List<string> ExtractBulletSection(string content, params string[] sectionNames)
    {
        var lines = content.Split('\n');
        var bullets = new List<string>();
        var inSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (sectionNames.Any(name => trimmed.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                inSection = true;
                continue;
            }

            if (inSection && (trimmed.StartsWith("- ") || trimmed.StartsWith("• ")))
            {
                bullets.Add(trimmed.TrimStart('-', '•', ' '));
            }
            else if (inSection && trimmed.Length > 0 && !trimmed.StartsWith("-") && !trimmed.StartsWith("•") && !trimmed.StartsWith("*"))
            {
                if (bullets.Count > 0) break;
            }
        }

        return bullets;
    }

    private async Task LogResponse(string action, AssistantRequest request, string response)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"{timestamp}_{action}.md";
            var filepath = Path.Combine(LogDirectory, filename);

            var logContent = $"""
                # {action} — {DateTime.Now:yyyy-MM-dd HH:mm:ss}

                ## Request
                - **Job Link:** {request.JobLink}
                - **Company Link:** {request.CompanyLink ?? "N/A"}
                - **Language:** {request.Language ?? "English"}
                - **Tone:** {request.Tone ?? "professional"}
                - **Notes:** {request.Notes ?? "N/A"}

                ## Response
                {response}

                ---
                *Generated by Application Assistant*
                """;

            await System.IO.File.WriteAllTextAsync(filepath, logContent);
        }
        catch { }
    }

    private class LlmChatResponse
    {
        public List<LlmChatChoice>? Choices { get; set; }
    }

    private class LlmChatChoice
    {
        public LlmChatMessage? Message { get; set; }
    }

    private class LlmChatMessage
    {
        public string? Content { get; set; }
    }
}

public class AssistantRequest
{
    public required string JobLink { get; set; }
    public string? CompanyLink { get; set; }
    public string? Language { get; set; }
    public string? Tone { get; set; }
    public string? Notes { get; set; }
    public string? Recommendation { get; set; }
}

public class AssistantResponse
{
    public string Content { get; set; } = string.Empty;
    public List<string>? MatchedSkills { get; set; }
    public List<string>? PotentialGaps { get; set; }
}
