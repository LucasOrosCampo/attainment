using attainment.Infrastructure;

namespace attainment.test;

public sealed class ExamRepositoryTests
{
    private readonly ExamRepository _repository = new();

    [Fact]
    public void Parse_ValidExamJson_ReturnsExamWithExpectedData()
    {
        var json = """
        {
          "Questions": [
            {
              "Content": "What is 2 + 2?",
              "Options": [
                { "Number": 1, "Content": "3" },
                { "Number": 2, "Content": "4" },
                { "Number": 3, "Content": "5" }
              ],
              "CorrectOption": 2,
              "Explanation": "2 + 2 = 4."
            }
          ]
        }
        """;

        var exam = _repository.Parse(json);

        var question = Assert.Single(exam.Questions);
        Assert.Equal("What is 2 + 2?", question.Content);
        Assert.Equal(3, question.Options.Length);
        Assert.Equal(2, question.CorrectOption);
        Assert.Equal("2 + 2 = 4.", question.Explanation);
    }

    [Theory]
    [InlineData("{}", "at least one question")]
    [InlineData("{\"Questions\":[]}", "at least one question")]
    [InlineData("{not-json}", "not valid JSON")]
    public void Parse_InvalidDocument_ThrowsValidationError(string json, string expectedMessage)
    {
        var exception = Assert.Throws<ExamValidationException>(() => _repository.Parse(json));
        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_InvalidQuestionInvariants_ReportsEveryRelevantError()
    {
        var json = """
        {
          "Questions": [
            {
              "Content": "",
              "Options": [
                { "Number": 2, "Content": "" },
                { "Number": 2, "Content": "B" },
                { "Number": 4, "Content": "C" }
              ],
              "CorrectOption": 9,
              "Explanation": ""
            }
          ]
        }
        """;

        var exception = Assert.Throws<ExamValidationException>(() => _repository.Parse(json));

        Assert.Contains("no content", exception.Message);
        Assert.Contains("sequential", exception.Message);
        Assert.Contains("empty option", exception.Message);
        Assert.Contains("unknown correct option", exception.Message);
        Assert.Contains("no explanation", exception.Message);
    }
}
