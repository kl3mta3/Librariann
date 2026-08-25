using System;

namespace Librariann.API.Services;

/// <summary>
/// Issues short-lived, one-use references so linked-library credentials do not have to be returned in navigation DTOs.
/// </summary>
public interface IExternalSourceLaunchTokenStore
{
    string Issue(Uri destination);
    bool TryTake(string token, out Uri? destination);
}
