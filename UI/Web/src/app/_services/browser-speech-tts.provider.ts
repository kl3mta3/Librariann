import {DOCUMENT} from '@angular/common';
import {inject, Injectable} from '@angular/core';
import {TtsPlaybackOptions, TtsProvider, TtsVoice} from '../_models/tts/tts-provider';

@Injectable({providedIn: 'root'})
export class BrowserSpeechTtsProvider implements TtsProvider {
  private readonly document = inject(DOCUMENT);
  private readonly synthesis = this.document.defaultView?.speechSynthesis;

  readonly id = 'browser-web-speech';
  readonly isSupported = this.synthesis !== undefined && 'SpeechSynthesisUtterance' in (this.document.defaultView ?? {});

  getVoices(): TtsVoice[] {
    return (this.synthesis?.getVoices() ?? []).map(voice => ({
      id: voice.voiceURI,
      name: voice.name,
      language: voice.lang,
      isDefault: voice.default,
    }));
  }

  onVoicesChanged(callback: () => void): () => void {
    this.synthesis?.addEventListener('voiceschanged', callback);
    return () => this.synthesis?.removeEventListener('voiceschanged', callback);
  }

  speak(text: string, options: TtsPlaybackOptions): void {
    if (!this.synthesis || !this.document.defaultView) {
      options.onError();
      return;
    }

    const utterance = new this.document.defaultView.SpeechSynthesisUtterance(text);
    utterance.rate = options.rate;
    utterance.pitch = options.pitch;
    utterance.volume = options.volume;
    utterance.voice = this.synthesis.getVoices().find(voice => voice.voiceURI === options.voiceId) ?? null;
    utterance.onend = options.onEnd;
    utterance.onerror = options.onError;
    this.synthesis.speak(utterance);
  }

  pause(): void {
    this.synthesis?.pause();
  }

  resume(): void {
    this.synthesis?.resume();
  }

  stop(): void {
    this.synthesis?.cancel();
  }
}
