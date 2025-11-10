using System.Threading;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var isValid = await _userRepository.ValidateCredentialsAsync(username, password, cancellationToken);
        if (!isValid)
        {
            return null;
        }

        return await _userRepository.GetByUsernameAsync(username, cancellationToken);
    }

    public Task<User> CreateUserAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        return _userRepository.CreateAsync(user, password, cancellationToken);
    }

    public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByUsernameAsync(username, cancellationToken);
    }
}

