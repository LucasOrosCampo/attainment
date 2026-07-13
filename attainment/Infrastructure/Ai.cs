using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using attainment.Application;
using attainment.Models;
using Microsoft.Extensions.Logging;

namespace attainment.Infrastructure;

public interface IAi
{
    Task<string> PromptAsync(
        string message,
        FileInfo? file = null,
        CancellationToken cancellationToken = default);
}

public sealed class AiRequestException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
    : InvalidOperationException(message, innerException)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}

public sealed class OpenAi(
    HttpClient httpClient,
    ISettingsService settingsService,
    ILogger<OpenAi> logger) : IAi
{
    internal const long MaximumFileSize = 50L * 1024L * 1024L;

    public async Task<string> PromptAsync(
        string message,
        FileInfo? file = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A prompt is required.", nameof(message));
        }

        var apiKey = await settingsService.GetValueAsync(SettingKeys.OpenAIKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiRequestException("OpenAI API key is not configured. Add it in Settings.");
        }

        var model = await settingsService.GetValueAsync(SettingKeys.OpenAIModel, cancellationToken);
        if (string.IsNullOrWhiteSpace(model))
        {
            model = SettingKeys.DefaultValue(SettingKeys.OpenAIModel)!;
        }

        var content = new List<object>();
        if (file is not null)
        {
            content.Add(await CreateFileContentAsync(file, cancellationToken));
        }

        content.Add(new { type = "input_text", text = message });
        var payload = new
        {
            model,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = content.ToArray()
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var requestId = response.Headers.TryGetValues("x-request-id", out var values)
                    ? values.FirstOrDefault()
                    : null;
                logger.LogWarning(
                    "OpenAI request failed with status {StatusCode} and request ID {RequestId}",
                    response.StatusCode,
                    requestId);
                throw new AiRequestException(
                    $"OpenAI rejected the request ({(int)response.StatusCode})." +
                    (requestId is null ? string.Empty : $" Request ID: {requestId}"),
                    response.StatusCode);
            }

            return ExtractOutputText(responseBody);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiRequestException("The OpenAI request timed out.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenAI request failed before receiving a response");
            throw new AiRequestException("OpenAI could not be reached. Check the network connection.", null, ex);
        }
    }

    internal static string ExtractOutputText(string responseBody)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(responseBody);
        }
        catch (JsonException ex)
        {
            throw new AiRequestException("OpenAI returned an unreadable response.", null, ex);
        }

        using (document)
        {
        if (!document.RootElement.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            throw new AiRequestException("OpenAI returned an unexpected response.");
        }

        var parts = new List<string>();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                    contentItem.TryGetProperty("text", out var text) && text.GetString() is { } value)
                {
                    parts.Add(value);
                }
            }
        }

            return parts.Count > 0
                ? string.Join(Environment.NewLine, parts)
                : throw new AiRequestException("OpenAI returned no text output.");
        }
    }

    private static async Task<object> CreateFileContentAsync(
        FileInfo file,
        CancellationToken cancellationToken)
    {
        file.Refresh();
        if (!file.Exists)
        {
            throw new FileNotFoundException("The source file no longer exists.", file.FullName);
        }

        if (file.Length > MaximumFileSize)
        {
            throw new AiRequestException("The source file exceeds OpenAI's 50 MB request limit.");
        }

        var bytes = await File.ReadAllBytesAsync(file.FullName, cancellationToken);
        var mimeType = GetMimeType(file.Extension);
        return new
        {
            type = "input_file",
            filename = file.Name,
            file_data = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}"
        };
    }

    private static string GetMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
}
