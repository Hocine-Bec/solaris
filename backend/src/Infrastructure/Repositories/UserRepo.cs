using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepo(AppDbContext context) : GenericRepo<User>(context), IUserRepo
{
    public Task<User?> GetByEmailAsync(string email)
    {
        var normalized = User.NormalizeEmail(email);
        return Context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized);
    }

    public Task<bool> AnyActiveAdminAsync() =>
        Context.Users.AnyAsync(u => u.Role == UserRole.Admin && u.IsActive);

    public Task<bool> IsActiveAsync(int id) =>
        Context.Users.AnyAsync(u => u.Id == id && u.IsActive);
}
