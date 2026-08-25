using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.LibrariannPlus;

namespace Librariann.API.Services.Plus;

public interface ILibrariannPlusProviderHealthService
{
    Task<IList<LibrariannPlusProviderHealthSnapshotDto>> GetProviderHealthSnapshot(bool forceCheck = false, CancellationToken ct = default);
}
