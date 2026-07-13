using System.Net;
using System.Text;
using System.Text.Json;
using attainment.Application;
using attainment.Infrastructure;
using attainment.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace attainment.test;

public sealed class OpenAiTests
{
    [Fact]
    public async Task PromptAsync_UsesResponsesEndpointAndExtractsOutputText()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, SuccessfulResponse("exam-json"));
        var client = CreateClient(handler);

        var result = await client.PromptAsync("Generate an exam");

        Assert.Equal("exam-json", result);
        Assert.Equal("https://api.openai.com/v1/responses", handler.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        using var request = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("gpt-4o-mini", request.RootElement.GetProperty("model").GetString());
        var content = request.RootElement.GetProperty("input")[0].GetProperty("content");
        Assert.Single(content.EnumerateArray());
        Assert.Equal("input_text", content[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task PromptAsync_WithPdf_SendsResponsesFileDataUri()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(path, [1, 2, 3, 4]);
        try
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, SuccessfulResponse("{}"));
            var client = CreateClient(handler);

            await client.PromptAsync("Generate an exam", new FileInfo(path));

            using var request = JsonDocument.Parse(handler.RequestBody!);
            var content = request.RootElement.GetProperty("input")[0].GetProperty("content");
            Assert.Equal("input_file", content[0].GetProperty("type").GetString());
            Assert.Equal(Path.GetFileName(path), content[0].GetProperty("filename").GetString());
            Assert.StartsWith("data:application/pdf;base64,", content[0].GetProperty("file_data").GetString());
            Assert.Equal("input_text", content[1].GetProperty("type").GetString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task PromptAsync_WhenProviderRejectsRequest_ReturnsStableError()
    {
        var handler = new RecordingHandler(HttpStatusCode.BadRequest, "{\"error\":{\"message\":\"sensitive detail\"}}", "request-123");
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AiRequestException>(
            () => client.PromptAsync("Generate an exam"));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("request-123", exception.Message);
        Assert.DoesNotContain("sensitive detail", exception.Message);
    }

    private static OpenAi CreateClient(RecordingHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };
        return new OpenAi(httpClient, new StubSettingsService(), NullLogger<OpenAi>.Instance);
    }

    private static string SuccessfulResponse(string text) => $$"""
    {
      "output": [
        {
          "type": "message",
          "content": [
            { "type": "output_text", "text": {{JsonSerializer.Serialize(text)}} }
          ]
        }
      ]
    }
    """;

    private sealed class RecordingHandler(
        HttpStatusCode statusCode,
        string responseBody,
        string? requestId = null) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }
        public string? AuthorizationScheme { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
            if (requestId is not null)
            {
                response.Headers.Add("x-request-id", requestId);
            }

            return response;
        }
    }

    private sealed class StubSettingsService : ISettingsService
    {
        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
        {
            var value = key switch
            {
                SettingKeys.OpenAIKey => "test-api-key",
                SettingKeys.OpenAIModel => "gpt-4o-mini",
                _ => null
            };
            return Task.FromResult(value);
        }

        public Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ProtectSecretsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
