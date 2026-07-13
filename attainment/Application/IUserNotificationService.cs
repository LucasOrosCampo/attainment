namespace attainment.Application;

public interface IUserNotificationService
{
    void ShowError(string title, string message, Exception exception);
}
