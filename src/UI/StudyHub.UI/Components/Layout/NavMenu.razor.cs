using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Layout;

public partial class NavMenu : ComponentBase, IDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ISidebarStateService SidebarState { get; set; } = default!;

    private static readonly NavGroup[] Groups =
    [
        new(
            "Overview",
            [
                new("/", "Dashboard", Icons.Dashboard),
                new("/semesters", "Semesters", Icons.Semesters),
                new("/courses", "Courses", Icons.Courses),
            ]),
        new(
            "Study",
            [
                new("/calendar", "Calendar", Icons.Calendar),
                new("/documents", "Documents", Icons.Documents),
                new("/notes", "Notes", Icons.Notes),
                new("/flashcards", "Flashcards", Icons.Flashcards),
            ]),
        new(
            "Intelligence",
            [
                new("/ai", "AI Assistant", Icons.Ai, ShowBadge: true),
                new("/analytics", "Analytics", Icons.Analytics),
            ]),
        new(
            null,
            [
                new("/settings", "Settings", Icons.Settings),
            ]),
    ];

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) => SidebarState.Close();

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }

    private readonly record struct NavEntry(string Href, string Label, string IconPaths, bool ShowBadge = false);

    private readonly record struct NavGroup(string? Label, NavEntry[] Entries);

    private static class Icons
    {
        public const string Dashboard = "<path d=\"M3 13h8V3H3zM13 21h8v-8h-8zM13 9h8V3h-8zM3 21h8v-6H3z\"/>";
        public const string Semesters = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M16 2v4M8 2v4M3 10h18\"/>";
        public const string Courses = "<path d=\"M4 19.5A2.5 2.5 0 0 1 6.5 17H20\"/><path d=\"M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z\"/>";
        public const string Calendar = Semesters;
        public const string Documents = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z\"/><path d=\"M14 2v6h6M16 13H8M16 17H8M10 9H8\"/>";
        public const string Notes = "<path d=\"M12 20h9\"/><path d=\"M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z\"/>";
        public const string Flashcards = "<rect x=\"2\" y=\"6\" width=\"16\" height=\"14\" rx=\"2\"/><path d=\"M6 3h14a2 2 0 0 1 2 2v11\"/>";
        public const string Ai = "<path d=\"M12 2a3 3 0 0 0-3 3v1a3 3 0 0 0-3 3 3 3 0 0 0 0 6 3 3 0 0 0 3 3v1a3 3 0 0 0 6 0v-1a3 3 0 0 0 3-3 3 3 0 0 0 0-6 3 3 0 0 0-3-3V5a3 3 0 0 0-3-3z\"/><path d=\"M9 9h.01M15 9h.01\"/>";
        public const string Analytics = "<path d=\"M3 3v18h18\"/><path d=\"M18 9l-5 5-3-3-4 4\"/>";
        public const string Settings = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z\"/>";
    }
}
