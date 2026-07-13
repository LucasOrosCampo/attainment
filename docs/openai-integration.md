# OpenAI Integration

## Purpose

Attainment sends source material and an exam-generation prompt to the OpenAI API when the user explicitly selects **Ask AI**. The response is validated as an exam before it is previewed or exported.

## Configuration

The Settings page manages two values:

- `openai.key`: the API key. It is encrypted with Windows DPAPI for the current user before SQLite storage.
- `openai.model`: the Responses API model. The default is `gpt-4o-mini` and can be changed without recompiling.

Existing plaintext keys are protected during application startup. Logs and user-facing errors never include the key or complete provider response bodies.

## API Contract

The client uses `POST https://api.openai.com/v1/responses`.

Text mode extracts PDF text locally with PdfPig and sends it as an `input_text` item. Direct-file mode sends the PDF as an `input_file` item using a data URI:

```json
{
  "model": "gpt-4o-mini",
  "input": [
    {
      "role": "user",
      "content": [
        {
          "type": "input_file",
          "filename": "source.pdf",
          "file_data": "data:application/pdf;base64,..."
        },
        {
          "type": "input_text",
          "text": "Generate the exam..."
        }
      ]
    }
  ]
}
```

Direct-file mode preserves PDF page images and layout, but may consume more input tokens. Attainment enforces the API's 50 MB combined file limit before allocating the Base64 request body.

Official references:

- [OpenAI file inputs guide](https://developers.openai.com/api/docs/guides/file-inputs)
- [OpenAI models](https://developers.openai.com/api/docs/models)

## Reliability

- Requests use an injected `HttpClient` with a three-minute timeout.
- Generation is asynchronous and can be cancelled from the UI.
- HTTP errors are translated into stable application errors and retain the provider request ID when available.
- Tests use a fake `HttpMessageHandler`; they never call OpenAI.

## Exam Validation

The response must be valid JSON and satisfy all domain invariants:

- At least one and no more than 100 questions.
- Every question has content and an explanation.
- Every question has between three and six non-empty options.
- Option numbers are sequential and start at one.
- `CorrectOption` points to an existing option.

Invalid output remains visible for diagnosis but cannot be previewed as a valid exam or exported.

## Privacy Checklist

- Send a document only after the user starts generation.
- Do not add document contents, model responses or keys to application logs.
- Explain to distributed users that direct-file and extracted-text modes transmit study material to OpenAI.
- Review OpenAI data controls and organizational policy before using sensitive material.
