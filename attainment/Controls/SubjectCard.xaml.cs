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
            DataContext = Subject;
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
