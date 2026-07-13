using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using attainment.Models;

namespace attainment.Controls;

public partial class SubjectCard : UserControl
{
    public static readonly RoutedEvent DeleteRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(DeleteRequested),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(SubjectCard));

    public static readonly RoutedEvent OpenRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(OpenRequested),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(SubjectCard));

    public static readonly RoutedEvent FavoriteChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(FavoriteChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(SubjectCard));

    public static readonly DependencyProperty SubjectProperty = DependencyProperty.Register(
        nameof(Subject),
        typeof(Subject),
        typeof(SubjectCard),
        new PropertyMetadata(null));

    public Subject Subject
    {
        get => (Subject)GetValue(SubjectProperty);
        set => SetValue(SubjectProperty, value);
    }

    public event RoutedEventHandler DeleteRequested
    {
        add => AddHandler(DeleteRequestedEvent, value);
        remove => RemoveHandler(DeleteRequestedEvent, value);
    }

    public event RoutedEventHandler OpenRequested
    {
        add => AddHandler(OpenRequestedEvent, value);
        remove => RemoveHandler(OpenRequestedEvent, value);
    }

    public event RoutedEventHandler FavoriteChanged
    {
        add => AddHandler(FavoriteChangedEvent, value);
        remove => RemoveHandler(FavoriteChangedEvent, value);
    }

    public SubjectCard()
    {
        InitializeComponent();
    }

    private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent, this));
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(FavoriteChangedEvent, this));
    }

    protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!e.Handled)
        {
            RaiseEvent(new RoutedEventArgs(OpenRequestedEvent, this));
        }
    }
}

public sealed class FavoriteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Brushes.Gold : Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
