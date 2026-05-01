using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using System.Net.Http;

namespace JobProcessorApi.Controllers;

[ApiController]
[Route("[controller]")]
public class JobController : ControllerBase
{
    private static readonly string DataFile = Path.Combine(AppContext.BaseDirectory, "job-applications.json");
    private static readonly object FileLock = new();
    private static readonly HttpClient HttpClient = new();
    private static readonly string? OpenAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { Message = "Backend test endpoint is reachable." });
    }

    [HttpGet("list")]
    public IActionResult List()
    {
        var applications = LoadApplications();
        return Ok(applications);
    }

    [HttpPost("process")]
    [HttpPost("add")]
    public async Task<IActionResult> ProcessJob([FromBody] JobApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobLink))
        {
            return BadRequest("Job link is required.");
        }

        if (!Uri.TryCreate(request.JobLink.Trim(), UriKind.Absolute, out var uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest("Job link must be a valid http or https URL.");
        }

        var normalizedJobLink = NormalizeJobLink(uriResult.ToString());
        var extractedData = await ExtractJobDetails(normalizedJobLink);

        lock (FileLock)
        {
            var applications = LoadApplications();
            if (applications.Any(x => string.Equals(NormalizeJobLink(x.JobLink), normalizedJobLink, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict("An application with this job link already exists.");
            }

            var application = new JobApplication
            {
                Id = Guid.NewGuid(),
                JobLink = normalizedJobLink,
                Role = request.Role?.Trim() ?? extractedData.JobTitle ?? string.Empty,
                Company = request.Company?.Trim() ?? extractedData.CompanyName ?? string.Empty,
                Source = request.Source?.Trim() ?? string.Empty,
                Status = request.Status?.Trim() ?? "New",
                NextStep = request.NextStep?.Trim() ?? string.Empty,
                Deadline = request.Deadline ?? extractedData.ApplicationDeadline,
                Notes = request.Notes?.Trim() ?? string.Empty,
                ResumeVersion = request.ResumeVersion?.Trim() ?? string.Empty,
                CoverLetterVersion = request.CoverLetterVersion?.Trim() ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow,
                Domain = uriResult.Host,
                ExtractedJobTitle = extractedData.JobTitle,
                ExtractedCompanyName = extractedData.CompanyName,
                ExtractedCompanyLink = extractedData.CompanyLink,
                ExtractedApplicationDeadline = extractedData.ApplicationDeadline
            };

            applications.Add(application);
            PersistApplications(applications);
            return Ok(new { Message = "Job application saved.", Application = application });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] JobApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobLink))
        {
            return BadRequest("Job link is required.");
        }

        if (!Uri.TryCreate(request.JobLink.Trim(), UriKind.Absolute, out var uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest("Job link must be a valid http or https URL.");
        }

        var normalizedJobLink = NormalizeJobLink(uriResult.ToString());
        var extractedData = await ExtractJobDetails(normalizedJobLink);

        lock (FileLock)
        {
            var applications = LoadApplications();
            var existing = applications.FirstOrDefault(x => x.Id == id);
            if (existing is null)
            {
                return NotFound("Application not found.");
            }

            if (applications.Any(x => x.Id != id && string.Equals(NormalizeJobLink(x.JobLink), normalizedJobLink, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict("Another application with this job link already exists.");
            }

            existing.JobLink = normalizedJobLink;
            existing.Role = request.Role?.Trim() ?? extractedData.JobTitle ?? string.Empty;
            existing.Company = request.Company?.Trim() ?? extractedData.CompanyName ?? string.Empty;
            existing.Source = request.Source?.Trim() ?? string.Empty;
            existing.Status = request.Status?.Trim() ?? "New";
            existing.NextStep = request.NextStep?.Trim() ?? string.Empty;
            existing.Deadline = request.Deadline ?? extractedData.ApplicationDeadline;
            existing.Notes = request.Notes?.Trim() ?? string.Empty;
            existing.ResumeVersion = request.ResumeVersion?.Trim() ?? string.Empty;
            existing.CoverLetterVersion = request.CoverLetterVersion?.Trim() ?? string.Empty;
            existing.ProcessedAtUtc = DateTime.UtcNow;
            existing.Domain = uriResult.Host;
            existing.ExtractedJobTitle = extractedData.JobTitle;
            existing.ExtractedCompanyName = extractedData.CompanyName;
            existing.ExtractedCompanyLink = extractedData.CompanyLink;
            existing.ExtractedApplicationDeadline = extractedData.ApplicationDeadline;

            PersistApplications(applications);
            return Ok(new { Message = "Job application updated.", Application = existing });
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        lock (FileLock)
        {
            var applications = LoadApplications();
            var removed = applications.RemoveAll(x => x.Id == id);
            if (removed == 0)
            {
                return NotFound("Application not found.");
            }

            PersistApplications(applications);
            return Ok(new { Message = "Job application deleted." });
        }
    }

    private List<JobApplication> LoadApplications()
    {
        lock (FileLock)
        {
            if (!System.IO.File.Exists(DataFile))
            {
                return new List<JobApplication>();
            }

            var content = System.IO.File.ReadAllText(DataFile);
            return string.IsNullOrWhiteSpace(content)
                ? new List<JobApplication>()
                : JsonSerializer.Deserialize<List<JobApplication>>(content) ?? new List<JobApplication>();
        }
    }

    private void PersistApplications(List<JobApplication> applications)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var content = JsonSerializer.Serialize(applications, options);
        System.IO.File.WriteAllText(DataFile, content);
    }

    private static string NormalizeJobLink(string link)
    {
        if (!Uri.TryCreate(link.Trim(), UriKind.Absolute, out var uri))
        {
            return link.Trim();
        }

        var normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        if (!string.IsNullOrEmpty(uri.Query))
        {
            normalized += uri.Query;
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            normalized += uri.Fragment;
        }

        return normalized;
    }

    private JobApplication SaveApplication(JobApplication application)
    {
        lock (FileLock)
        {
            var applications = LoadApplications();
            applications.Add(application);
            PersistApplications(applications);
            return application;
        }
    }

    private async Task<ExtractedJobData> ExtractJobDetails(string url)
    {
        try
        {
            var response = await HttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return new ExtractedJobData();
            }

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            if (!string.IsNullOrEmpty(OpenAiApiKey))
            {
                var pageText = ExtractPageText(doc);
                var aiExtracted = await ExtractWithAI(pageText);
                if (aiExtracted != null)
                {
                    return aiExtracted;
                }
            }

            var jobTitle = ExtractJobTitle(doc);
            var companyName = ExtractCompanyName(doc);
            var companyLink = ExtractCompanyLink(doc);
            var applicationDeadline = ExtractApplicationDeadline(doc);

            return new ExtractedJobData
            {
                JobTitle = jobTitle,
                CompanyName = companyName,
                CompanyLink = companyLink,
                ApplicationDeadline = applicationDeadline
            };
        }
        catch
        {
            return new ExtractedJobData();
        }
    }

    private async Task<ExtractedJobData?> ExtractWithAI(string pageText)
    {
        try
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are an expert at extracting job posting details from text." },
                    new { role = "user", content = $"Extract the job title, company name, company website URL, and application deadline from the following text. Return only valid JSON with keys jobTitle, companyName, companyLink, deadline. Use null for missing values. Do not add any extra commentary. Text:\n{pageText}" }
                },
                temperature = 0.0,
                max_tokens = 250
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
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var completion = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(json, options);
            var contentText = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(contentText))
            {
                return null;
            }

            var jsonStart = contentText.IndexOf('{');
            var jsonEnd = contentText.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                return null;
            }

            var extractedJson = contentText[jsonStart..(jsonEnd + 1)];
            var extracted = JsonSerializer.Deserialize<AiExtractedData>(extractedJson, options);
            if (extracted == null)
            {
                return null;
            }

            DateTime? deadline = null;
            if (!string.IsNullOrWhiteSpace(extracted.Deadline) && DateTime.TryParse(extracted.Deadline, out var dt))
            {
                deadline = dt;
            }

            return new ExtractedJobData
            {
                JobTitle = extracted.JobTitle,
                CompanyName = extracted.CompanyName,
                CompanyLink = extracted.CompanyLink,
                ApplicationDeadline = deadline
            };
        }
        catch
        {
            return null;
        }
    }

    private string ExtractPageText(HtmlDocument doc)
    {
        var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//noscript");
        if (nodesToRemove != null)
        {
            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
        }

        var text = doc.DocumentNode.InnerText;
        text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ");
        return text.Length > 12000 ? text[..12000] : text;
    }

    private class OpenAiChatCompletionResponse
    {
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private class OpenAiChoice
    {
        public OpenAiMessage? Message { get; set; }
    }

    private class OpenAiMessage
    {
        public string? Content { get; set; }
    }

    private string? ExtractJobTitle(HtmlDocument doc)
    {
        // Try title tag
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null)
        {
            var title = titleNode.InnerText.Trim();
            // Remove common suffixes
            title = title.Replace(" - LinkedIn", "").Replace(" | Indeed", "").Replace(" - Glassdoor", "");
            return title;
        }

        // Try OpenGraph title
        var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
        if (ogTitle != null)
        {
            return ogTitle.GetAttributeValue("content", "").Trim();
        }

        return null;
    }

    private string? ExtractCompanyName(HtmlDocument doc)
    {
        // Try OpenGraph site name
        var ogSite = doc.DocumentNode.SelectSingleNode("//meta[@property='og:site_name']");
        if (ogSite != null)
        {
            return ogSite.GetAttributeValue("content", "").Trim();
        }

        // Try common selectors (this is heuristic)
        var companyNodes = doc.DocumentNode.SelectNodes("//*[contains(@class, 'company') or contains(@class, 'employer')]");
        if (companyNodes != null)
        {
            foreach (var node in companyNodes)
            {
                var text = node.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(text) && text.Length < 100)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private string? ExtractCompanyLink(HtmlDocument doc)
    {
        // Try OpenGraph URL
        var ogUrl = doc.DocumentNode.SelectSingleNode("//meta[@property='og:url']");
        if (ogUrl != null)
        {
            var url = ogUrl.GetAttributeValue("content", "").Trim();
            if (Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return url;
            }
        }

        // Try canonical link
        var canonical = doc.DocumentNode.SelectSingleNode("//link[@rel='canonical']");
        if (canonical != null)
        {
            var href = canonical.GetAttributeValue("href", "").Trim();
            if (Uri.TryCreate(href, UriKind.Absolute, out _))
            {
                return href;
            }
        }

        return null;
    }

    private DateTime? ExtractApplicationDeadline(HtmlDocument doc)
    {
        // Look for dates in text (heuristic)
        var text = doc.DocumentNode.InnerText;
        var datePatterns = new[] { @"deadline[:\s]*(\d{1,2}/\d{1,2}/\d{4})", @"apply by[:\s]*(\d{1,2}/\d{1,2}/\d{4})", @"closes[:\s]*(\d{1,2}/\d{1,2}/\d{4})" };
        foreach (var pattern in datePatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (DateTime.TryParse(match.Groups[1].Value, out var date))
                {
                    return date;
                }
            }
        }

        return null;
    }
}

public class JobApplicationRequest
{
    public required string JobLink { get; set; }
    public string? Role { get; set; }
    public string? Company { get; set; }
    public string? Source { get; set; }
    public string? Status { get; set; }
    public string? NextStep { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Notes { get; set; }
    public string? ResumeVersion { get; set; }
    public string? CoverLetterVersion { get; set; }
}

public class JobApplication
{
    public Guid Id { get; set; }
    public string JobLink { get; set; } = default!;
    public string Role { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string NextStep { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string ResumeVersion { get; set; } = string.Empty;
    public string CoverLetterVersion { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string? ExtractedJobTitle { get; set; }
    public string? ExtractedCompanyName { get; set; }
    public string? ExtractedCompanyLink { get; set; }
    public DateTime? ExtractedApplicationDeadline { get; set; }
}

public class ExtractedJobData
{
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLink { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
}

public class AiExtractedData
{
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLink { get; set; }
    public string? Deadline { get; set; }
}
