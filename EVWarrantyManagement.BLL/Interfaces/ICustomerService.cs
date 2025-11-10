using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.BLL.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default);

    Task<Customer?> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default);

    Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    Task DeleteCustomerAsync(int customerId, CancellationToken cancellationToken = default);
}

