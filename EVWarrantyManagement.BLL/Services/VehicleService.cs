using System.Threading;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BO.Models;
using EVWarrantyManagement.DAL.Interfaces;

namespace EVWarrantyManagement.BLL.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehicleService(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.GetAllAsync(cancellationToken);
    }

    public Task<Vehicle?> GetVehicleAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
    }

    public Task<Vehicle?> GetVehicleByVinAsync(string vin, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.GetByVinAsync(vin, cancellationToken);
    }

    public Task<Vehicle> CreateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.CreateAsync(vehicle, cancellationToken);
    }

    public Task UpdateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
    }

    public Task DeleteVehicleAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.DeleteAsync(vehicleId, cancellationToken);
    }
}

