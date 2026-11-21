namespace Application.DTOs.Lead;

/// <summary>
/// An open lead's corrected contact details (FR-L7): a typo in the name, the email or the phone. The property and the
/// visitor's note stay as they were sent.
/// </summary>
public sealed class UpdateLeadRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
