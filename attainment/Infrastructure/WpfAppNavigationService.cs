using attainment.Application;
using attainment.Models;
using attainment.Views;
using Microsoft.Extensions.DependencyInjection;

namespace attainment.Infrastructure;

public sealed class WpfAppNavigationService(IServiceProvider serviceProvider) : IAppNavigationService
{
    public void OpenResources(int subjectId) => GetMainWindow().OpenResourcesForSubject(subjectId);

    public void OpenExam(Resource resource)
    {
        var page = serviceProvider.GetRequiredService<ExamCreationPage>();
        page.ViewModel.Resource = resource;
        GetMainWindow().ResourcesFrame.Navigate(page);
    }

    public void GoBack()
    {
        var frame = GetMainWindow().ResourcesFrame;
        if (frame.CanGoBack)
        {
            frame.GoBack();
        }
    }

    private static MainWindow GetMainWindow() =>
        System.Windows.Application.Current.MainWindow as MainWindow
        ?? throw new InvalidOperationException("The main window is not available.");
}
