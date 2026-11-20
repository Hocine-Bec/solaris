namespace Domain.Enums;

public enum OutboxMessageStatus
{
    /// <summary>Waiting to be sent, at its next attempt time.</summary>
    Pending,

    /// <summary>The mail server accepted it.</summary>
    Sent,

    /// <summary>Still not sent once its 24 hours had passed: shown to the Admin, who can retry it (FR-N2).</summary>
    Failed
}
