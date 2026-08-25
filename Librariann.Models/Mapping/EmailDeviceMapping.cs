using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Device.EmailDevice;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;Device, EmailDeviceDto&gt;()</c>.</summary>
public static class EmailDeviceMapping
{
    public static readonly Expression<Func<Device, EmailDeviceDto>> ToEmailDeviceDtoExpression = d => new EmailDeviceDto
    {
        Id = d.Id,
        Name = d.Name!,
        EmailAddress = d.EmailAddress!,
        Platform = d.Platform,
    };

    private static readonly Func<Device, EmailDeviceDto> CompiledToEmailDeviceDto = ToEmailDeviceDtoExpression.Compile();

    public static EmailDeviceDto ToEmailDeviceDto(this Device d) => CompiledToEmailDeviceDto(d);
}
