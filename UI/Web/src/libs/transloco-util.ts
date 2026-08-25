/** Responsible for all things Transloco **/
export const translocoPrefixKey = 'transloco--librariann';

export function clearTransloco() {
  localStorage.removeItem('translocoLang');
  localStorage.removeItem('@transloco/translations');
  localStorage.removeItem('@transloco/translations/timestamp');
}
