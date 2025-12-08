using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;

namespace ExchangeMail.Core.Services;

public class AiEmailService : IAiEmailService
{
    private readonly IConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiEmailService(IConfigurationService configService, IHttpClientFactory httpClientFactory)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> SummarizeAsync(string content)
    {
        bool enabled = await _configService.GetSummarizationEnabledAsync();
        if (!enabled)
        {
            return "AI features are disabled by the administrator.";
        }

        string provider = await _configService.GetSummarizationProviderAsync();
        string prompt = $"Please summarize the following email. If there are any actionable tasks or deadlines, list them under a header '<h3>Action Items</h3>'. Format the output as HTML using <div>, <ul>, <li>, and <strong> tags. Do not use markdown backticks.\n\n{content}";

        return await CallLlmAsync(provider, prompt);
    }

    public async Task<string> GenerateDraftAsync(string prompt)
    {
        bool enabled = await _configService.GetSummarizationEnabledAsync(); // Reusing the same enable switch for now? Or should I add a new one? Assuming 'SummarizationEnabled' covers general AI features or I should stick to that naming or rename config?
        // Let's assume 'SummarizationEnabled' enables the AI features generally for now to avoid DB migration complexity unless requested.
        // But better to check if I should add a specific one. The previous plan didn't specify a new config. 
        // I will use IsSummarizationEnabled as a proxy for "AI Enabled" but I should probably rename the method in IConfig to be generic later if I wanted to be perfect.
        // For now, let's just check the same flag.
        if (!enabled)
        {
            return "AI features are disabled by the administrator.";
        }

        string provider = await _configService.GetSummarizationProviderAsync();
        string systemPrompt = "You are a helpful email assistant. Write a professional email based on the user's prompt. Do not include subject lines or placeholders unless necessary. Just write the body.";
        string fullPrompt = $"{systemPrompt}\n\nUser Prompt: {prompt}";

        return await CallLlmAsync(provider, fullPrompt);
    }

    public async Task<string> GetLabelsAsync(string content)
    {
        bool enabled = await _configService.GetSummarizationEnabledAsync();
        if (!enabled)
        {
            return string.Empty;
        }

        string provider = await _configService.GetSummarizationProviderAsync();
        string prompt = $@"
Analyze the following email and provide a comma-separated list of labels.
1. Categorization Keywords: Work, Personal, Finance, Urgent, Marketing, Social, Updates.
2. Security Keywords: Suspicious, Fraud, Phishing.
   - Mark as 'Suspicious' if the email asks for passwords, bank details, or seems unusual.
   - Mark as 'Fraud' or 'Phishing' if there are clear indicators of scam.
   
Return ONLY the comma-separated labels. Do not include any other text.

Email Content:
{content}";

        return await CallLlmAsync(provider, prompt);
    }

    public async Task<string> GenerateDailyBriefingAsync(string contextData, string timeOfDay)
    {
        bool enabled = await _configService.GetSummarizationEnabledAsync();
        if (!enabled)
        {
            return "AI features are disabled.";
        }

        string provider = await _configService.GetSummarizationProviderAsync();
        string prompt = $@"
You are a helpful personal assistant. Provide a {timeOfDay} briefing based on the following context (Events, Tasks, Important Emails).
Your specific goal is to summarize this information into a concise, friendly, and motivating 1-2 paragraph overview.
Highlight the most critical items.
Format the output as HTML paragraphs (<p>). Do not use headers or lists, just narrative text.

Context:
{contextData}";

        return await CallLlmAsync(provider, prompt);
    }

    private async Task<string> CallLlmAsync(string provider, string prompt)
    {
        if (provider == "OpenAI")
        {
            return await CallOpenAI(prompt);
        }
        else
        {
            return await CallLocalLLM(prompt);
        }
    }

    private async Task<string> CallOpenAI(string prompt)
    {
        var apiKey = await _configService.GetOpenAIApiKeyAsync();
        if (string.IsNullOrEmpty(apiKey))
        {
            return "OpenAI API Key is not configured.";
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 500
        };

        try
        {
            var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"Error from OpenAI: {response.StatusCode} - {error}";
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response returned.";
        }
        catch (Exception ex)
        {
            return $"Error calling OpenAI: {ex.Message}";
        }
    }

    private async Task<string> CallLocalLLM(string prompt)
    {
        var url = await _configService.GetLocalLlmUrlAsync();
        var modelName = await _configService.GetLocalLlmModelNameAsync();

        var client = _httpClientFactory.CreateClient();

        var requestBody = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = -1,
            stream = false
        };

        try
        {
            var response = await client.PostAsJsonAsync(url, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"Error from Local LLM: {response.StatusCode} - {error}";
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response returned.";
        }
        catch (Exception ex)
        {
            return $"Error calling Local LLM at {url}: {ex.Message}";
        }
    }

    private class OpenAIChatResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
