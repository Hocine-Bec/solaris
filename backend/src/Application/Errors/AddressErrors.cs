using Application.Common.Models;

namespace Application.Errors;

public static class AddressErrors
{
    public static readonly Error AlreadyExists =
        new(ErrorKind.Conflict, "address.already_exists", "Address already exists.");
}
