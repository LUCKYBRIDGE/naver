export const FIXTURE_URL = 'http://127.0.0.1:8765/p1a.html';
export const SNAPSHOT_EXPRESSION = 'window.p1aSnapshot()';

export function validateTarget(tab) {
  if (!Number.isInteger(tab?.id) || tab.id < 0 || tab.url !== FIXTURE_URL) {
    throw new Error('지정된 로컬 시험 탭만 사용할 수 있습니다.');
  }
  return tab.id;
}

export function createCommands(snapshot) {
  const { width, height } = snapshot?.viewport ?? {};
  const check = p => {
    if (![width, height, p?.x, p?.y].every(Number.isFinite) ||
        width <= 0 || height <= 0 || p.x < 0 || p.y < 0 || p.x >= width || p.y >= height) {
      throw new Error('시험 요소가 화면 밖에 있습니다. 창 크기와 스크롤을 확인하세요.');
    }
    return p;
  };
  const points = snapshot?.points ?? {};
  for (const key of ['click', 'drag', 'scroll', 'input', 'textarea']) check(points[key]);
  const mouse = (type, p, extra = {}) => ({
    method: 'Input.dispatchMouseEvent',
    params: { type, ...check(p), ...extra }
  });
  const click = p => [
    mouse('mousePressed', p, { button: 'left', buttons: 1, clickCount: 1 }),
    mouse('mouseReleased', p, { button: 'left', buttons: 0, clickCount: 1 })
  ];
  const key = (name, code, text) => [
    { method: 'Input.dispatchKeyEvent', params: {
      type: text ? 'keyDown' : 'rawKeyDown', key: name, code: name,
      windowsVirtualKeyCode: code, ...(text ? { text, unmodifiedText: text } : {})
    } },
    { method: 'Input.dispatchKeyEvent', params: { type: 'keyUp', key: name, code: name, windowsVirtualKeyCode: code } }
  ];
  const end = { x: points.drag.x + 40, y: points.drag.y + 20 };
  return [
    ...click(points.click),
    mouse('mousePressed', points.drag, { button: 'left', buttons: 1, clickCount: 1 }),
    mouse('mouseMoved', end, { button: 'left', buttons: 1 }),
    mouse('mouseReleased', end, { button: 'left', buttons: 0, clickCount: 1 }),
    mouse('mouseWheel', points.scroll, { deltaX: 0, deltaY: 160 }),
    ...click(points.input),
    { method: 'Input.insertText', params: { text: '한글 ABC 123' } },
    ...key('Backspace', 8),
    ...click(points.textarea),
    { method: 'Input.insertText', params: { text: '한글 ABC 123' } },
    ...key('Backspace', 8),
    ...key('Enter', 13, '\r')
  ];
}

export function delay(ms, signal) {
  return new Promise((resolve, reject) => {
    const aborted = () => { clearTimeout(timer); reject(new Error('시험을 중단했습니다.')); };
    const timer = setTimeout(() => { signal?.removeEventListener('abort', aborted); resolve(); }, ms);
    if (signal?.aborted) aborted();
    else signal?.addEventListener('abort', aborted, { once: true });
  });
}

// Read-only, fixed expression for our fixture. No page-supplied CDP method or script.
export async function runProbe(api, tabId, { signal, wait = delay } = {}) {
  let attached = false;
  let pressed = null;
  let pressedKey = null;
  let before;
  const guard = () => {
    if (signal?.aborted) throw new Error('시험을 중단했습니다.');
  };
  const snapshot = async () => {
    guard();
    const tab = await api.getTab(tabId);
    if (validateTarget(tab) !== tabId) throw new Error('대상 탭이 변경되었습니다.');
    const result = await api.command(tabId, 'Runtime.evaluate', {
      expression: SNAPSHOT_EXPRESSION, returnByValue: true
    });
    const value = result?.result?.value;
    if (result?.exceptionDetails || typeof value?.documentId !== 'string') {
      throw new Error('시험 페이지를 읽지 못했습니다.');
    }
    if (before && (value.documentId !== before.documentId ||
        JSON.stringify(value.viewport) !== JSON.stringify(before.viewport) ||
        JSON.stringify(value.points) !== JSON.stringify(before.points))) {
      throw new Error('문서 또는 좌표가 변경되어 시험을 중단했습니다.');
    }
    return value;
  };
  try {
    guard();
    if (validateTarget(await api.getTab(tabId)) !== tabId) throw new Error('대상 탭이 변경되었습니다.');
    await api.attach(tabId);
    attached = true;
    before = await snapshot();
    const commands = createCommands(before);
    await wait(5000, signal);
    for (const command of commands) {
      await snapshot();
      guard();
      await api.command(tabId, command.method, command.params);
      if (command.params.type === 'mousePressed') pressed = command.params;
      if (command.params.type === 'mouseMoved' && pressed) pressed = { ...pressed, ...command.params };
      if (command.params.type === 'mouseReleased') pressed = null;
      if (command.method === 'Input.dispatchKeyEvent') {
        pressedKey = command.params.type === 'keyUp' ? null : command.params;
      }
      await wait(100, signal);
    }
    await wait(300, signal);
    return { tabId, before, after: await snapshot(), teacherFocus: 'UNVERIFIED',
      note: '페이지 관찰값입니다. Windows 커서·포커스·교사 문자 보호는 별도 실기기 시험이 필요합니다.' };
  } finally {
    if (attached) {
      // Release only in the same document. Never inject cleanup into a navigated tab.
      if (pressed || pressedKey) {
        try {
          const tab = await api.getTab(tabId);
          if (validateTarget(tab) === tabId) {
            const result = await api.command(tabId, 'Runtime.evaluate', {
              expression: SNAPSHOT_EXPRESSION, returnByValue: true
            });
            if (result?.result?.value?.documentId === before?.documentId) {
              if (pressed) await api.command(tabId, 'Input.dispatchMouseEvent', {
                type: 'mouseReleased', x: pressed.x, y: pressed.y, button: 'left', buttons: 0, clickCount: 1
              });
              if (pressedKey) await api.command(tabId, 'Input.dispatchKeyEvent', {
                type: 'keyUp', key: pressedKey.key, code: pressedKey.code,
                windowsVirtualKeyCode: pressedKey.windowsVirtualKeyCode
              });
            }
          }
        } catch { /* A closed/detached target cannot receive cleanup. */ }
      }
      // Surface detach errors; they require the user to stop the debugger manually.
      await api.detach(tabId);
    }
  }
}
