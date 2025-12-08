using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;

namespace ExchangeMail.Core.Services;

public class EmailSummarizerService : IEmailSummarizerService
{
    private readonly IConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailSummarizerService(IConfigurationService configService, IHttpClientFactory httpClientFactory)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> SummarizeAsync(string content)
    {
        bool enabled = await _configService.GetSummarizationEnabledAsync();
        if (!enabled)
        {
            return "Summarization is disabled by the administrator.";
        }

        string provider = await _configService.GetSummarizationProviderAsync();
        string prompt = $"Please summarize the following email briefly:\n\n{content}";

        if (provider == "OpenAI")
        {
            return await SummarizeWithOpenAI(prompt);
        }
        else
        {
            return await SummarizeWithLocalLLM(prompt);
        }
    }

    private async Task<string> SummarizeWithOpenAI(string prompt)
    {
        var apiKey = await _configService.GetOpenAIApiKeyAsync();
        if (string.IsNullOrEmpty(apiKey))
        {
            return "OpenAI API Key is not configured.";
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // Standard OpenAI Chat Completion request
        var requestBody = new
        {
            model = "gpt-3.5-turbo", // or configurable? For now hardcode or use model setting if applicable
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 150
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
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No summary returned.";
        }
        catch (Exception ex)
        {
            return $"Error calling OpenAI: {ex.Message}";
        }
    }

    private async Task<string> SummarizeWithLocalLLM(string prompt)
    {
        var url = await _configService.GetLocalLlmUrlAsync();
        var modelName = await _configService.GetLocalLlmModelNameAsync();

        var client = _httpClientFactory.CreateClient();

        // LM Studio often supports the OpenAI Chat Completion format at /v1/chat/completions
        var requestBody = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = -1, // LM Studio specific for no limit, or standard int
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
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No summary returned.";
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
