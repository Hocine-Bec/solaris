using Domain.Entities;

namespace Application.Interfaces.Services;

/// <summary>Issues the signed token a staff session carries.</summary>
public interface ISessionTokenIssuer
{
    (string Token, DateTimeOffset ExpiresAt) Issue(User user);
}
