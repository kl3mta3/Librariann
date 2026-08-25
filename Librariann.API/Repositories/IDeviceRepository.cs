using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Device.EmailDevice;
using Librariann.Models.Entities;

namespace Librariann.API.Repositories;

public interface IDeviceRepository
{
    void Update(Device device);
    Task<IList<EmailDeviceDto>> GetDevicesForUserAsync(int userId, CancellationToken ct = default);
    Task<Device?> GetDeviceById(int deviceId, CancellationToken ct = default);
}
