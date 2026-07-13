using System.Text.Json;
using attainment.Models;

namespace attainment.Infrastructure;

public sealed class ExamValidationException(IReadOnlyList<string> errors)
    : InvalidOperationException(string.Join(Environment.NewLine, errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

public sealed class ExamRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Exam Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ExamValidationException(["The exam response is empty."]);
        }

        Exam? exam;
        try
        {
            exam = JsonSerializer.Deserialize<Exam>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ExamValidationException([$"The response is not valid JSON: {ex.Message}"]);
        }

        if (exam is null)
        {
            throw new ExamValidationException(["The response does not contain an exam."]);
        }

        var errors = Validate(exam);
        return errors.Count == 0 ? exam : throw new ExamValidationException(errors);
    }

    internal static IReadOnlyList<string> Validate(Exam exam)
    {
        var errors = new List<string>();
        if (exam.Questions is not { Length: > 0 })
        {
            return ["The exam must contain at least one question."];
        }

        if (exam.Questions.Length > 100)
        {
            errors.Add("The exam cannot contain more than 100 questions.");
        }

        for (var questionIndex = 0; questionIndex < exam.Questions.Length; questionIndex++)
        {
            var question = exam.Questions[questionIndex];
            var label = $"Question {questionIndex + 1}";
            if (question is null)
            {
                errors.Add($"{label} is missing.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(question.Content))
            {
                errors.Add($"{label} has no content.");
            }

            if (question.Options is not { Length: >= 3 and <= 6 })
            {
                errors.Add($"{label} must contain between 3 and 6 options.");
                continue;
            }

            var expectedNumbers = Enumerable.Range(1, question.Options.Length).ToArray();
            var actualNumbers = question.Options.Select(option => option?.Number ?? 0).ToArray();
            if (!actualNumbers.SequenceEqual(expectedNumbers))
            {
                errors.Add($"{label} option numbers must be sequential and start at 1.");
            }

            if (question.Options.Any(option => option is null || string.IsNullOrWhiteSpace(option.Content)))
            {
                errors.Add($"{label} contains an empty option.");
            }

            if (!actualNumbers.Contains(question.CorrectOption))
            {
                errors.Add($"{label} references an unknown correct option.");
            }

            if (string.IsNullOrWhiteSpace(question.Explanation))
            {
                errors.Add($"{label} has no explanation.");
            }
        }

        return errors;
    }
}
