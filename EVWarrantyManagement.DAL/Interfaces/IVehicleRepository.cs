using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.DAL.Interfaces;

public interface IVehicleRepository
{
    Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Vehicle?> GetByIdAsync(int vehicleId, CancellationToken cancellationToken = default);

    Task<Vehicle?> GetByVinAsync(string vin, CancellationToken cancellationToken = default);

    Task<Vehicle> CreateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task DeleteAsync(int vehicleId, CancellationToken cancellationToken = default);
}

