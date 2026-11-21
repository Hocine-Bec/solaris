using Application.Common.Models;

namespace Application.Errors;

public static class CustomerErrors
{
    public static Error NotFound(int id) =>
        new(ErrorKind.NotFound, "customer.not_found", $"Customer with ID {id} not found.");
}
