using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using attainment.Models;
using attainment.Application;

namespace attainment.Views
{
    /// <summary>
    /// Interaction logic for ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        private enum ViewMode
        {
            List,
            Create
        }

        private readonly IProductService _productService;
        private readonly IUserNotificationService _notifications;
        private bool _resourcesLoaded = false;
        private ViewMode _currentMode = ViewMode.List;

        private List<Resource> _allResources = [];
        private List<Product> _allProducts = [];
        public ProductPage(IProductService productService, IUserNotificationService notifications)
        {
            InitializeComponent();
            _productService = productService;
            _notifications = notifications;
            Loaded += ProductPage_Loaded;
        }

        private async void ProductPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_resourcesLoaded)
            {
                await LoadResourcesAsync();
                _resourcesLoaded = true;
            }

            await LoadProductsAsync();
            ApplyFilter();
        }

        private async Task LoadResourcesAsync()
        {
            try
            {
                _allResources = [.. await _productService.GetResourcesAsync()];

                // Insert an "All resources" pseudo item
                var allItem = new Resource { Id = 0, Title = "All resources" };
                var listForCombo = new List<Resource> { allItem };
                listForCombo.AddRange(_allResources);

                ResourcesComboBox.ItemsSource = listForCombo;
                ResourcesComboBox.SelectedValue = 0;
            }
            catch (Exception ex)
            {
                _notifications.ShowError("Data Error", "Resources could not be loaded. Try again.", ex);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                _allProducts = [.. await _productService.GetAllAsync()];
            }
            catch (Exception ex)
            {
                _notifications.ShowError("Data Error", "Products could not be loaded. Try again.", ex);
            }
        }

        private void ProductSearchBar_SearchTextChanged(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ProductSearchBar_SearchSubmitted(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ResourcesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string search = (ProductSearchBar?.Text ?? string.Empty).Trim();
            int selectedResourceId = 0;
            if (ResourcesComboBox?.SelectedValue is int id)
                selectedResourceId = id;

            IEnumerable<Product> q = _allProducts;

            if (selectedResourceId != 0)
            {
                q = q.Where(p => p.ResourceId == selectedResourceId);
            }

            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(p =>
                    (p.Name?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                    (p.Content?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                    p.Type.ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (p.Resource?.Title?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                );
            }

            ProductsItemsControl.ItemsSource = q.ToList();
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMode(ViewMode.Create);
        }

        private void SwitchMode(ViewMode mode)
        {
            _currentMode = mode;
            switch (mode)
            {
                case ViewMode.List:
                    ProductsListView.Visibility = Visibility.Visible;
                    CreateProductView.Visibility = Visibility.Collapsed;
                    break;
                case ViewMode.Create:
                    // reset fields
                    CreateNameTextBox.Text = string.Empty;
                    CreateContentTextBox.Text = string.Empty;

                    // Resource selector for creation uses real resources (no pseudo)
                    CreateResourcesComboBox.ItemsSource = _allResources;

                    // Preselect current resource if any
                    int selectedId = 0;
                    if (ResourcesComboBox?.SelectedValue is int rid) selectedId = rid;
                    if (selectedId != 0 && _allResources.Any(r => r.Id == selectedId))
                        CreateResourcesComboBox.SelectedValue = selectedId;
                    else if (_allResources.Count > 0)
                        CreateResourcesComboBox.SelectedIndex = 0;

                    // Populate type combo from enum
                    CreateTypeComboBox.ItemsSource = Enum.GetValues(typeof(ProductType));
                    if (CreateTypeComboBox.SelectedIndex < 0)
                        CreateTypeComboBox.SelectedIndex = 0;

                    ProductsListView.Visibility = Visibility.Collapsed;
                    CreateProductView.Visibility = Visibility.Visible;
                    CreateNameTextBox.Focus();
                    break;
            }
        }

        private void CreateCancelButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchMode(ViewMode.List);
        }

        private async void CreateSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var name = (CreateNameTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Product name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CreateResourcesComboBox.SelectedValue is not int resourceId || !_allResources.Any(r => r.Id == resourceId))
            {
                MessageBox.Show("Please select a valid resource.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CreateTypeComboBox.SelectedItem is not ProductType type)
            {
                MessageBox.Show("Please select a product type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string content = (CreateContentTextBox.Text ?? string.Empty).Trim();

            try
            {
                await _productService.CreateAsync(name, content, resourceId, type);

                await LoadProductsAsync();
                ApplyFilter();
                SwitchMode(ViewMode.List);
            }
            catch (DuplicateNameException ex)
            {
                MessageBox.Show(ex.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _notifications.ShowError("Data Error", "The product could not be saved. Try again.", ex);
            }
        }
    }
}
