using Application.DTOs.Common;

namespace Application.DTOs.Lead;

/// <summary>
/// A page of the lead list (FR-L3): <c>?status=New&amp;page=1</c>. <c>status</c> may repeat
/// (<c>status=Converted&amp;status=Disqualified&amp;status=Lost</c>); none lists every lead.
/// </summary>
public sealed class LeadPageRequest : PageRequest
{
    public List<string> Status { get; set; } = [];
}
