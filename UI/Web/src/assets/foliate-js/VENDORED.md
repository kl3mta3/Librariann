# foliate-js (vendored)

Source: https://github.com/johnfactotum/foliate-js
Pinned commit: `78914aef4466eb960965702401634c2cb348e9b1` (2026-05-01)
License: MIT (see `LICENSE` in this folder)

This is a partial vendor - only the modules needed for reflowable/fixed-layout **EPUB** rendering, CFI
positions, search, and TTS. Not included (not needed since Librariann has its own PDF/comic readers and no
MOBI/FB2 support): `comic-book.js`, `fb2.js`, `mobi.js`, `pdf.js`, `opds.js`, `dict.js`, `footnotes.js`,
`quote-image.js`, `uri-template.js`, `reader.js`/`reader.html` (their demo app), `ui/`, `vendor/fflate.js`,
`vendor/pdfjs/`. `view.js`'s format-dispatch code for those formats is untouched (still present, dead code for
our usage) - it dynamically `import()`s them only when it detects that file type, which we never feed it, so
their absence here is safe.

Not published on npm - this library is meant to be vendored (git submodule or file copy) and loaded as plain
ES modules with no build step. Do not run this through esbuild/webpack bundling; load `view.js` via a runtime
`import()` of its `/assets/foliate-js/view.js` URL so its own internal relative dynamic imports
(`./epub.js`, `./vendor/zip.js`, `./paginator.js`, etc.) keep resolving correctly against real files.

To update: re-clone the repo at a newer commit, diff against this set, re-copy the same file list, update the
pinned commit hash above.
