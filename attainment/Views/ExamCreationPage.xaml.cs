using System.Windows;
using System.Windows.Controls;
using attainment.ViewModels;
using attainment.Application;

namespace attainment.Views;

public partial class ExamCreationPage : Page
{
    private readonly IAppNavigationService _navigationService;

    public ExamCreationPage(ExamCreationViewModel viewModel, IAppNavigationService navigationService)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _navigationService = navigationService;
        DataContext = viewModel;
    }

    public ExamCreationViewModel ViewModel { get; }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.GoBack();
    }
}
