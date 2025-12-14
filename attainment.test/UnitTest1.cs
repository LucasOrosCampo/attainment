using attainment.Infrastructure;
using attainment.Models;
using Xunit;

namespace attainment.test;

public class UnitTest1
{
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
                { "Number": 2, "Content": "4" }
              ],
              "CorrectOption": 2,
              "Explanation": "2 + 2 = 4."
            }
          ]
        }
        """;

        Exam exam = ExamRepository.Parse(json);

        Assert.NotNull(exam);
        Assert.NotNull(exam.Questions);
        Assert.Single(exam.Questions);

        var q = exam.Questions[0];
        Assert.Equal("What is 2 + 2?", q.Content);
        Assert.Equal(2, q.Options.Length);
        Assert.Equal(2, q.CorrectOption);
        Assert.Equal("2 + 2 = 4.", q.Explanation);

        Assert.Equal(1, q.Options[0].Number);
        Assert.Equal("3", q.Options[0].Content);
        Assert.Equal(2, q.Options[1].Number);
        Assert.Equal("4", q.Options[1].Content);
    }
}