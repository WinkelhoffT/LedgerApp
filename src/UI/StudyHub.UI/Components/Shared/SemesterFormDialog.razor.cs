using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.Semesters;

namespace StudyHub.UI.Components.Shared;

public partial class SemesterFormDialog
{
    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public SemesterDto? EditingSemester { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<SemesterDto> OnSaved { get; set; }

    private string Name { get; set; } = string.Empty;

    private DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    private DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(4));

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private SemesterDto? _lastLoadedSemester;

    protected override void OnParametersSet()
    {
        if (!IsOpen)
        {
            return;
        }

        if (EditingSemester == _lastLoadedSemester)
        {
            return;
        }

        _lastLoadedSemester = EditingSemester;
        ErrorMessage = null;
        Name = EditingSemester?.Name ?? string.Empty;
        StartDate = EditingSemester?.StartDate ?? DateOnly.FromDateTime(DateTime.Today);
        EndDate = EditingSemester?.EndDate ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(4));
    }

    private async Task SubmitAsync()
    {
        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var saved = EditingSemester is null
                ? await SemesterAccessor.CreateAsync(new CreateSemesterRequest(Name, StartDate, EndDate))
                : await SemesterAccessor.UpdateAsync(new UpdateSemesterRequest(EditingSemester.Id, Name, StartDate, EndDate));

            await OnSaved.InvokeAsync(saved);
            await Close();
        }
        catch (SemesterValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (DuplicateSemesterNameException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (SemesterArchivedException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task Close()
    {
        ErrorMessage = null;
        _lastLoadedSemester = null;
        await OnClose.InvokeAsync();
    }
}
