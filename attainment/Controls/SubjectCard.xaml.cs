using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using attainment.Models;

namespace attainment.Controls
{
    /// <summary>
    /// Interaction logic for SubjectCard.xaml
    /// </summary>
    public partial class SubjectCard : UserControl
    {
        // Routed event to notify parent controls that a delete was requested via context menu
        public static readonly RoutedEvent DeleteRequestedEvent = EventManager.RegisterRoutedEvent(
            name: nameof(DeleteRequested),
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(RoutedEventHandler),
            ownerType: typeof(SubjectCard));

        public event RoutedEventHandler DeleteRequested
        {
            add => AddHandler(DeleteRequestedEvent, value);
            remove => RemoveHandler(DeleteRequestedEvent, value);
        }

        public static readonly DependencyProperty SubjectProperty = 
            DependencyProperty.Register(
                nameof(Subject), 
                typeof(Subject), 
                typeof(SubjectCard), 
                new PropertyMetadata(null)
            );
        
        public Subject Subject
        {
            get => (Subject)GetValue(SubjectProperty);
            set => SetValue(SubjectProperty, value);
        }
        
        public SubjectCard()
        {
            InitializeComponent();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Raise a bubbling event so the page hosting the card can handle deletion
            RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent, this));
        }
    }

    public class FavoriteColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFavorite = (bool)value;
            return isFavorite ? Brushes.Gold : Brushes.Gray;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
