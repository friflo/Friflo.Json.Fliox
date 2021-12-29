

// see: [Making PWAs work offline with Service workers - Progressive web apps (PWAs) | MDN]
// https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Offline_Service_workers

const cacheName = 'static-v1';

const contentToCache = [
    "/",
    "/bundle.js",
    "/index.js",
    "/Json-Fliox-53x43.svg",
    "/fliox-512x512.png"
];

self.addEventListener("install",  e => {
    console.log('[Service Worker] Install');
    e.waitUntil((async () => {
      const cache = await caches.open(cacheName);
      console.log('[Service Worker] Caching all: app shell and content');
      await cache.addAll(contentToCache);
    })());
});

self.addEventListener("activate", event => {
    self.clients.claim();
});


self.addEventListener("fetch", e => {
    e.respondWith((async () => {
        const method = e.request.method;
        if (method == "GET") {
            const r = await caches.match(e.request);
            if (r) {
                console.log(`[Service Worker] Cached: GET: ${e.request.url}`);
                return r;
            }
        }
        // console.log(`[Service Worker] Fetch:  ${method}: ${e.request.url}`);
        return await fetch(e.request);
      })());
});