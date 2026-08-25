const SHELL_CACHE = 'librariann-shell-v1';
const SHELL_FILES = ['./', './site.webmanifest', './assets/icons/android-chrome-192x192.png',
  './assets/icons/android-chrome-256x256.png'];

self.addEventListener('install', event => {
  event.waitUntil(caches.open(SHELL_CACHE).then(cache => cache.addAll(SHELL_FILES)));
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(
    keys.filter(key => key.startsWith('librariann-shell-') && key !== SHELL_CACHE)
      .map(key => caches.delete(key)))));
  self.clients.claim();
});

self.addEventListener('fetch', event => {
  const request = event.request;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  if (url.origin !== self.location.origin || url.pathname.includes('/api/') || url.pathname.includes('/hubs/')) return;

  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).then(response => {
      const copy = response.clone();
      caches.open(SHELL_CACHE).then(cache => cache.put('./', copy));
      return response;
    }).catch(() => caches.match('./')));
    return;
  }

  if (['script', 'style', 'font', 'image'].includes(request.destination)) {
    event.respondWith(caches.match(request).then(cached => cached || fetch(request).then(response => {
      if (response.ok) caches.open(SHELL_CACHE).then(cache => cache.put(request, response.clone()));
      return response;
    })));
  }
});
