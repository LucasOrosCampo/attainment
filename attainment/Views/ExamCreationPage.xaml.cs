using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using attainment.ViewModels;

namespace attainment.Views
{
    public partial class ExamCreationPage : Page
    {
        private readonly ExamCreationViewModel _vm;
        public ExamCreationViewModel ViewModel => _vm;

        public ExamCreationPage(ExamCreationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
        }

        private async void AskAiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.IsLoading = true;
                
                var prompt = _vm.PromptText ?? string.Empty;

                // If UseBase64 is enabled and we have a selected resource, pass the file via IAi
                if (_vm.UseBase64 && _vm.Resource?.FilePath is string filePath && System.IO.File.Exists(filePath))
                {
                    var fileName = System.IO.Path.GetFileName(filePath);
                    var bytes = await Task.Run(() => System.IO.File.ReadAllBytes(filePath));
                    var b64 = Convert.ToBase64String(bytes);
                    var resultWithFile = await Task.Run(() => _vm.Ai.Prompt(prompt, b64, fileName));
                    _vm.ResultText = resultWithFile ?? string.Empty;
                }
                else
                {
                    var resultNoFile = await Task.Run(() => _vm.Ai.Prompt(prompt));
                    _vm.ResultText = resultNoFile ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI request failed: {ex.Message}", "AI Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _vm.IsLoading = false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
        }
    }
}
