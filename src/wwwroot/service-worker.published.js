// 최소한의 오프라인 캐싱 전략입니다.
// 더 정교한 캐싱(버전 관리, 프리캐시 목록)이 필요하면
// 공식 Blazor PWA 템플릿(dotnet new blazorwasm --pwa)의 service-worker.published.js를 참고해서 교체하세요.

const CACHE_NAME = 'wonday-cache-v1';

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', (event) => {
  event.respondWith(
    fetch(event.request)
      .then((response) => {
        const clone = response.clone();
        caches.open(CACHE_NAME).then((cache) => cache.put(event.request, clone));
        return response;
      })
      .catch(() => caches.match(event.request))
  );
});
