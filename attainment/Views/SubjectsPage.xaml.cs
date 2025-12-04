using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using attainment.Models;

namespace attainment.Views
{
    /// <summary>
    /// Interaction logic for SubjectsPage.xaml
    /// </summary>
    public partial class SubjectsPage : Page
    {
        private ApplicationDbContext _dbContext;
        private List<Subject> _allSubjects = new List<Subject>();
        
        public SubjectsPage()
        {
            InitializeComponent();
            _dbContext = new ApplicationDbContext();
            LoadSubjects();
        }

        private async void LoadSubjects()
        {
            try
            {
                // Load subjects from database
                _allSubjects = await _dbContext.Subjects.ToListAsync();
                SubjectsItemsControl.ItemsSource = _allSubjects;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading subjects: {ex.Message}", "Database Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string searchTerm = SearchBox.Text.Trim().ToLower();
            
            if (string.IsNullOrEmpty(searchTerm))
            {
                SubjectsItemsControl.ItemsSource = _allSubjects;
                return;
            }
            
            var filteredSubjects = _allSubjects.Where(s => 
                s.Name.ToLower().Contains(searchTerm) || 
                (s.Description != null && s.Description.ToLower().Contains(searchTerm))
            ).ToList();
            
            SubjectsItemsControl.ItemsSource = filteredSubjects;
        }

        private void AddSubjectButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement adding new subject functionality
            // This would typically open a dialog or navigate to a new page
            MessageBox.Show("Add Subject functionality will be implemented soon.", 
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
