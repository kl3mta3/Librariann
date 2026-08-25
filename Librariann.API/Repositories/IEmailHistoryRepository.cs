using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.Email;

namespace Librariann.API.Repositories;

public interface IEmailHistoryRepository
{
    Task<IList<EmailHistoryDto>> GetEmailDtos(UserParams userParams, CancellationToken ct = default);
}
