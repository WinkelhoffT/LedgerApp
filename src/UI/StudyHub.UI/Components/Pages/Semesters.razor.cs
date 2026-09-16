using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.Semesters;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Semesters
{
    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    private IReadOnlyList<SemesterDto>? SemesterList { get; set; }

    private bool ShowArchived { get; set; }

    private bool IsDialogOpen { get; set; }

    private SemesterDto? EditingSemester { get; set; }

    private IReadOnlyList<SemesterDto> VisibleSemesters =>
        SemesterList is null
            ? []
            : SemesterList.Where(s => ShowArchived || !s.IsArchived).ToList();

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Semesters", "Manage your study terms");
        await LoadSemestersAsync();
    }

    private async Task LoadSemestersAsync()
    {
        SemesterList = await SemesterAccessor.GetAllAsync();
    }

    private void OpenCreateDialog()
    {
        EditingSemester = null;
        IsDialogOpen = true;
    }

    private void OpenEditDialog(SemesterDto semester)
    {
        EditingSemester = semester;
        IsDialogOpen = true;
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
        EditingSemester = null;
    }

    private async Task HandleSavedAsync(SemesterDto _)
    {
        await LoadSemestersAsync();
    }

    private async Task ArchiveAsync(SemesterDto semester)
    {
        await SemesterAccessor.ArchiveAsync(semester.Id);
        await LoadSemestersAsync();
    }

    private async Task RestoreAsync(SemesterDto semester)
    {
        await SemesterAccessor.RestoreAsync(semester.Id);
        await LoadSemestersAsync();
    }
}
