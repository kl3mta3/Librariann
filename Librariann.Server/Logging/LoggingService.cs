using Librariann.API.Services;

namespace Librariann.Server.Logging;

public class LoggingService: ILoggingService
{
    public void SwitchLogLevel(string level)
    {
        LogLevelOptions.SwitchLogLevel(level);
    }
}
