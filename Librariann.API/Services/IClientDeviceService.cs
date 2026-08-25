using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Device.ClientDevice;
using Librariann.Models.Entities.Progress;
using Librariann.Models.Entities.User;

namespace Librariann.API.Services;

public interface IClientDeviceService
{
    Task<ClientDevice> IdentifyOrRegisterDeviceAsync(int userId, ClientInfoData clientInfo, string? uiFingerprint, CancellationToken cancellationToken = default);
    Task<bool> RenameDeviceAsync(int userId, int deviceId, string newName, CancellationToken ct = default);
    Task<bool> DeleteDeviceAsync(int userId, int deviceId, CancellationToken ct = default);
    Task UpdateFriendlyNameAsync(int userId, UpdateClientDeviceNameDto dto, CancellationToken ct = default);
}
