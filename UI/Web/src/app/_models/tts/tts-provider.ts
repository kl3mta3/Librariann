export interface TtsVoice {
  id: string;
  name: string;
  language: string;
  isDefault: boolean;
}

export interface TtsPlaybackOptions {
  voiceId?: string;
  rate: number;
  pitch: number;
  volume: number;
  onEnd: () => void;
  onError: () => void;
}

export interface TtsProvider {
  readonly id: string;
  readonly isSupported: boolean;
  getVoices(): TtsVoice[];
  onVoicesChanged(callback: () => void): () => void;
  speak(text: string, options: TtsPlaybackOptions): void;
  pause(): void;
  resume(): void;
  stop(): void;
}
