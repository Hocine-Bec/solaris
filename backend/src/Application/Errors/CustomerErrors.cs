using Application.Common.Models;

namespace Application.Errors;

public static class CustomerErrors
{
    public static readonly Error EmailTaken =
        new(ErrorKind.Conflict, "customer.email_taken", "A customer with this email already exists.");
}
