namespace StudyHub.UI.Services;

/// <summary>
/// Raw JS-interop access to the browser-persisted theme preference - separate from
/// <see cref="IThemeStateHolder"/>, which holds the in-memory current value and notifies
/// subscribers when it changes.
/// </summary>
public interface IThemeAccessor
{
    Task<string> GetThemeAsync();

    Task SetThemeAsync(string theme);
}
