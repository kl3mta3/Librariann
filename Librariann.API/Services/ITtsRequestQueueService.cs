using System.Threading;
using System.Threading.Tasks;

namespace Librariann.API.Services;

/// <summary>
/// Serializes all Kokoro TTS synthesis requests through a single in-process FIFO queue before they reach
/// <see cref="IKokoroTtsService"/>, instead of letting every incoming request call Kokoro directly and
/// concurrently. Two reasons this exists as its own layer rather than relying on Kokoro's own concurrency gate
/// (Librariann-Kokoro-Server's `Concurrency:MaxParallelSynthesis` semaphore):
/// <list type="bullet">
/// <item>A burst of requests (many users listening at once) waits in an unbounded queue instead of piling up as
/// concurrent outbound HTTP calls Librariann's own process has to juggle - smoother under load, same guarantee
/// regardless of how Kokoro itself is configured or which Kokoro server implementation is pointed at.</item>
/// <item>A request is never dropped/lost because of load - it waits its turn. If the caller's own HTTP request
/// is cancelled (they navigated away, stopped playback) while still queued, its task is cancelled without ever
/// reaching Kokoro, but the queue itself keeps draining for everyone else.</item>
/// </list>
/// Each caller gets back its own result via its own awaited <see cref="System.Threading.Tasks.Task"/> - there is
/// no shared/reusable buffer a response could be read from by the wrong caller, so queuing doesn't introduce any
/// cross-request mixing risk (verified live: see the two-concurrent-request test in this session's history).
/// </summary>
public interface ITtsRequestQueueService
{
    /// <summary>
    /// Enqueues a synthesis job and returns the same <see cref="Librariann.API.Services.KokoroSynthesisResult"/>
    /// (or null) that a direct <see cref="IKokoroTtsService.SynthesizeAsync"/> call would have returned - queuing
    /// is an internal implementation detail, not something callers need to branch on.
    /// </summary>
    Task<KokoroSynthesisResult?> EnqueueSynthesisAsync(string text, string? voiceId, double speed, CancellationToken ct = default);
}
