namespace Domain.Enums;

/// <summary>
/// Represents the current stage of a potential customer's journey
/// from initial inquiry to conversion in the solar sales process.
/// </summary>
public enum LeadStatus
{
    /// <summary>
    /// A new lead captured from a website free quote request.
    /// </summary>
    New,

    /// <summary>
    /// The sales or support team has contacted the lead via phone, email, or message.
    /// </summary>
    Contacted,
    
    /// <summary>
    /// The lead has successfully converted into a paying customer.
    /// </summary>
    Converted,

    /// <summary>
    /// The lead is not a valid prospect (e.g., outside service area, fake submission, renter).
    /// </summary>
    Disqualified,
    
    /// <summary>
    /// The lead stopped responding or lost interest after some time.
    /// </summary>
    Lost,
}