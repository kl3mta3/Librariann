using System;
using System.Threading.Tasks;
using Librariann.API.Attributes;
using Librariann.API.Services.SignalR;
using Librariann.Common.Extensions;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Librariann.Services.SignalR;

public interface ILogHub;

[Authorize]
[SkipDeviceTracking]
public class LogHub : Hub<ILogHub>
{
    private readonly IEventHub _eventHub;
    private readonly IPresenceTracker _tracker;

    public LogHub(IEventHub eventHub, IPresenceTracker tracker)
    {
        _eventHub = eventHub;
        _tracker = tracker;
    }


    public override async Task OnConnectedAsync()
    {
        await _tracker.UserConnected(Context.User!.GetUserId(), Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _tracker.UserDisconnected(Context.User!.GetUserId(), Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    [Authorize(PolicyConstants.AdminRole)]
    public async Task SendLogAsString(string message)
    {
        await _eventHub.SendMessageAsync("LogString", new SignalRMessage()
        {
            Body = message,
            EventType = "LogString",
            Name = "LogString",
        }, true);
    }

    [Authorize(PolicyConstants.AdminRole)]
    public async Task SendLogAsObject(object messageObject)
    {
        await _eventHub.SendMessageAsync("LogObject", new SignalRMessage()
        {
            Body = messageObject,
            EventType = "LogString",
            Name = "LogString",
        }, true);
    }
}
