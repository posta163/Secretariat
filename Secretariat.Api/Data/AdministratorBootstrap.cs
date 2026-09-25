using System.Data;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.Models;

namespace Secretariat.Api.Data;

public static class AdministratorBootstrap
{
    public static async Task<AppUser> EnsureCreatedAsync(SecretariatDbContext context)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var existing = await context.AppUsers
            .OrderBy(user => user.Id)
            .FirstOrDefaultAsync(user => user.Role == UserRole.Administrator);
        if (existing != null)
        {
            await transaction.CommitAsync();
            return existing;
        }

        const string email = "administrator@secretariat.test";
        if (await context.AppUsers.AnyAsync(user => user.Email == email))
            throw new InvalidOperationException("The bootstrap email is already assigned to a non-administrator account. No roles were changed.");

        var administrator = new AppUser
        {
            DisplayName = "Administrator",
            Email = email,
            Role = UserRole.Administrator
        };
        context.AppUsers.Add(administrator);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return administrator;
    }
}
