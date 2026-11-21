namespace Application.DTOs.Lead;

/// <summary>
/// The quote form's answer: the lead's ID only. A request for an email that already has an open lead answers that
/// lead (BR-L1), so the answer carries nothing a stranger who typed that email shouldn't see.
/// </summary>
public sealed record LeadCreatedResponse(int Id);

/// <summary>What sending the quote form did: a new lead, or the open lead this email already had (BR-L1).</summary>
public sealed record LeadCreated(int Id, bool IsNew);
