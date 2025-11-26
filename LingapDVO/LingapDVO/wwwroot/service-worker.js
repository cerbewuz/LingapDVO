// Service Worker for LingapDVO - v1.0.0
const CACHE_NAME = 'lingapdvo-cache-v1';
const RUNTIME_CACHE = 'lingapdvo-runtime-v1';

// Resources to cache on install
const PRECACHE_URLS = [
    '/',
    '/css/landingpage.css',
    '/js/landingpage.js',
    '/css/bootstrap.min.css',
    '/js/bootstrap.min.js',
    '/js/jquery-3.3.1.min.js',
    '/Icon/lingaplogo.ico',
    '/images/helping.jpg'
];

// Install event - cache critical resources
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('Service Worker: Precaching resources');
                return cache.addAll(PRECACHE_URLS);
            })
            .then(() => self.skipWaiting())
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames
                    .filter(cacheName => {
                        return cacheName !== CACHE_NAME && cacheName !== RUNTIME_CACHE;
                    })
                    .map(cacheName => {
                        console.log('Service Worker: Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    })
            );
        }).then(() => self.clients.claim())
    );
});

// Fetch event - serve from cache, fallback to network
self.addEventListener('fetch', event => {
    // Skip cross-origin requests
    if (!event.request.url.startsWith(self.location.origin)) {
        return;
    }

    // Skip caching POST, PUT, DELETE requests - only cache GET requests
    if (event.request.method !== 'GET') {
        return;
    }

    // Cache-first strategy for static assets
    if (event.request.url.match(/\.(css|js|jpg|jpeg|png|webp|gif|svg|woff|woff2|ttf|eot|ico)$/)) {
        event.respondWith(
            caches.match(event.request).then(cachedResponse => {
                if (cachedResponse) {
                    return cachedResponse;
                }

                return fetch(event.request).then(response => {
                    // Don't cache if not a success response
                    if (!response || response.status !== 200 || response.type !== 'basic') {
                        return response;
                    }

                    const responseToCache = response.clone();
                    caches.open(RUNTIME_CACHE).then(cache => {
                        cache.put(event.request, responseToCache);
                    });

                    return response;
                });
            })
        );
    }
    // Network-first strategy for HTML pages
    else {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    const responseToCache = response.clone();
                    caches.open(RUNTIME_CACHE).then(cache => {
                        cache.put(event.request, responseToCache);
                    });
                    return response;
                })
                .catch(() => {
                    return caches.match(event.request);
                })
        );
    }
});

// Handle messages from clients
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
