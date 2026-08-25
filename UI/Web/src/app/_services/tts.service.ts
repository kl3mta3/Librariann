import {computed, inject, Injectable, signal} from '@angular/core';
import {BrowserSpeechTtsProvider} from './browser-speech-tts.provider';
import {KokoroTtsProvider} from './kokoro-tts.provider';
import {TtsProvider, TtsVoice} from '../_models/tts/tts-provider';

export type TtsPlaybackState = 'idle' | 'playing' | 'paused';
export type TtsProviderId = 'browser' | 'kokoro';

interface TtsSegment {
  text: string;
  paragraphIndex: number;
  sentenceIndex: number;
  element: HTMLElement;
}

@Injectable({providedIn: 'root'})
export class TtsService {
  private readonly browserProvider = inject(BrowserSpeechTtsProvider);
  private readonly kokoroProvider = inject(KokoroTtsProvider);
  private segments: TtsSegment[] = [];
  private generation = 0;
  private highlightedElement?: HTMLElement;

  /**
   * Which TtsProvider implementation actually does the speaking - the "device" TTS (browser SpeechSynthesis) or
   * the Kokoro server-proxy. Not persisted anywhere yet (defaults to 'browser' each session) - a real
   * preference field is a follow-up once Kokoro has a real server to point at; the provider seam itself is what
   * matters for now. Changing it takes effect on the next speakCurrent() call, same as any other setting here.
   */
  readonly providerId = signal<TtsProviderId>('browser');
  private readonly provider = computed<TtsProvider>(() =>
    this.providerId() === 'kokoro' ? this.kokoroProvider : this.browserProvider);

  readonly isSupported = computed(() => this.provider().isSupported);
  readonly state = signal<TtsPlaybackState>('idle');
  readonly voices = signal<TtsVoice[]>([]);
  readonly voiceId = signal<string | undefined>(undefined);
  readonly rate = signal(1);
  readonly pitch = signal(1);
  readonly volume = signal(1);
  readonly currentIndex = signal(-1);
  readonly currentText = computed(() => this.segments[this.currentIndex()]?.text ?? '');
  readonly progressLabel = computed(() => {
    const segment = this.segments[this.currentIndex()];
    return segment ? `${segment.paragraphIndex + 1}.${segment.sentenceIndex + 1}` : '';
  });

  constructor() {
    this.refreshVoices();
    this.browserProvider.onVoicesChanged(() => this.refreshVoices());
    this.kokoroProvider.onVoicesChanged(() => this.refreshVoices());
  }

  /** Switches which TtsProvider actually speaks - see the `providerId` doc comment. */
  setProvider(id: TtsProviderId): void {
    if (this.providerId() === id) return;
    this.stop();
    this.providerId.set(id);
    // Without this, refreshVoices()'s "only pick a default if none is set" guard leaves whatever voice ID the
    // *previous* provider was using in place - e.g. switching from Device TTS to Kokoro would keep sending a
    // browser voice name like "Microsoft David - English (United States)" straight to Kokoro, which doesn't
    // recognize it and silently falls back to its own default voice instead of the one actually selected.
    this.voiceId.set(undefined);
    this.refreshVoices();
  }

  listen(root: HTMLElement): void {
    if (!this.isSupported()) return;
    this.segments = this.extractSegments(root);
    if (this.segments.length === 0) return;
    this.currentIndex.set(0);
    this.speakCurrent();
  }

  pause(): void {
    if (this.state() !== 'playing') return;
    this.provider().pause();
    this.state.set('paused');
  }

  resume(): void {
    if (this.state() !== 'paused') return;
    this.provider().resume();
    this.state.set('playing');
  }

  stop(): void {
    this.generation++;
    this.provider().stop();
    this.state.set('idle');
    this.currentIndex.set(-1);
    this.clearHighlight();
  }

  nextSentence(): void {
    this.moveTo(Math.min(this.currentIndex() + 1, this.segments.length - 1));
  }

  previousSentence(): void {
    this.moveTo(Math.max(this.currentIndex() - 1, 0));
  }

  nextParagraph(): void {
    const current = this.segments[this.currentIndex()];
    if (!current) return;
    const next = this.segments.findIndex((segment, index) => index > this.currentIndex() && segment.paragraphIndex > current.paragraphIndex);
    if (next >= 0) this.moveTo(next);
  }

  previousParagraph(): void {
    const current = this.segments[this.currentIndex()];
    if (!current) return;
    for (let index = this.currentIndex() - 1; index >= 0; index--) {
      if (this.segments[index].paragraphIndex < current.paragraphIndex) {
        const paragraphIndex = this.segments[index].paragraphIndex;
        this.moveTo(this.segments.findIndex(segment => segment.paragraphIndex === paragraphIndex));
        return;
      }
    }
  }

  setVoice(id: string): void {
    this.voiceId.set(id || undefined);
    this.restartIfActive();
  }

  setRate(value: number): void {
    this.rate.set(value);
    this.restartIfActive();
  }

  setPitch(value: number): void {
    this.pitch.set(value);
    this.restartIfActive();
  }

  setVolume(value: number): void {
    this.volume.set(value);
    this.restartIfActive();
  }

  private refreshVoices(): void {
    const voices = this.provider().getVoices();
    this.voices.set(voices);
    if (!this.voiceId() && voices.length > 0) {
      this.voiceId.set((voices.find(voice => voice.isDefault) ?? voices[0]).id);
    }
  }

  private moveTo(index: number): void {
    if (index < 0 || index >= this.segments.length) return;
    this.currentIndex.set(index);
    this.speakCurrent();
  }

  private restartIfActive(): void {
    if (this.state() !== 'idle') this.speakCurrent();
  }

  private speakCurrent(): void {
    const segment = this.segments[this.currentIndex()];
    if (!segment) {
      this.stop();
      return;
    }

    const generation = ++this.generation;
    this.provider().stop();
    this.highlight(segment.element);
    this.state.set('playing');
    this.provider().speak(segment.text, {
      voiceId: this.voiceId(),
      rate: this.rate(),
      pitch: this.pitch(),
      volume: this.volume(),
      onEnd: () => {
        if (generation !== this.generation) return;
        if (this.currentIndex() + 1 >= this.segments.length) {
          this.stop();
          return;
        }
        this.currentIndex.update(index => index + 1);
        this.speakCurrent();
      },
      onError: () => {
        if (generation === this.generation) this.stop();
      },
    });
  }

  private extractSegments(root: HTMLElement): TtsSegment[] {
    const elements = Array.from(root.querySelectorAll<HTMLElement>('h1, h2, h3, h4, h5, h6, p, li, blockquote'))
      .filter(element => element.innerText.trim().length > 0)
      .filter(element => !element.querySelector('h1, h2, h3, h4, h5, h6, p, li, blockquote'));
    const paragraphs = elements.length > 0 ? elements : [root];
    const segments: TtsSegment[] = [];

    paragraphs.forEach((element, paragraphIndex) => {
      this.splitSentences(element.innerText).forEach((text, sentenceIndex) => {
        segments.push({text, paragraphIndex, sentenceIndex, element});
      });
    });
    return segments;
  }

  // EPUB text almost always uses smart/curly punctuation (U+2018-201F, U+2013/2014, U+2026) - Kokoro's (and to a
  // lesser extent the browser's) phonemizer doesn't reliably recognize a curly apostrophe as part of a
  // contraction, so "can't" (with U+2019) gets read as "can" + "t" ("can-tee"/"canty") instead of one word.
  // Normalizing to plain ASCII equivalents before synthesis fixes this regardless of which provider is active.
  private static readonly SMART_PUNCTUATION: ReadonlyArray<[RegExp, string]> = [
    [/[‘’‚‛]/g, "'"],
    [/[“”„‟]/g, '"'],
    [/[–—]/g, '-'],
    [/…/g, '...'],
  ];

  private normalizeForSpeech(text: string): string {
    return TtsService.SMART_PUNCTUATION.reduce((acc, [pattern, replacement]) => acc.replace(pattern, replacement), text);
  }

  private splitSentences(text: string): string[] {
    const normalized = this.normalizeForSpeech(text).replace(/\s+/g, ' ').trim();
    if (!normalized) return [];
    const Segmenter = (Intl as unknown as {Segmenter?: new (locale?: string, options?: {granularity: string}) => {segment(value: string): Iterable<{segment: string}>}}).Segmenter;
    if (Segmenter) {
      return Array.from(new Segmenter(undefined, {granularity: 'sentence'}).segment(normalized))
        .map(item => item.segment.trim())
        .filter(Boolean);
    }
    return normalized.match(/[^.!?]+(?:[.!?]+|$)/g)?.map(sentence => sentence.trim()).filter(Boolean) ?? [normalized];
  }

  private highlight(element: HTMLElement): void {
    this.clearHighlight();
    this.highlightedElement = element;
    element.classList.add('librariann-tts-speaking');
    element.scrollIntoView({block: 'center', behavior: 'smooth'});
  }

  private clearHighlight(): void {
    this.highlightedElement?.classList.remove('librariann-tts-speaking');
    this.highlightedElement = undefined;
  }
}
