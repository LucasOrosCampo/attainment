using System.Windows;
using System.Windows.Controls;
using attainment.ViewModels;

namespace attainment.Views;

public partial class ExamCreationPage : Page
{
    public ExamCreationPage(ExamCreationViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public ExamCreationViewModel ViewModel { get; }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationService?.CanGoBack == true)
        {
            NavigationService.GoBack();
        }
    }
}
