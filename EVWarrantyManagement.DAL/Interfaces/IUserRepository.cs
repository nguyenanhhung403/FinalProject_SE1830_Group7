using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.DAL.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User> CreateAsync(User user, string plainPassword, CancellationToken cancellationToken = default);

    Task<bool> ValidateCredentialsAsync(string username, string plainPassword, CancellationToken cancellationToken = default);
}

