using Application.Common.Models;

namespace Application.Errors;

public static class AddressErrors
{
    public static Error NotFound(int id) =>
        new(ErrorKind.NotFound, "address.not_found", $"Address with ID {id} not found.");

    public static readonly Error AlreadyExists =
        new(ErrorKind.Conflict, "address.already_exists", "Address already exists.");

    public static readonly Error ConcurrentChange =
        new(ErrorKind.Conflict, "address.concurrent_change", "The address was modified by another user.");
}
