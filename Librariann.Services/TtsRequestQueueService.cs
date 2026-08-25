using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Librariann.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

/// <summary>
/// Backing implementation for <see cref="ITtsRequestQueueService"/> - see that interface's doc comment for why
/// this exists. An unbounded <see cref="Channel{T}"/> feeds a single background consumer loop that calls
/// <see cref="IKokoroTtsService"/> one job at a time, strictly FIFO. Registered as both a singleton (so
/// <see cref="ITtsRequestQueueService.EnqueueSynthesisAsync"/> is reachable from controllers) and a hosted
/// service (so the consumer loop runs) pointing at the same instance - the same pattern already used for
/// <see cref="ActiveUserTrackerService"/> in ApplicationServiceExtensions.
/// </summary>
/// <remarks>
/// Takes <see cref="IServiceScopeFactory"/> rather than <see cref="IKokoroTtsService"/> directly: this class is
/// a Singleton (it has to be - the queue and its one consumer loop must outlive any single request), but
/// <see cref="IKokoroTtsService"/> is Scoped (it depends on the Scoped IUnitOfWork). A Singleton cannot
/// constructor-inject a Scoped service - ASP.NET Core's Development-mode DI validation rejects that exact
/// mismatch at startup (same class of bug already hit once this session with
/// ITrustedMetadataHttpClientFactory). Creating one short-lived scope per dequeued job is the standard fix.
/// </remarks>
public sealed class TtsRequestQueueService(IServiceScopeFactory scopeFactory, ILogger<TtsRequestQueueService> logger)
    : BackgroundService, ITtsRequestQueueService
{
    private sealed record QueueItem(string Text, string? VoiceId, double Speed,
        TaskCompletionSource<KokoroSynthesisResult?> Completion, CancellationToken RequestCt);

    // Unbounded and single-reader: every enqueue succeeds immediately (a request is never rejected/dropped for
    // being "too busy"), and there's exactly one consumer loop processing jobs strictly in arrival order.
    private readonly Channel<QueueItem> _channel = Channel.CreateUnbounded<QueueItem>(
        new UnboundedChannelOptions {SingleReader = true, SingleWriter = false});

    public Task<KokoroSynthesisResult?> EnqueueSynthesisAsync(string text, string? voiceId, double speed, CancellationToken ct = default)
    {
        // RunContinuationsAsynchronously: without it, TrySetResult() below would run the awaiting controller's
        // continuation synchronously on the consumer loop's thread, delaying the next dequeue for everyone else
        // behind it in line.
        var tcs = new TaskCompletionSource<KokoroSynthesisResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var item = new QueueItem(text, voiceId, speed, tcs, ct);

        // If the caller's own HTTP request is aborted while this job is still sitting in the queue (they
        // navigated away, hit stop), unblock their await immediately rather than making them wait for a result
        // nobody's listening for anymore. The consumer loop below skips already-completed items when it gets to
        // them, so an abandoned job never reaches Kokoro at all - it doesn't just get discarded after synthesis.
        ct.Register(() => tcs.TrySetCanceled(ct));

        if (!_channel.Writer.TryWrite(item))
        {
            // Unbounded channel - TryWrite only fails after Writer.Complete() is called, which this class never
            // does outside of process shutdown. Treat as "Kokoro unavailable" rather than throwing.
            tcs.TrySetResult(null);
        }

        return tcs.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                if (item.Completion.Task.IsCompleted) continue; // Caller already gave up while queued - skip it.

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var kokoroTtsService = scope.ServiceProvider.GetRequiredService<IKokoroTtsService>();
                    var result = await kokoroTtsService.SynthesizeAsync(item.Text, item.VoiceId, item.Speed, item.RequestCt);
                    item.Completion.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    item.Completion.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    // IKokoroTtsService.SynthesizeAsync already catches its own request failures and returns
                    // null - this is a defensive backstop so one unexpected exception can never take the whole
                    // consumer loop down and stop synthesis for every other queued/future request.
                    logger.LogWarning(ex, "Queued Kokoro TTS synthesis job failed unexpectedly");
                    item.Completion.TrySetResult(null);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }
}
