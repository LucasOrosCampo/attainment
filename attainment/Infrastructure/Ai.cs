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
    string Prompt(string message);
}

public class OpenAi(IDbContextFactory<ApplicationDbContext> dbFactory) : IAi
{
    private static readonly HttpClient _httpClient = new();
    private string? _apiKey;

    public string Prompt(string message)
    {
        _apiKey ??= dbFactory.CreateDbContext().Settings.FirstOrDefault(s => s.Key == KEYS.OpenAIKey)?.Value;

        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException(
                "OpenAI API key is required but not configured. Please set the API key in settings.");

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = message }
            }
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var response = _httpClient.Send(request);
            response.EnsureSuccessStatusCode();

            using var reader = new StreamReader(response.Content.ReadAsStream());
            var responseBody = reader.ReadToEnd();

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            return jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ??
                   "";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("OpenAI completion request failed. Please check your API key and network connection.", ex);
        }
    }
}