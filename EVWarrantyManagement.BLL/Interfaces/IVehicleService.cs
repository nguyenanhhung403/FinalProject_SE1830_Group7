using System.Threading;
using EVWarrantyManagement.BO.Models;

namespace EVWarrantyManagement.BLL.Interfaces;

public interface IVehicleService
{
    Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default);

    Task<Vehicle?> GetVehicleAsync(int vehicleId, CancellationToken cancellationToken = default);

    Task<Vehicle?> GetVehicleByVinAsync(string vin, CancellationToken cancellationToken = default);

    Task<Vehicle> CreateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task UpdateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task DeleteVehicleAsync(int vehicleId, CancellationToken cancellationToken = default);
}

