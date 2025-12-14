using System.IO;
using attainment.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace attainment.Infrastructure;

public interface IAi
{
    // When base64file is provided, the HTTP request body should include the file
    // rather than appending its text to the prompt. Filename is optional.
    string Prompt(string message, string? base64file = null, string? filename = null);
}

public class OpenAi(IDbContextFactory<ApplicationDbContext> dbFactory) : IAi
{
    private static readonly HttpClient _httpClient = new();
    private string? _apiKey;

    public string Prompt(string message, string? base64file = null, string? filename = null)
    {
        _apiKey ??= dbFactory.CreateDbContext().Settings.FirstOrDefault(s => s.Key == KEYS.OpenAIKey)?.Value;

        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException(
                "OpenAI API key is required but not configured. Please set the API key in settings.");

        // Build request body depending on whether a base64-encoded file is supplied
        string jsonContent;
        HttpRequestMessage request;
        if (!string.IsNullOrEmpty(base64file))
        {
            // Build the structured payload that includes the file and the input text
            var filePayload = new
            {
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_file", filename = string.IsNullOrWhiteSpace(filename) ? "file.bin" : filename, file_data = base64file },
                            new { type = "input_text", text = message }
                        }
                    }
                }
            };

            jsonContent = JsonSerializer.Serialize(filePayload);
            var body = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            // Note: Endpoint kept as-is; adjust if your backend uses a different URL for file-enabled payloads
            request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = body
            };
        }
        else
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = message }
                }
            };

            jsonContent = JsonSerializer.Serialize(requestBody);
            var body = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = body
            };
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var response = _httpClient.Send(request);
            response.EnsureSuccessStatusCode();

            using var reader = new StreamReader(response.Content.ReadAsStream());
            var responseBody = reader.ReadToEnd();

            // Try to parse as OpenAI chat completions; if it fails, return raw body
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                if (jsonResponse.ValueKind == JsonValueKind.Object &&
                    jsonResponse.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
                {
                    var content = choices[0].GetProperty("message").GetProperty("content").GetString();
                    return content ?? string.Empty;
                }

                // Fallback: if there's an "output" or similar key, try to return it; else raw
                if (jsonResponse.TryGetProperty("output", out var outputEl))
                {
                    return outputEl.ToString();
                }

                return responseBody;
            }
            catch
            {
                return responseBody;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("OpenAI completion request failed. Please check your API key and network connection.", ex);
        }
    }
}