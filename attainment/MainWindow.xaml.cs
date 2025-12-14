using System.Windows;
using attainment.ViewModels;

namespace attainment;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm, Views.ProductPage productPage)
    {
        InitializeComponent();
        DataContext = vm;

        // Inject ProductPage instance created by DI into the Products tab
        if (ProductsFrame != null)
        {
            ProductsFrame.Content = productPage;
        }
    }
}