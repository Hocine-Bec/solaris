namespace Application.Common.Exceptions;

/// <summary>The mail server can't be reached, so no email can be sent until it comes back.</summary>
public sealed class MailServerUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
