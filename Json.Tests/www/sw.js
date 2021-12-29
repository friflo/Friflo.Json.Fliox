
self.addEventListener("install", event => {
    console.log("install");
    event.waitUntil(
        caches.open("static").then(cache => {
            return cache.addAll([
                "/",
                "/bundle.js",
                "/index.js",
                "/Json-Fliox-53x43.svg",
                "/fliox-512x512.png"
            ])
        })
    )
});


self.addEventListener("fetch", event => {
    console.log(`intercept fetch request ${event.request.url}`);
});