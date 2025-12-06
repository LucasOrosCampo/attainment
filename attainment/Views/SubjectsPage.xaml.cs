using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using attainment.Models;
using attainment.Controls;

namespace attainment.Views
{
    /// <summary>
    /// Interaction logic for SubjectsPage.xaml
    /// </summary>
    public partial class SubjectsPage : Page
    {
        private enum ViewMode
        {
            List,
            Create
        }
        
        private ApplicationDbContext _dbContext;
        private List<Subject> _allSubjects = [];
        private ViewMode _currentMode = ViewMode.List;
        
        public SubjectsPage()
        {
            InitializeComponent();
            _dbContext = new ApplicationDbContext();
            LoadSubjects();

            // Listen for delete requests bubbling up from SubjectCard context menus
            SubjectsItemsControl.AddHandler(SubjectCard.DeleteRequestedEvent, new RoutedEventHandler(SubjectCard_DeleteRequested));
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
            // Switch to creation mode
            SwitchMode(ViewMode.Create);
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Switch back to list mode without saving
            SwitchMode(ViewMode.List);
        }
        
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(SubjectNameTextBox.Text))
            {
                MessageBox.Show("Subject name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                // Create new subject from form data
                var newSubject = new Subject
                {
                    Name = SubjectNameTextBox.Text.Trim(),
                    Description = DescriptionTextBox.Text.Trim(),
                    IsFavorite = IsFavoriteCheckBox.IsChecked ?? false
                };
                
                // Add to database and save changes
                _dbContext.Subjects.Add(newSubject);
                await _dbContext.SaveChangesAsync();
                
                // Reload subjects to show the new one
                LoadSubjects();
                
                // Switch back to list mode
                SwitchMode(ViewMode.List);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving subject: {ex.Message}", "Database Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    
        private async void SubjectCard_DeleteRequested(object sender, RoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource is SubjectCard card && card.Subject is Subject subject)
                {
                    // Remove the subject from the database
                    _dbContext.Subjects.Remove(subject);
                    await _dbContext.SaveChangesAsync();

                    // Reload subjects from the database to refresh the view
                    LoadSubjects();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting subject: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SwitchMode(ViewMode mode)
        {
            _currentMode = mode;
            
            switch (mode)
            {
                case ViewMode.List:
                    // Show list view, hide creation view
                    SubjectsListView.Visibility = Visibility.Visible;
                    CreateSubjectView.Visibility = Visibility.Collapsed;
                    break;
                    
                case ViewMode.Create:
                    // Reset form fields
                    SubjectNameTextBox.Text = string.Empty;
                    DescriptionTextBox.Text = string.Empty;
                    IsFavoriteCheckBox.IsChecked = false;
                    
                    // Show creation view, hide list view
                    SubjectsListView.Visibility = Visibility.Collapsed;
                    CreateSubjectView.Visibility = Visibility.Visible;
                    
                    // Set focus to the name field
                    SubjectNameTextBox.Focus();
                    break;
            }
        }
    }
}
