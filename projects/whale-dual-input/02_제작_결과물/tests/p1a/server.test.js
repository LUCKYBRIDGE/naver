import test from 'node:test';
import assert from 'node:assert/strict';
import { once } from 'node:events';
import { request } from 'node:http';
import { createFixtureServer } from './server.mjs';

test('server exposes only fixed fixture assets on loopback, without repository files', async () => {
  const server = createFixtureServer();
  server.listen(0, '127.0.0.1');
  await once(server, 'listening');
  const root = `http://127.0.0.1:${server.address().port}`;
  try {
    for (const path of ['/p1a.html', '/fixture.js', '/fixture.css']) {
      const response = await fetch(root + path);
      assert.equal(response.status, 200);
      assert.equal(response.headers.get('cache-control'), 'no-store');
      assert.ok((await response.text()).length > 0);
    }
    for (const path of ['/.env', '/probe/worker.js', '/package.json', '/p1a.html?x=1']) {
      assert.equal((await fetch(root + path)).status, 404);
    }
    assert.equal((await fetch(root + '/p1a.html', { method: 'POST' })).status, 405);
    const rejectedHost = await new Promise((resolve, reject) => {
      const req = request(root + '/p1a.html', { headers: { Host: 'evil.example' } }, response => {
        response.resume();
        response.on('end', () => resolve(response.statusCode));
      });
      req.on('error', reject);
      req.end();
    });
    assert.equal(rejectedHost, 403);
    const head = await fetch(root + '/p1a.html', { method: 'HEAD' });
    assert.equal(head.status, 200);
    assert.equal(await head.text(), '');
  } finally { await new Promise(resolve => server.close(resolve)); }
});
