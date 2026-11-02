using Application.DTOs.User;
using Domain.Entities;
using Domain.Enums;
using Mapster;

namespace Application.Mappings;

public class UserMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to UserResponse
        config.NewConfig<User, UserResponse>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.Role, src => src.Role.ToString());

        // Entity to UserListResponse
        config.NewConfig<User, UserListResponse>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.Role, src => src.Role.ToString());

        // CreateUserRequest to Entity (Password hashing should be done in service layer)
        config.NewConfig<CreateUserRequest, User>()
            .Map(dest => dest.Role, src => Enum.Parse<UserRole>(src.Role))
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.IsActive, src => true)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.PasswordHash) // Handle in service with BCrypt
            .Ignore(dest => dest.LastLoginAt)
            .Ignore(dest => dest.AssignedInstallations)
            .Ignore(dest => dest.AssignedTickets);

        // UpdateUserRequest to Entity
        config.NewConfig<UpdateUserRequest, User>()
            .IgnoreNullValues(true)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.PasswordHash)
            .Ignore(dest => dest.Role)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.LastLoginAt)
            .Ignore(dest => dest.AssignedInstallations)
            .Ignore(dest => dest.AssignedTickets);
    }
}