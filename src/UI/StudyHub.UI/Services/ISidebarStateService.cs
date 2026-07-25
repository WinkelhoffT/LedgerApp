namespace StudyHub.UI.Services;

public interface ISidebarStateService
{
    bool IsOpen { get; }

    event Action? Changed;

    void Toggle();

    void Close();
}
