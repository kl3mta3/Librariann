using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Mapping;
using Librariann.API.Repositories;
using Librariann.Models.DTOs.Device.EmailDevice;
using Librariann.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public class DeviceRepository(DataContext context) : IDeviceRepository
{
    public void Update(Device device)
    {
        context.Entry(device).State = EntityState.Modified;
    }

    public async Task<IList<EmailDeviceDto>> GetDevicesForUserAsync(int userId, CancellationToken ct = default)
    {
        return await context.Device
            .Where(d => d.AppUserId == userId)
            .OrderBy(d => d.LastUsed)
            .Select(EmailDeviceMapping.ToEmailDeviceDtoExpression)
            .ToListAsync(ct);
    }

    public async Task<Device?> GetDeviceById(int deviceId, CancellationToken ct = default)
    {
        return await context.Device
            .Where(d => d.Id == deviceId)
            .SingleOrDefaultAsync(ct);
    }
}
