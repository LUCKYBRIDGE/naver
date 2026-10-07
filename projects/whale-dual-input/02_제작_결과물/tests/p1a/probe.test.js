import test from 'node:test';
import assert from 'node:assert/strict';
import { FIXTURE_URL, runProbe, validateTarget, createCommands } from './probe/engine.js';

const point = { x: 50, y: 60 };
const snapshot = {
  documentId: 'doc-a', viewport: { width: 800, height: 600 },
  points: { click: point, drag: point, scroll: point, input: point, textarea: point }
};
function harness() {
  const calls = [];
  let current = { id: 7, url: FIXTURE_URL };
  const api = {
    getTab: async () => current,
    attach: async id => calls.push(['attach', id]),
    detach: async id => calls.push(['detach', id]),
    command: async (id, method, params) => {
      calls.push([method, id, params]);
      return method === 'Runtime.evaluate' ? { result: { value: snapshot } } : {};
    }
  };
  return { api, calls, setTab: tab => { current = tab; } };
}

test('only the exact loopback fixture and integer tab ID are accepted', () => {
  assert.equal(validateTarget({ id: 0, url: FIXTURE_URL }), 0);
  for (const url of ['https://example.com', FIXTURE_URL + '?q=1', FIXTURE_URL + '#x', 'http://localhost:8765/p1a.html']) {
    assert.throws(() => validateTarget({ id: 7, url }));
  }
  for (const id of [-1, NaN, 1.5, '7', undefined]) {
    assert.throws(() => validateTarget({ id, url: FIXTURE_URL }));
  }
});

test('fixed suite includes Korean, Backspace and Enter without foreground commands', () => {
  const commands = createCommands(snapshot);
  assert.ok(commands.some(c => c.method === 'Input.insertText' && c.params.text === '한글 ABC 123'));
  assert.ok(commands.some(c => c.params.key === 'Backspace'));
  assert.ok(commands.some(c => c.params.key === 'Enter'));
  assert.ok(commands.every(c => c.method.startsWith('Input.')));
  assert.throws(() => createCommands({ ...snapshot, points: { ...snapshot.points, click: { x: -1, y: 0 } } }));
});

test('selected tab remains fixed and debugger is released after completion', async () => {
  const h = harness();
  const report = await runProbe(h.api, 7, { wait: async () => {} });
  assert.equal(report.teacherFocus, 'UNVERIFIED');
  assert.equal(report.before.documentId, 'doc-a');
  assert.equal(h.calls[0][0], 'attach');
  assert.equal(h.calls.at(-1)[0], 'detach');
  assert.ok(h.calls.every(c => c[1] === 7));
});

test('navigation during delay prevents input and releases debugger', async () => {
  const h = harness();
  await assert.rejects(runProbe(h.api, 7, {
    wait: async () => h.setTab({ id: 7, url: 'https://example.com' })
  }));
  assert.ok(!h.calls.some(c => c[0].startsWith('Input.')));
  assert.equal(h.calls.at(-1)[0], 'detach');
});

test('same URL reload changes document token and prevents input', async () => {
  const h = harness();
  let reads = 0;
  const command = h.api.command;
  h.api.command = async (...args) => {
    const result = await command(...args);
    if (args[1] === 'Runtime.evaluate' && ++reads > 1) {
      return { result: { value: { ...snapshot, documentId: 'doc-b' } } };
    }
    return result;
  };
  await assert.rejects(runProbe(h.api, 7, { wait: async () => {} }), /문서/);
  assert.ok(!h.calls.some(c => c[0].startsWith('Input.')));
});

test('cancel before attach sends nothing', async () => {
  const h = harness();
  const controller = new AbortController();
  controller.abort();
  await assert.rejects(runProbe(h.api, 7, { signal: controller.signal }));
  assert.equal(h.calls.length, 0);
});

test('command failure releases debugger and stops subsequent commands', async () => {
  const h = harness();
  const command = h.api.command;
  h.api.command = async (...args) => {
    if (args[1].startsWith('Input.')) throw new Error('CDP failed');
    return command(...args);
  };
  await assert.rejects(runProbe(h.api, 7, { wait: async () => {} }), /CDP failed/);
  assert.equal(h.calls.at(-1)[0], 'detach');
});

test('attach failure does not detach another debugger session', async () => {
  const h = harness();
  h.api.attach = async () => { throw new Error('occupied'); };
  await assert.rejects(runProbe(h.api, 7, { wait: async () => {} }), /occupied/);
  assert.equal(h.calls.length, 0);
});

test('cancel during a pressed pointer releases it in the same document', async () => {
  const h = harness();
  const controller = new AbortController();
  const command = h.api.command;
  h.api.command = async (...args) => {
    const result = await command(...args);
    if (args[2].type === 'mousePressed') controller.abort();
    return result;
  };
  await assert.rejects(runProbe(h.api, 7, { signal: controller.signal, wait: async () => {} }));
  assert.equal(h.calls.filter(c => c[2]?.type === 'mouseReleased').length, 1);
  assert.equal(h.calls.at(-1)[0], 'detach');
});

test('navigation after pointer down never sends release into a foreign page', async () => {
  const h = harness();
  const command = h.api.command;
  h.api.command = async (...args) => {
    const result = await command(...args);
    if (args[2].type === 'mousePressed') h.setTab({ id: 7, url: 'https://example.com' });
    return result;
  };
  await assert.rejects(runProbe(h.api, 7, { wait: async () => {} }));
  assert.equal(h.calls.filter(c => c[2]?.type === 'mouseReleased').length, 0);
  assert.equal(h.calls.at(-1)[0], 'detach');
});

test('viewport change during delay prevents inputs', async () => {
  const h = harness();
  const command = h.api.command;
  let changed = false;
  h.api.command = async (...args) => {
    const result = await command(...args);
    if (changed && args[1] === 'Runtime.evaluate') {
      return { result: { value: { ...snapshot, viewport: { width: 400, height: 600 } } } };
    }
    return result;
  };
  await assert.rejects(runProbe(h.api, 7, { wait: async () => { changed = true; } }), /좌표/);
  assert.ok(!h.calls.some(c => c[0].startsWith('Input.')));
});

test('cancel after key down releases the key in the same document', async () => {
  const h = harness();
  const controller = new AbortController();
  const command = h.api.command;
  h.api.command = async (...args) => {
    const result = await command(...args);
    if (args[2].type === 'rawKeyDown') controller.abort();
    return result;
  };
  await assert.rejects(runProbe(h.api, 7, { signal: controller.signal, wait: async () => {} }));
  assert.equal(h.calls.filter(c => c[2]?.type === 'keyUp').length, 1);
  assert.equal(h.calls.at(-1)[0], 'detach');
});
