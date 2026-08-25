using System.Collections.Generic;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.Common.Helpers;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librariann.Server.Controllers;

[Authorize(Policy = PolicyGroups.AdminPolicy)]
public class EmailController(IUnitOfWork unitOfWork) : BaseApiController
{
    [HttpGet("all")]
    public async Task<ActionResult<IList<EmailHistoryDto>>> GetEmails()
    {
        return Ok(await unitOfWork.EmailHistoryRepository.GetEmailDtos(UserParams.Default));
    }
}
