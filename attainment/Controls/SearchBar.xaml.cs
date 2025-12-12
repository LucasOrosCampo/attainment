using System.Windows;
using System.Windows.Controls;

namespace attainment.Controls
{
    public partial class SearchBar : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(SearchBar), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly RoutedEvent SearchTextChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(SearchTextChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchBar));

        public event RoutedEventHandler SearchTextChanged
        {
            add => AddHandler(SearchTextChangedEvent, value);
            remove => RemoveHandler(SearchTextChangedEvent, value);
        }

        public static readonly RoutedEvent SearchSubmittedEvent = EventManager.RegisterRoutedEvent(
            nameof(SearchSubmitted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchBar));

        public event RoutedEventHandler SearchSubmitted
        {
            add => AddHandler(SearchSubmittedEvent, value);
            remove => RemoveHandler(SearchSubmittedEvent, value);
        }

        public SearchBar()
        {
            InitializeComponent();
        }

        private void PART_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SearchTextChangedEvent, this));
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SearchSubmittedEvent, this));
        }
    }
}
