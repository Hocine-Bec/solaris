namespace Application.Common.Models;

/// <summary>How a save ended. Any other failure is unexpected and stays an exception.</summary>
public enum SaveOutcome
{
    Saved,

    /// <summary>A row was changed or deleted after it was read.</summary>
    Conflict,

    /// <summary>A unique index rejected the write.</summary>
    Duplicate
}
