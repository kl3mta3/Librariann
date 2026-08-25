import {BookPaperTheme} from './book-paper-theme';

// Reuse the mature light-reader rules, then replace only the reading surface.
export const BookBambooTheme = `
${BookPaperTheme.replaceAll('brtheme-paper', 'brtheme-bamboo')}

:root .brtheme-bamboo {
  --theme-bg-color: #d8c38f;
  --drawer-bg-color: #d8c38f;
  --accordion-surface-bg-color: #d8c38f;
  --br-actionbar-bg-color: #d8c38f;
}

.brtheme-bamboo .reader-container {
  background-color: color-mix(in srgb, #d8c38f var(--reader-background-opacity, 100%), transparent) !important;
  background-image:
    repeating-linear-gradient(90deg, rgba(84, 65, 28, 0.10) 0 1px, transparent 1px 42px),
    repeating-linear-gradient(0deg, rgba(255, 255, 255, 0.08) 0 2px, rgba(99, 77, 31, 0.04) 2px 5px) !important;
  background-blend-mode: multiply, soft-light;
}
`;
