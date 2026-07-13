using System.Windows;
using System.Windows.Controls;
using attainment.Application;
using attainment.Controls;
using attainment.Models;

namespace attainment.Views;

public partial class SubjectsPage : Page
{
    private enum ViewMode
    {
        List,
        Create
    }

    private readonly ISubjectService _subjectService;
    private readonly IAppNavigationService _navigationService;
    private readonly IUserNotificationService _notifications;
    private List<Subject> _allSubjects = [];

    public SubjectsPage(
        ISubjectService subjectService,
        IAppNavigationService navigationService,
        IUserNotificationService notifications)
    {
        InitializeComponent();
        _subjectService = subjectService;
        _navigationService = navigationService;
        _notifications = notifications;
        Loaded += SubjectsPage_Loaded;

        SubjectsItemsControl.AddHandler(
            SubjectCard.DeleteRequestedEvent,
            new RoutedEventHandler(SubjectCard_DeleteRequested));
        SubjectsItemsControl.AddHandler(
            SubjectCard.OpenRequestedEvent,
            new RoutedEventHandler(SubjectCard_OpenRequested));
        SubjectsItemsControl.AddHandler(
            SubjectCard.FavoriteChangedEvent,
            new RoutedEventHandler(SubjectCard_FavoriteChanged));
    }

    private async void SubjectsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSubjectsAsync();
    }

    private async Task LoadSubjectsAsync()
    {
        try
        {
            _allSubjects = [.. await _subjectService.GetAllAsync()];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Data Error", "Subjects could not be loaded. Try again.", ex);
        }
    }

    private void SubjectSearchBar_SearchTextChanged(object sender, RoutedEventArgs e) => ApplyFilter();

    private void SubjectSearchBar_SearchSubmitted(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var searchTerm = (SubjectSearchBar?.Text ?? string.Empty).Trim();
        SubjectsItemsControl.ItemsSource = string.IsNullOrEmpty(searchTerm)
            ? _allSubjects
            : _allSubjects.Where(subject =>
                subject.Name.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                (subject.Description?.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ?? false))
                .ToList();
    }

    private void AddSubjectButton_Click(object sender, RoutedEventArgs e) => SwitchMode(ViewMode.Create);

    private void CancelButton_Click(object sender, RoutedEventArgs e) => SwitchMode(ViewMode.List);

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SubjectNameTextBox.Text))
        {
            MessageBox.Show("Subject name is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _subjectService.CreateAsync(
                SubjectNameTextBox.Text,
                DescriptionTextBox.Text,
                IsFavoriteCheckBox.IsChecked ?? false);
            await LoadSubjectsAsync();
            SwitchMode(ViewMode.List);
        }
        catch (DuplicateNameException ex)
        {
            MessageBox.Show(ex.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Data Error", "The subject could not be saved. Try again.", ex);
        }
    }

    private async void SubjectCard_DeleteRequested(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not SubjectCard { Subject: Subject subject })
        {
            return;
        }

        try
        {
            var impact = await _subjectService.GetDeletionImpactAsync(subject.Id);
            var message = impact.ResourceCount == 0
                ? $"Delete '{subject.Name}'?"
                : $"Delete '{subject.Name}' and its {impact.ResourceCount} resources and {impact.ProductCount} products? This cannot be undone.";

            if (MessageBox.Show(message, "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
                MessageBoxResult.Yes)
            {
                return;
            }

            await _subjectService.DeleteAsync(subject.Id);
            await LoadSubjectsAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Data Error", "The subject could not be deleted. Try again.", ex);
        }
    }

    private void SubjectCard_OpenRequested(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is SubjectCard { Subject: Subject subject })
        {
            _navigationService.OpenResources(subject.Id);
        }
    }

    private async void SubjectCard_FavoriteChanged(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not SubjectCard { Subject: Subject subject })
        {
            return;
        }

        try
        {
            await _subjectService.SetFavoriteAsync(subject.Id, subject.IsFavorite);
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Data Error", "The favorite could not be saved. The list will be reloaded.", ex);
            await LoadSubjectsAsync();
        }
    }

    private void SwitchMode(ViewMode mode)
    {
        switch (mode)
        {
            case ViewMode.List:
                SubjectsListView.Visibility = Visibility.Visible;
                CreateSubjectView.Visibility = Visibility.Collapsed;
                break;
            case ViewMode.Create:
                SubjectNameTextBox.Text = string.Empty;
                DescriptionTextBox.Text = string.Empty;
                IsFavoriteCheckBox.IsChecked = false;
                SubjectsListView.Visibility = Visibility.Collapsed;
                CreateSubjectView.Visibility = Visibility.Visible;
                SubjectNameTextBox.Focus();
                break;
        }
    }
}
