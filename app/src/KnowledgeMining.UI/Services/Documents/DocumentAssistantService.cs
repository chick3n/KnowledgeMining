﻿using KnowledgeMining.Application.Common.Options;
using KnowledgeMining.UI.Models;
using Microsoft.Extensions.Options;

namespace KnowledgeMining.UI.Services.Documents
{
    public class DocumentAssistantService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AssistantOptions _assistantOptions;
        private readonly ILogger _logger;

        public DocumentAssistantService(IHttpClientFactory httpClientFactory, IOptions<AssistantOptions> assistantOptions, ILogger<DocumentAssistantService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _assistantOptions = assistantOptions.Value;
            _logger = logger;
        }

        public bool CanAssist() => _assistantOptions != null && _assistantOptions.BaseUri != null;

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
                var response = await client.PostAsJsonAsync("prompt", request);

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