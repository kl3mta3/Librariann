import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import {environment} from '../../environments/environment';
import {TtsPlaybackOptions, TtsProvider, TtsVoice} from '../_models/tts/tts-provider';

/**
 * Server-proxied TTS provider - the Kokoro counterpart to BrowserSpeechTtsProvider (the "device" TTS). Same
 * TtsProvider contract, so TtsService's chunking/sentence-splitting/highlighting logic needs no changes at all;
 * only which provider gets used for playback differs. `speak()` posts the chunk to Librariann's own backend
 * (`POST reader/tts/synthesize`), which forwards it to the configured Kokoro server and returns audio bytes -
 * Kokoro itself never needs to be reachable by the browser, only by the Librariann backend. See
 * docs/kokoro-tts-integration.md for the full contract and rationale.
 */
@Injectable({providedIn: 'root'})
export class KokoroTtsProvider implements TtsProvider {
  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  readonly id = 'kokoro';
  // Always true - unlike BrowserSpeechTtsProvider's synchronous SpeechSynthesis check, whether Kokoro is
  // actually configured/reachable is only knowable from the backend, discovered lazily on first speak()/
  // getVoices() call rather than blocking construction on a settings round-trip.
  readonly isSupported = true;

  private audio: HTMLAudioElement | null = null;
  private voicesCache: TtsVoice[] | null = null;
  private voicesChangedCallback: (() => void) | null = null;
  // Bumped by stop() (and by speak() itself, superseding whatever came before) so a synthesis request
  // still in flight when the user stops/moves on can be told, once it finally resolves, that it's
  // stale - without this, a sentence already stopped/paused could still land and auto-play seconds
  // later purely because the network request for it happened to still be pending.
  private requestId = 0;
  // Set by pause() so a request that resolves while paused doesn't ignore the guard above (stop()
  // wasn't called, so requestId is still current) but also doesn't just start playing anyway - the
  // audio is prepared and held ready for resume() instead.
  private pausedWhilePending = false;

  getVoices(): TtsVoice[] {
    // Populated lazily - see refreshVoices(). Empty until the first fetch resolves; TtsService's voice list
    // will simply be empty for one tick, same as BrowserSpeechTtsProvider before 'voiceschanged' first fires.
    if (this.voicesCache === null) {
      this.voicesCache = [];
      this.refreshVoices();
    }
    return this.voicesCache;
  }

  onVoicesChanged(callback: () => void): () => void {
    // Unlike browser voices (an OS-level 'voiceschanged' event), Kokoro's list only ever changes on our own
    // fetch resolving - stash the callback and fire it once refreshVoices()'s request comes back.
    this.voicesChangedCallback = callback;
    return () => { this.voicesChangedCallback = null; };
  }

  speak(text: string, options: TtsPlaybackOptions): void {
    this.stop();
    const requestId = ++this.requestId;

    firstValueFrom(this.httpClient.post(`${this.baseUrl}reader/tts/synthesize`,
      {text, voiceId: options.voiceId, speed: options.rate},
      {responseType: 'blob'},
    )).then(blob => {
      // stop() (or a newer speak() call) superseded this request while it was still in flight - the
      // audio we just got back is for a sentence that's no longer current, so discard it instead of
      // playing it regardless.
      if (requestId !== this.requestId) return;

      const url = URL.createObjectURL(blob as Blob);
      const audio = new Audio(url);
      this.audio = audio;
      audio.volume = options.volume;
      audio.onended = () => { URL.revokeObjectURL(url); options.onEnd(); };
      audio.onerror = () => { URL.revokeObjectURL(url); options.onError(); };
      // Paused while this was still downloading - hold it ready (so resume() has something to play)
      // rather than starting playback out from under a paused state.
      if (this.pausedWhilePending) return;
      audio.play().catch(() => options.onError());
    }).catch(() => options.onError());
  }

  pause(): void {
    this.pausedWhilePending = true;
    this.audio?.pause();
  }

  resume(): void {
    this.pausedWhilePending = false;
    this.audio?.play().catch(() => {});
  }

  stop(): void {
    this.requestId++;
    this.pausedWhilePending = false;
    if (!this.audio) return;
    this.audio.pause();
    this.audio.removeAttribute('src');
    this.audio = null;
  }

  private refreshVoices(): void {
    firstValueFrom(this.httpClient.get<string[]>(`${this.baseUrl}reader/tts/voices`)).then(voices => {
      this.voicesCache = voices.map(id => ({id, name: id, language: '', isDefault: id === voices[0]}));
      this.voicesChangedCallback?.();
    }).catch(() => {
      // Reset to null (not []) on failure so the next getVoices() call retries instead of being stuck empty
      // forever - a transient failure (Kokoro not reachable yet, endpoint not configured yet) shouldn't
      // permanently poison the cache once the real cause is fixed.
      this.voicesCache = null;
    });
  }
}
