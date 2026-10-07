import test from 'node:test';
import assert from 'node:assert/strict';

test('worker rejects foreign senders, fixes target list, and cancels duplicate/active runs', async () => {
  const events = {};
  const event = name => ({ addListener: callback => { events[name] = callback; } });
  const url = 'http://127.0.0.1:8765/p1a.html';
  const panelUrl = 'chrome-extension://test/panel.html';
  const calls = [];
  const api = {
    runtime: { id: 'test', getURL: () => panelUrl, onMessage: event('message') },
    action: { onClicked: event('clicked') },
    tabs: {
      get: (id, done) => done({ id, url }),
      query: (query, done) => done([{ id: 7, windowId: 1, url }, { id: 8, url: 'https://example.com' }]),
      create: () => {}, onUpdated: event('updated'), onRemoved: event('removed')
    },
    debugger: {
      attach: (target, version, done) => { calls.push(['attach', target.tabId]); done(); },
      detach: (target, done) => { calls.push(['detach', target.tabId]); done(); },
      sendCommand: (target, method, params, done) => done({ result: { value: {
        documentId: 'doc', viewport: { width: 800, height: 600 },
        points: Object.fromEntries(['click', 'drag', 'scroll', 'input', 'textarea'].map(id => [id, { x: 50, y: 60 }]))
      } } }),
      onDetach: event('detach')
    }
  };
  globalThis.chrome = api;
  try {
    await import('./probe/worker.js');
    const send = (message, sender = { id: 'test', url: panelUrl }) =>
      new Promise(resolve => events.message(message, sender, resolve));
    assert.match((await send({ type: 'run', tabId: 7 }, { id: 'other', url: panelUrl })).error, /제어 화면/);
    assert.deepEqual(calls, []);
    assert.deepEqual(await send({ type: 'targets' }), [{ id: 7, windowId: 1 }]);
    assert.match((await send({ type: 'arbitrary-method' })).error, /지원하지/);
    assert.equal((await send({ type: 'run', tabId: 7 })).status, 'RUNNING');
    assert.match((await send({ type: 'run', tabId: 8 })).error, /이미/);
    assert.equal((await send({ type: 'stop' })).status, 'STOPPING');
    await new Promise(resolve => setImmediate(resolve));
    assert.equal((await send({ type: 'status' })).status, 'STOPPED');
    assert.deepEqual(calls, [['attach', 7], ['detach', 7]]);
  } finally { delete globalThis.chrome; }
});
