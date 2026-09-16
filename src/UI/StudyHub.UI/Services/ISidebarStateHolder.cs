namespace StudyHub.UI.Services;

public interface ISidebarStateHolder
{
    bool IsOpen { get; }

    event Action? Changed;

    void Toggle();

    void Close();
}
