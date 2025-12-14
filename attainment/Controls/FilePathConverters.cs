using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace attainment.Controls
{
    /// <summary>
    /// Returns Visible when the bound string is a non-empty file path; otherwise Collapsed.
    /// </summary>
    public class FilePathToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            return string.IsNullOrWhiteSpace(path) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns a short label based on file extension: PDF, PPT, PPTX, DOCX, etc.
    /// Defaults to FILE when unknown.
    /// </summary>
    public class FilePathToExtensionLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var ext = Path.GetExtension(path)?.ToLowerInvariant() ?? string.Empty;
            return ext switch
            {
                ".pdf" => "PDF",
                ".ppt" => "PPT",
                ".pptx" => "PPTX",
                ".doc" => "DOC",
                ".docx" => "DOCX",
                ".xlsx" => "XLSX",
                ".xls" => "XLS",
                _ => (ext.StartsWith('.') && ext.Length > 1) ? ext[1..].ToUpperInvariant() : "FILE"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns a Brush based on the file type for visual cue.
    /// </summary>
    public class FilePathToBrushConverter : IValueConverter
    {
        private static readonly Brush PdfBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e53e3e"));
        private static readonly Brush PptBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ed8936"));
        private static readonly Brush DefaultBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path)) return DefaultBrush;
            var ext = Path.GetExtension(path)?.ToLowerInvariant() ?? string.Empty;
            return ext switch
            {
                ".pdf" => PdfBrush,
                ".ppt" => PptBrush,
                ".pptx" => PptBrush,
                _ => DefaultBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns true only if the bound string is a path to an existing file with .pdf extension.
    /// Use to enable/disable actions that require an actual PDF file.
    /// </summary>
    public class FilePathToExistingPdfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                if (!File.Exists(path)) return false;
                var ext = Path.GetExtension(path);
                return string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
