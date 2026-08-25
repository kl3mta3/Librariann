/**
 * Converts a pre-foliate-js annotation's XPath/EndingXPath/SelectedText anchor into a DOM Range within a
 * foliate-js section document, so it can be turned into a CFI via `CFI.fromRange()`. Faithfully ports
 * `Librariann.Services/Helpers/AnnotationHelper.cs`'s server-side resolution logic (which the *old* reader uses
 * to inject `<app-epub-highlight>` spans into HtmlAgilityPack-parsed HTML) to the browser's native DOM/XPath
 * APIs, rather than reimplementing EPUB CFI generation itself - foliate-js already does that correctly.
 *
 * Two cases, matching `BookController`/`BookService`'s existing split (`XPath === EndingXPath` => single
 * element, spans multiple sibling block elements otherwise):
 * - Single element: the whole highlight lives inside one element's text.
 * - Multi element: the highlight spans consecutive sibling block elements (the old reader has to inject a
 *   separate `<app-epub-highlight>` per element for this; a DOM Range naturally spans elements on its own, and
 *   `CFI.fromRange()` produces one composite range-CFI covering it - no need to mirror the per-element split).
 */

const INLINE_TAGS = new Set(['em', 'strong', 'i', 'b', 'span', 'a', 'cite']);
const ID_XPATH_PATTERN = /^id\("([^"]+)"\)$/;

interface TextPoint {
  node: Node;
  offset: number;
}

const XPATH_SEGMENT_PATTERN = /^([a-zA-Z][a-zA-Z0-9]*)(\[\d+\])?$/;

/**
 * foliate-js parses EPUB sections as real XHTML (`doc.contentType === 'application/xhtml+xml'`), which puts
 * every element in the XHTML namespace - a plain, unprefixed XPath like `//body/section[1]/p[4]` (what's
 * actually stored, generated against a namespace-less HTML parse by the old reader) silently matches nothing
 * against a namespaced document without an explicit resolver. Rewriting each tag-name segment to
 * `*[local-name()='tag']` sidesteps needing a real resolver and matches regardless of namespace.
 */
function toNamespaceAgnosticXPath(xpath: string): string {
  return xpath
    .split('/')
    .map(segment => {
      const match = XPATH_SEGMENT_PATTERN.exec(segment);
      return match ? `*[local-name()='${match[1]}']${match[2] ?? ''}` : segment;
    })
    .join('/');
}

/** Ports `AnnotationHelper.FindElementByXPath` - the id(...) shortcut first, native XPath otherwise. */
function findElementByXPath(doc: Document, xpath: string): Element | null {
  const idMatch = ID_XPATH_PATTERN.exec(xpath);
  if (idMatch) return doc.getElementById(idMatch[1]);
  try {
    const result = doc.evaluate(toNamespaceAgnosticXPath(xpath), doc, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    return result.singleNodeValue as Element | null;
  } catch {
    return null;
  }
}

/** Ports `AnnotationHelper.NormalizeToBlockElement` - break out of inline tags to their enclosing block. */
function normalizeToBlockElement(node: Element | null): Element | null {
  while (node && INLINE_TAGS.has(node.tagName.toLowerCase())) {
    node = node.parentElement;
  }
  return node;
}

/** Ports `AnnotationHelper.GetElementsInRange` - walks element siblings from start to end, inclusive. */
function getElementsInRange(start: Element, end: Element): Element[] {
  if (start === end) return [start];
  const elements: Element[] = [];
  let current: Element | null = start;
  while (current && current !== end) {
    elements.push(current);
    current = current.nextElementSibling;
  }
  if (current === end) elements.push(end);
  return elements;
}

function normalizeWhitespace(text: string): string {
  return text.trim().replace(/\s+/g, ' ');
}

/**
 * Ports `AnnotationHelper.MapNormalizedPositionToOriginal` exactly - maps a character offset in
 * whitespace-collapsed text back to the corresponding offset in the original (un-collapsed) text.
 */
function mapNormalizedPositionToOriginal(originalText: string, normalizedPosition: number): number {
  const normalizedText = normalizeWhitespace(originalText);
  if (normalizedPosition >= normalizedText.length) return originalText.length;

  let originalPos = 0;
  let normalizedPos = 0;

  while (originalPos < originalText.length && /\s/.test(originalText[originalPos])) originalPos++;

  while (originalPos < originalText.length && normalizedPos < normalizedPosition) {
    if (/\s/.test(originalText[originalPos])) {
      while (originalPos < originalText.length && /\s/.test(originalText[originalPos])) originalPos++;
    } else {
      originalPos++;
    }
    normalizedPos++;
  }

  return originalPos;
}

/** Walks the text nodes under `root` to find the (node, local offset) boundary point at a flat character offset. */
function resolveTextOffset(root: Node, targetOffset: number): TextPoint | null {
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
  let consumed = 0;
  let node: Node | null;
  while ((node = walker.nextNode())) {
    const len = node.textContent?.length ?? 0;
    if (consumed + len >= targetOffset) {
      return {node, offset: targetOffset - consumed};
    }
    consumed += len;
  }
  return null;
}

function buildRangeForSingleElement(doc: Document, xpath: string, selectedText: string): Range | null {
  const elem = findElementByXPath(doc, xpath);
  if (!elem) return null;

  const originalText = elem.textContent ?? '';
  const startPos = normalizeWhitespace(originalText).indexOf(normalizeWhitespace(selectedText));
  if (startPos < 0) return null;

  const realStart = mapNormalizedPositionToOriginal(originalText, startPos);
  const realEnd = mapNormalizedPositionToOriginal(originalText, startPos + selectedText.length);

  const startPoint = resolveTextOffset(elem, realStart);
  const endPoint = resolveTextOffset(elem, realEnd);
  if (!startPoint || !endPoint) return null;

  const range = doc.createRange();
  range.setStart(startPoint.node, startPoint.offset);
  range.setEnd(endPoint.node, endPoint.offset);
  return range;
}

function buildRangeForMultiElement(doc: Document, startXPath: string, endXPath: string, selectedText: string): Range | null {
  const startElem = normalizeToBlockElement(findElementByXPath(doc, startXPath));
  const endElem = normalizeToBlockElement(findElementByXPath(doc, endXPath));
  if (!startElem || !endElem) return null;

  const elements = getElementsInRange(startElem, endElem);
  if (elements.length === 0) return null;

  const fullText = elements.map(e => e.textContent ?? '').join('\n\n');
  const selStart = normalizeWhitespace(fullText).indexOf(normalizeWhitespace(selectedText));
  if (selStart < 0) return null;
  const selEnd = selStart + normalizeWhitespace(selectedText).length;

  const origStart = mapNormalizedPositionToOriginal(fullText, selStart);
  const origEnd = mapNormalizedPositionToOriginal(fullText, selEnd);

  let cursor = 0;
  let startPoint: TextPoint | null = null;
  let endPoint: TextPoint | null = null;
  for (const el of elements) {
    const len = (el.textContent ?? '').length;
    const elStart = cursor;
    const elEnd = cursor + len;

    if (!startPoint && origStart >= elStart && origStart <= elEnd) {
      startPoint = resolveTextOffset(el, origStart - elStart);
    }
    if (origEnd >= elStart && origEnd <= elEnd) {
      endPoint = resolveTextOffset(el, origEnd - elStart);
    }

    cursor = elEnd + 2; // '\n\n' separator, matching AnnotationHelper's join
  }

  if (!startPoint || !endPoint) return null;

  const range = doc.createRange();
  range.setStart(startPoint.node, startPoint.offset);
  range.setEnd(endPoint.node, endPoint.offset);
  return range;
}

/**
 * Builds a DOM Range for a legacy annotation's stored XPath/EndingXPath/SelectedText within the given section
 * document (from foliate-js's `renderer.getContents()`). Returns null if the anchor can't be resolved (e.g. the
 * XPath no longer matches - the original HTML this was captured against may have shifted slightly in how
 * foliate-js re-serializes it versus the old server-side HtmlAgilityPack parse). Callers should treat null as
 * "leave this annotation's cfi unset for now, try again another time" rather than an error.
 */
export function buildRangeForAnnotation(doc: Document, xpath: string, endingXpath: string | null, selectedText: string): Range | null {
  const descope = (x: string) => x.replace('//BODY/DIV[1]', '//BODY').toLowerCase();
  const start = descope(xpath);
  const end = endingXpath ? descope(endingXpath) : start;

  return start === end
    ? buildRangeForSingleElement(doc, start, selectedText)
    : buildRangeForMultiElement(doc, start, end, selectedText);
}
