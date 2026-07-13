using System.Windows;
using attainment.Application;
using Microsoft.Extensions.Logging;

namespace attainment.Infrastructure;

public sealed class WpfUserNotificationService(ILogger<WpfUserNotificationService> logger)
    : IUserNotificationService
{
    public void ShowError(string title, string message, Exception exception)
    {
        logger.LogError(exception, "{ErrorTitle}: {UserMessage}", title, message);
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
