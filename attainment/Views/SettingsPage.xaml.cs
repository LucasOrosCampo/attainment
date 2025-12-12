using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using attainment.Models;
using Microsoft.EntityFrameworkCore;

namespace attainment.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        private List<Setting> _settings = [];

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await _dbContext.Settings.LoadAsync();
            _settings = _dbContext.Settings
                .OrderBy(s => s.Key)
                .ToList();
            SettingsItemsControl.ItemsSource = _settings;
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _dbContext.Entry(this).ReloadAsync();
                await _dbContext.Settings.LoadAsync();
                _settings = _dbContext.Settings
                    .OrderBy(s => s.Key)
                    .ToList();
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
                // Ensure every declared key exists
                foreach (var key in KEYS.All())
                {
                    if (_settings.All(s => s.Key != key))
                    {
                        _settings.Add(new Setting { Key = key, Value = null });
                    }
                }

                // Sync context with current list
                foreach (var setting in _settings)
                {
                    var tracked = await _dbContext.Settings.FindAsync(setting.Key);
                    if (tracked == null)
                    {
                        await _dbContext.Settings.AddAsync(setting);
                    }
                    else
                    {
                        tracked.Value = setting.Value;
                    }
                }

                await _dbContext.SaveChangesAsync();
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
}
