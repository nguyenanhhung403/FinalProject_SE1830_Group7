using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.BLL.Interfaces;

public interface IAuthService
{
    Task<User?> SignInAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<User> CreateUserAsync(User user, string password, CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
}

