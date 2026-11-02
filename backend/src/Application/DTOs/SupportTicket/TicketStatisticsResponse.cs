namespace Application.DTOs.SupportTicket;

public class TicketStatisticsResponse
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public double AverageResolutionTimeDays { get; set; }
    public int HighPriorityTickets { get; set; }
}