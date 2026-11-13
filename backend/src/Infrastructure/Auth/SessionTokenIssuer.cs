using System.Globalization;
using System.Text;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

/// <summary>A JWT signed with HMAC-SHA256, holding the account's ID, email and role, valid for the session's length.</summary>
public sealed class SessionTokenIssuer(IOptions<JwtOptions> options) : ISessionTokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new();

    public (string Token, DateTimeOffset ExpiresAt) Issue(User user)
    {
        var jwt = options.Value;
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddHours(jwt.LifetimeHours);

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [Session.UserIdClaim] = user.Id.ToString(CultureInfo.InvariantCulture),
                [Session.EmailClaim] = user.Email,
                [Session.RoleClaim] = user.Role.ToString()
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)), SecurityAlgorithms.HmacSha256)
        });
        return (token, expiresAt);
    }
}
