namespace Domain.Enums;

/// <summary>Which notification an outbox message delivers.</summary>
public enum OutboxMessageType
{
    /// <summary>To the person who asked for a quote: what happens next, and what they sent.</summary>
    LeadWelcome,

    /// <summary>To the office: a new lead, who, where and when to call.</summary>
    SalesNewLeadAlert,

    /// <summary>To the customer: their system is switched on (FR-I7).</summary>
    InstallationActivated
}
