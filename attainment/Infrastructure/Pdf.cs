using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace attainment.Infrastructure;

public interface IPdf
{
    public string Convert(FileInfo file);
}

public class Pdf: IPdf
{
    public string Convert(FileInfo file)
    {
        if (!file.Exists)
            throw new FileNotFoundException($"PDF file not found: {file.FullName}");

        var text = new StringBuilder();

        using (var document = PdfDocument.Open(file.FullName))
        {
            foreach (var page in document.GetPages())
            {
                text.AppendLine(page.Text);
            }
        }

        return text.ToString();
    }
}