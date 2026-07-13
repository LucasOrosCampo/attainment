using attainment.Models;

namespace attainment.Application;

public interface IAppNavigationService
{
    void OpenResources(int subjectId);

    void OpenExam(Resource resource);

    void GoBack();
}
