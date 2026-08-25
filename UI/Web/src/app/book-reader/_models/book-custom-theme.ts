import {BookPaperTheme} from './book-paper-theme';

export const BookCustomTheme = `
${BookPaperTheme.replaceAll('brtheme-paper', 'brtheme-custom')}

:root .brtheme-custom {
  --theme-bg-color: var(--reader-background-color, #f1e4d5);
  --drawer-bg-color: var(--reader-background-color, #f1e4d5);
  --drawer-text-color: var(--reader-text-color, #17191d);
  --body-text-color: var(--reader-text-color, #17191d);
  --bs-body-color: var(--reader-text-color, #17191d);
  --accordion-surface-bg-color: var(--reader-background-color, #f1e4d5);
  --br-actionbar-bg-color: var(--reader-background-color, #f1e4d5);
  --br-actionbar-button-text-color: var(--reader-text-color, #17191d);
}

.brtheme-custom .reader-container {
  background-image: none !important;
  background-color: color-mix(in srgb, var(--reader-background-color, #f1e4d5) var(--reader-background-opacity, 100%), transparent) !important;
}

.brtheme-custom .book-content,
.brtheme-custom .book-content *:not(img):not(svg):not(image) {
  color: var(--reader-text-color, #17191d) !important;
  background-color: transparent !important;
  text-shadow: none !important;
}

.brtheme-custom .book-content a,
.brtheme-custom .book-content :link {
  color: var(--reader-link-color, #174f91) !important;
}
`;
