using System.Threading;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        return _customerRepository.GetAllAsync(cancellationToken);
    }

    public Task<Customer?> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return _customerRepository.GetByIdAsync(customerId, cancellationToken);
    }

    public Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        return _customerRepository.CreateAsync(customer, cancellationToken);
    }

    public Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        return _customerRepository.UpdateAsync(customer, cancellationToken);
    }

    public Task DeleteCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return _customerRepository.DeleteAsync(customerId, cancellationToken);
    }
}

