namespace ExchangeMail.Core.Services;

public interface IConfigurationService
{
    Task<string> GetDomainAsync();
    Task SetDomainAsync(string domain);
    Task<int> GetPortAsync();
    Task SetPortAsync(int port);
    Task<string> GetSmtpHostAsync();
    Task SetSmtpHostAsync(string host);
    Task<int> GetSmtpPortAsync();
    Task SetSmtpPortAsync(int port);
    Task<string> GetSmtpUsernameAsync();
    Task SetSmtpUsernameAsync(string username);
    Task<string> GetSmtpPasswordAsync();
    Task SetSmtpPasswordAsync(string password);
    Task<bool> GetSmtpEnableSslAsync();
    Task SetSmtpEnableSslAsync(bool enableSsl);
    Task<DateTime?> GetServerHeartbeatAsync();
    Task SetServerHeartbeatAsync(DateTime timestamp);
    Task<bool> GetInternalRoutingEnabledAsync();
    Task SetInternalRoutingEnabledAsync(bool enabled);

    // Summarization Settings
    Task<bool> GetSummarizationEnabledAsync();
    Task SetSummarizationEnabledAsync(bool enabled);
    Task<string> GetSummarizationProviderAsync();
    Task SetSummarizationProviderAsync(string provider);
    Task<string> GetOpenAIApiKeyAsync();
    Task SetOpenAIApiKeyAsync(string apiKey);
    Task<string> GetLocalLlmUrlAsync();
    Task SetLocalLlmUrlAsync(string url);
    Task<string> GetLocalLlmModelNameAsync();
    Task SetLocalLlmModelNameAsync(string modelName);
}
