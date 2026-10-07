using StudyHub.Shared.Anki;

namespace StudyHub.Logic.Integration.Anki;

/// <summary>
/// Read-only access to the local Anki desktop app through the AnkiConnect add-on. Throws
/// <see cref="AnkiConnectUnavailableException"/> when Anki can't be reached or reports an error.
/// </summary>
public interface IAnkiConnectAccessor
{
    /// <summary>Today's due counts for every deck, subdecks included, under their full names.</summary>
    Task<IReadOnlyList<AnkiDeckCountsDto>> GetDeckCountsAsync(CancellationToken cancellationToken = default);
}
