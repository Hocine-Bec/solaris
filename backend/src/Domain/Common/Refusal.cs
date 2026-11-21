namespace Domain.Common;

/// <summary>
/// Why the Domain refused a change: a stable code (such as <c>installation.transition_not_allowed</c>) and a message for
/// people. The Domain doesn't reference <c>Application</c>, where <c>Result</c> lives (ADR 1): a service turns a refusal
/// into an error.
/// </summary>
public sealed record Refusal(string Code, string Message);
