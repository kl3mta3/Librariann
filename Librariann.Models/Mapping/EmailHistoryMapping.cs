using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Email;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;EmailHistory, EmailHistoryDto&gt;()</c>.</summary>
public static class EmailHistoryMapping
{
    public static readonly Expression<Func<EmailHistory, EmailHistoryDto>> ToEmailHistoryDtoExpression = e => new EmailHistoryDto
    {
        Id = e.Id,
        Sent = e.Sent,
        SendDate = e.SendDate,
        EmailTemplate = e.EmailTemplate,
        ErrorMessage = e.ErrorMessage,
        ToUserName = e.AppUser.UserName!,
    };

    private static readonly Func<EmailHistory, EmailHistoryDto> CompiledToEmailHistoryDto = ToEmailHistoryDtoExpression.Compile();

    public static EmailHistoryDto ToEmailHistoryDto(this EmailHistory e) => CompiledToEmailHistoryDto(e);
}
