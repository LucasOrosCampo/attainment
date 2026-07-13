using System.Windows;
using System.Windows.Controls;
using attainment.Application;
using attainment.Models;

namespace attainment.Views;

public partial class SettingsPage : Page
{
    private readonly ISettingsService _settingsService;
    private List<Setting> _settings = [];

    public SettingsPage(ISettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        Loaded += SettingsPage_Loaded;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ReloadAsync();
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            _settings = [.. await _settingsService.GetAllAsync()];
            SettingsItemsControl.ItemsSource = _settings;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reloading settings: {ex.Message}", "Database Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _settingsService.SaveAsync(_settings);
            await ReloadAsync();
            MessageBox.Show("Settings saved successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Database Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
