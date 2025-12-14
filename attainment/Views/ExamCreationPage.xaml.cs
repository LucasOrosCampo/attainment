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
                var result = await Task.Run(() => _vm.Ai.Prompt(prompt));
                _vm.ResultText = result ?? string.Empty;
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
    }
}
