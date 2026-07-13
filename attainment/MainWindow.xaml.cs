using System.Windows;

namespace attainment;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Views.ResourcePage _resourcePage;

    public MainWindow(
        Views.SubjectsPage subjectsPage,
        Views.ResourcePage resourcePage,
        Views.ProductPage productPage,
        Views.SettingsPage settingsPage)
    {
        InitializeComponent();
        _resourcePage = resourcePage;

        SubjectsFrame.Content = subjectsPage;
        ResourcesFrame.Content = resourcePage;
        ProductsFrame.Content = productPage;
        SettingsFrame.Content = settingsPage;
    }

    public void OpenResourcesForSubject(int subjectId)
    {
        MainTabControl.SelectedIndex = 1;
        _resourcePage.SelectSubject(subjectId);
    }
}
