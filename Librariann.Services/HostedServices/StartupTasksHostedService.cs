using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.API.Services.Scanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Librariann.Services.HostedServices;

public class StartupTasksHostedService(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var taskScheduler = scope.ServiceProvider.GetRequiredService<ITaskScheduler>();
        await taskScheduler.ScheduleTasks(cancellationToken);
        taskScheduler.ScheduleUpdaterTasks();

        try
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            if ((await unitOfWork.SettingsRepository.GetSettingsDtoAsync(cancellationToken)).EnableFolderWatching)
            {
                var libraryWatcher = scope.ServiceProvider.GetRequiredService<ILibraryWatcher>();
                // Push this off for a bit for people with massive libraries, as it can take up to 45 mins and blocks the thread
                BackgroundJob.Enqueue(() => libraryWatcher.StartWatching());
            }
        }
        catch (Exception)
        {
            // Fail silently
        }

    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
