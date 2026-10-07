import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

const routes = new Map([
  ['/p1a.html', ['p1a.html', 'text/html; charset=utf-8']],
  ['/fixture.js', ['fixture.js', 'text/javascript; charset=utf-8']],
  ['/fixture.css', ['fixture.css', 'text/css; charset=utf-8']]
]);
export function createFixtureServer() {
  return createServer(async (request, response) => {
    const expectedHost = `127.0.0.1:${request.socket.localPort}`;
    if (request.headers.host !== expectedHost) {
      response.writeHead(403); response.end('Forbidden'); return;
    }
    if (!['GET', 'HEAD'].includes(request.method)) {
      response.writeHead(405, { Allow: 'GET, HEAD' }); response.end(); return;
    }
    const route = routes.get(request.url);
    if (!route) { response.writeHead(404); response.end('Not found'); return; }
    try {
      const body = await readFile(new URL(route[0], import.meta.url));
      response.writeHead(200, {
        'Content-Type': route[1], 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff',
        'Content-Security-Policy': "default-src 'self'; img-src 'self' data:; connect-src 'none'; frame-ancestors 'none'"
      });
      response.end(request.method === 'HEAD' ? undefined : body);
    } catch { response.writeHead(500); response.end('Fixture unavailable'); }
  });
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const server = createFixtureServer();
  server.on('error', error => {
    console.error(`시험 서버 시작 실패: ${error.code}`); process.exitCode = 1;
  });
  server.listen(8765, '127.0.0.1', () => console.log('P1a 시험 페이지: http://127.0.0.1:8765/p1a.html (Ctrl+C로 종료)'));
}
