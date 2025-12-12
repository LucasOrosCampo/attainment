using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using attainment.Models;
using Microsoft.Win32;

namespace attainment.Views
{
    /// <summary>
    /// Interaction logic for ResourcePage.xaml
    /// </summary>
    public partial class ResourcePage : Page
    {
        private enum ViewMode
        {
            List,
            Create
        }

        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        private Subject? _initialSubject;
        private List<Subject> _allSubjects = [];
        private List<Resource> _allResources = [];
        private bool _subjectsLoaded = false;
        private ViewMode _currentMode = ViewMode.List;

        // Parameterless constructor for XAML navigation (Resources tab direct click)
        public ResourcePage()
        {
            InitializeComponent();
            Loaded += ResourcePage_Loaded;
        }

        public ResourcePage(Subject subject)
        {
            InitializeComponent();
            _initialSubject = subject;
            Loaded += ResourcePage_Loaded;
        }

        private async void ResourcePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Load subjects once on first navigation to this page
            if (!_subjectsLoaded)
            {
                await LoadSubjectsAsync();
                _subjectsLoaded = true;
            }

            // Always refresh resources list
            await LoadResourcesAsync();

            // Apply initial filter based on selected subject and search text
            ApplyFilter();
        }

        private async Task LoadSubjectsAsync()
        {
            try
            {
                _allSubjects = await _dbContext.Subjects
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                // Insert an "All subjects" pseudo-item at the top
                var allItem = new Subject { Id = 0, Name = "All subjects" };
                var subjectsForCombo = new List<Subject> { allItem };
                subjectsForCombo.AddRange(_allSubjects);

                SubjectsComboBox.ItemsSource = subjectsForCombo;
                SubjectsComboBox.SelectedValue = _initialSubject?.Id ?? 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading subjects: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadResourcesAsync()
        {
            try
            {
                _allResources = await _dbContext.Resources
                    .Include(r => r.Subject)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading resources: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResourceSearchBar_SearchTextChanged(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ResourceSearchBar_SearchSubmitted(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string searchTerm = (ResourceSearchBar?.Text ?? string.Empty).Trim().ToLower();
            int selectedSubjectId = 0;
            if (SubjectsComboBox?.SelectedValue is int id)
            {
                selectedSubjectId = id;
            }

            IEnumerable<Resource> query = _allResources;

            // Filter by selected subject (skip when 0 == All subjects)
            if (selectedSubjectId != 0)
            {
                query = query.Where(r => r.SubjectId == selectedSubjectId);
            }

            // Filter by search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    (r.Title?.ToLower().Contains(searchTerm) ?? false) ||
                    (r.Description?.ToLower().Contains(searchTerm) ?? false) ||
                    (r.Url?.ToLower().Contains(searchTerm) ?? false) ||
                    (r.FilePath?.ToLower().Contains(searchTerm) ?? false)
                );
            }

            ResourcesItemsControl.ItemsSource = query.ToList();
        }

        private void SubjectsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SwitchMode(ViewMode mode)
        {
            _currentMode = mode;
            switch (mode)
            {
                case ViewMode.List:
                    ResourcesListView.Visibility = Visibility.Visible;
                    CreateResourceView.Visibility = Visibility.Collapsed;
                    break;
                case ViewMode.Create:
                    // Reset fields
                    CreateTitleTextBox.Text = string.Empty;
                    CreateFileTextBox.Text = string.Empty;

                    // Populate subjects for creation (exclude the pseudo "All subjects")
                    CreateSubjectsComboBox.ItemsSource = _allSubjects;

                    // Preselect current filter subject if any
                    int selectedId = 0;
                    if (SubjectsComboBox?.SelectedValue is int id)
                        selectedId = id;
                    if (selectedId != 0 && _allSubjects.Any(s => s.Id == selectedId))
                        CreateSubjectsComboBox.SelectedValue = selectedId;
                    else if (_allSubjects.Count > 0)
                        CreateSubjectsComboBox.SelectedIndex = 0;

                    ResourcesListView.Visibility = Visibility.Collapsed;
                    CreateResourceView.Visibility = Visibility.Visible;
                    CreateTitleTextBox.Focus();
                    break;
            }
        }

        private void AddResourceButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMode(ViewMode.Create);
        }

        private void CreateCancelButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMode(ViewMode.List);
        }

        private async void CreateSaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate title
            var title = (CreateTitleTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Resource title is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate subject selection
            if (CreateSubjectsComboBox.SelectedValue is not int subjectId || !_allSubjects.Any(s => s.Id == subjectId))
            {
                MessageBox.Show("Please select a valid subject.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate title uniqueness (case-insensitive)
            bool exists = await _dbContext.Resources.AnyAsync(r => r.Title.ToLower() == title.ToLower());
            if (exists)
            {
                MessageBox.Show("A resource with the same title already exists.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate file if provided
            string? filePath = string.IsNullOrWhiteSpace(CreateFileTextBox.Text) ? null : CreateFileTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(filePath))
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("The selected file does not exist.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                string[] allowed = [".pdf", ".ppt", ".pptx"];
                if (!allowed.Contains(ext))
                {
                    MessageBox.Show("Only PDF or PPT/PPTX files are allowed.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                var resource = new Resource
                {
                    Title = title,
                    SubjectId = subjectId,
                    FilePath = filePath,
                    CreatedAt = DateTime.Now
                };

                _dbContext.Resources.Add(resource);
                await _dbContext.SaveChangesAsync();

                // Reload and return to list
                await LoadResourcesAsync();
                ApplyFilter();
                SwitchMode(ViewMode.List);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving resource: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Documents (*.pdf;*.ppt;*.pptx)|*.pdf;*.ppt;*.pptx|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                CreateFileTextBox.Text = dialog.FileName;
            }
        }

        private void FileIconButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn) return;
                var path = btn.Tag as string;
                if (string.IsNullOrWhiteSpace(path)) return;

                // If it's a file, open Explorer selecting it; if directory, open it directly
                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true
                    });
                }
                else if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show($"The file or directory does not exist:\n{path}", "Open in Explorer", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open in Explorer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
