using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.UI.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnowledgeMining.UI.Services.Documents
{
    public class DocumentAssistantService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AssistantOptions _assistantOptions;
        private readonly ILogger _logger;

        private bool? _online = null;

        public DocumentAssistantService(IHttpClientFactory httpClientFactory, IOptions<AssistantOptions> assistantOptions, ILogger<DocumentAssistantService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _assistantOptions = assistantOptions.Value;
            _logger = logger;
        }

        public bool IsEnabled => _assistantOptions != null && _assistantOptions.Enabled;

        public bool CanAssist() => _assistantOptions != null && _assistantOptions.BaseUri != null && IsEnabled;

        public async Task<bool> IsOnline()
        {
            if (_online != null)
                return _online.Value;

            if (!CanAssist())
            {
                _online = false;
                return _online.Value;
            }

            var client = _httpClientFactory.CreateClient(AssistantOptions.Name);
            client.BaseAddress = new Uri(_assistantOptions!.BaseUri!);

            try
            {
                var response = await client.GetAsync("health");

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _online = true;
                    return _online.Value;
                }
            }
            catch (Exception)
            {
            }

            _online = false;
            return _online.Value;
        }

        public async Task<string?> SimplePrompt(string query, string content, string identifier = null)
        {
            if (string.IsNullOrEmpty(query))
                return null;

            if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(identifier))
                return null;

            var payload = new GptIndexerPromptRequest();
            payload.Name = identifier;
            payload.Input = query;
            payload.Documents.Add(content.Trim());

            var response = await Prompt(payload);

            if (response != null && !string.IsNullOrEmpty(response?.solution?.response ?? string.Empty))
            {
                return response!.solution!.response!;
            }

            return null;
        }

        private async Task<GptIndexerPromptResponse?> Prompt(GptIndexerPromptRequest request)
        {
            if (!CanAssist())
                return null;

            var client = _httpClientFactory.CreateClient(AssistantOptions.Name);
            client.BaseAddress = new Uri(_assistantOptions!.BaseUri!);

            try
            {
                //var response = await client.PostAsJsonAsync("prompt", request);
                var content = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var response = await client.PostAsync("prompt", new StringContent(content, System.Text.Encoding.UTF8, "application/json"));

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await response.Content.ReadFromJsonAsync<GptIndexerPromptResponse>();
                }
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex, "Assistant failed with {payload}", request);
            }

            return null;
        }
    }
}
