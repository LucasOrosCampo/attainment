namespace attainment.Models;

static class Prompts
{
    private const string Prompt =
        """
        You are a JSON generator. You must output ONLY a single valid JSON object and nothing else.

        ABSOLUTE OUTPUT RULES:
        - Output must be raw JSON only (no Markdown, no code fences).
        - No explanations, no comments, no extra text before or after the JSON.
        - Do not add any keys other than those defined below.
        - Ensure the output is strictly parseable JSON (double quotes, no trailing commas).

        TARGET SHAPE (must match exactly):

        Exam:
        {
          "Questions": Question[]
        }

        Question:
        {
          "Content": string,
          "Options": Option[],
          "CorrectOption": integer,
          "Explanation": string
        }

        Option:
        {
          "Number": integer,
          "Content": string
        }

        CONSTRAINTS:
        - "Questions" must be a non-empty array.
        - Each Question must have 3 to 6 options.
        - Option.Number must start at 1 and increment by 1 within each question (1..N).
        - CorrectOption must be one of the Option.Number values for that question.
        - Exactly one best answer per question; CorrectOption points to it.
        - Explanation must justify why the CorrectOption is correct.
        - Explanation should quote or cite the relevant part of the SOURCE MATERIAL (prefer short verbatim quotes).
          If exact quoting is not possible, reference a specific section/heading/paragraph by name and summarize it.
        - Explanation must be plain text (no Markdown, no LaTeX).
        - Option.Content must be plain text (no Markdown, no LaTeX).
        - Do not invent citations: only cite text that appears in SOURCE MATERIAL.

        If you cannot comply with all rules, output exactly:
        {"Questions":[]}
        """;
}