using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Mapping;
using Librariann.API.Repositories;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.Email;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public class EmailHistoryRepository(DataContext context) : IEmailHistoryRepository
{
    public async Task<IList<EmailHistoryDto>> GetEmailDtos(UserParams userParams, CancellationToken ct = default)
    {
        return await context.EmailHistory
            .OrderByDescending(h => h.SendDate)
            .Select(EmailHistoryMapping.ToEmailHistoryDtoExpression)
            .ToListAsync(ct);
    }
}
