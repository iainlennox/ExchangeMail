using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteConfigurationService : IConfigurationService
{
    private readonly ExchangeMailContext _context;
    private const string DomainKey = "Domain";

    public SqliteConfigurationService(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task<string> GetDomainAsync()
    {
        var config = await _context.Configurations.FindAsync(DomainKey);
        return config?.Value ?? "localhost";
    }

    public async Task SetDomainAsync(string domain)
    {
        var config = await _context.Configurations.FindAsync(DomainKey);
        if (config == null)
        {
            _context.Configurations.Add(new ConfigEntity { Key = DomainKey, Value = domain });
        }
        else
        {
            config.Value = domain;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetPortAsync()
    {
        var config = await _context.Configurations.FindAsync("SmtpPort");
        if (config != null && int.TryParse(config.Value, out int port))
        {
            return port;
        }
        return 2525; // Default
    }

    public async Task SetPortAsync(int port)
    {
        await SetValueAsync("SmtpPort", port.ToString());
    }

    public async Task<string> GetSmtpHostAsync() => await GetValueAsync("SmtpHost") ?? "";
    public async Task SetSmtpHostAsync(string host) => await SetValueAsync("SmtpHost", host);

    public async Task<int> GetSmtpPortAsync()
    {
        var config = await _context.Configurations.FindAsync("SmtpUpstreamPort");
        if (config != null && int.TryParse(config.Value, out int port))
        {
            return port;
        }
        return 587; // Default
    }
    public async Task SetSmtpPortAsync(int port) => await SetValueAsync("SmtpUpstreamPort", port.ToString());

    public async Task<string> GetSmtpUsernameAsync() => await GetValueAsync("SmtpUsername") ?? "";
    public async Task SetSmtpUsernameAsync(string username) => await SetValueAsync("SmtpUsername", username);

    public async Task<string> GetSmtpPasswordAsync() => await GetValueAsync("SmtpPassword") ?? "";
    public async Task SetSmtpPasswordAsync(string password) => await SetValueAsync("SmtpPassword", password);

    public async Task<bool> GetSmtpEnableSslAsync()
    {
        var val = await GetValueAsync("SmtpEnableSsl");
        return val == "True";
    }
    public async Task SetSmtpEnableSslAsync(bool enableSsl) => await SetValueAsync("SmtpEnableSsl", enableSsl.ToString());

    public async Task<DateTime?> GetServerHeartbeatAsync()
    {
        var val = await GetValueAsync("ServerHeartbeat");
        if (DateTime.TryParse(val, out DateTime heartbeat))
        {
            return heartbeat;
        }
        return null;
    }

    public async Task SetServerHeartbeatAsync(DateTime timestamp)
    {
        await SetValueAsync("ServerHeartbeat", timestamp.ToString("o"));
    }

    public async Task<bool> GetInternalRoutingEnabledAsync()
    {
        var val = await GetValueAsync("InternalRoutingEnabled");
        // Default to true if not set
        return val == null || val == "True";
    }

    public async Task SetInternalRoutingEnabledAsync(bool enabled)
    {
        await SetValueAsync("InternalRoutingEnabled", enabled.ToString());
    }

    // Summarization Settings
    public async Task<bool> GetSummarizationEnabledAsync()
    {
        var val = await GetValueAsync("SummarizationEnabled");
        return val == "True";
    }
    public async Task SetSummarizationEnabledAsync(bool enabled) => await SetValueAsync("SummarizationEnabled", enabled.ToString());

    public async Task<string> GetSummarizationProviderAsync() => await GetValueAsync("SummarizationProvider") ?? "OpenAI";
    public async Task SetSummarizationProviderAsync(string provider) => await SetValueAsync("SummarizationProvider", provider);

    public async Task<string> GetOpenAIApiKeyAsync() => await GetValueAsync("OpenAIApiKey") ?? "";
    public async Task SetOpenAIApiKeyAsync(string apiKey) => await SetValueAsync("OpenAIApiKey", apiKey);

    public async Task<string> GetLocalLlmUrlAsync() => await GetValueAsync("LocalLlmUrl") ?? "http://localhost:1234/v1/chat/completions";
    public async Task SetLocalLlmUrlAsync(string url) => await SetValueAsync("LocalLlmUrl", url);

    public async Task<string> GetLocalLlmModelNameAsync() => await GetValueAsync("LocalLlmModelName") ?? "local-model";
    public async Task SetLocalLlmModelNameAsync(string modelName) => await SetValueAsync("LocalLlmModelName", modelName);

    private async Task<string?> GetValueAsync(string key)
    {
        var config = await _context.Configurations.FindAsync(key);
        return config?.Value;
    }

    private async Task SetValueAsync(string key, string value)
    {
        var config = await _context.Configurations.FindAsync(key);
        if (config == null)
        {
            _context.Configurations.Add(new ConfigEntity { Key = key, Value = value });
        }
        else
        {
            config.Value = value;
        }
        await _context.SaveChangesAsync();
    }
}
