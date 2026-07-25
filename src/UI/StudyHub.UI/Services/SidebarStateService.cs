namespace StudyHub.UI.Services;

public sealed class SidebarStateService : ISidebarStateService
{
    public bool IsOpen { get; private set; }

    public event Action? Changed;

    public void Toggle() => SetOpen(!IsOpen);

    public void Close() => SetOpen(false);

    private void SetOpen(bool isOpen)
    {
        if (IsOpen == isOpen)
        {
            return;
        }

        IsOpen = isOpen;
        Changed?.Invoke();
    }
}
