using System.Security.Cryptography;
using System.Text;
using System.Threading;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVWarrantyManagement.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly EVWarrantyManagementContext _context;

    public UserRepository(EVWarrantyManagementContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, string plainPassword, CancellationToken cancellationToken = default)
    {
        user.PasswordHash = HashPassword(plainPassword);
        user.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string plainPassword, CancellationToken cancellationToken = default)
    {
        var user = await GetByUsernameAsync(username, cancellationToken);
        if (user is null || user.PasswordHash is null)
        {
            return false;
        }

        var computedHash = HashPassword(plainPassword);
        return CryptographicOperations.FixedTimeEquals(user.PasswordHash, computedHash);
    }

    private static byte[] HashPassword(string plainPassword)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(plainPassword);
        return SHA256.HashData(passwordBytes);
    }
}

