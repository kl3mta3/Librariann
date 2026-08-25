using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Librariann.API.Services;

public interface IBackupService
{
    public const string LogFile = "config/logs/librariann.log";

    Task BackupDatabase(CancellationToken ct = default);
    /// <summary>
    /// Returns a list of all log files for Librariann
    /// </summary>
    /// <param name="rollFiles">If file rolling is enabled. Defaults to True.</param>
    /// <returns></returns>
    IEnumerable<string> GetLogFiles(bool rollFiles = true);
}
