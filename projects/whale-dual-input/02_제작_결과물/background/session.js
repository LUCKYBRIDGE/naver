/** Session state is ephemeral. A restarted worker starts OFF. */
const api = globalThis.whale ?? globalThis.chrome;
const hostName = 'org.luckybridge.whale_dual_input';
const panelUrl = api.runtime.getURL('sidebar/sidebar.html');
let port = null, heartbeat = null, attached = false, nativeReady = false, stopping = null;
let target = null, token = '', session = '', lastSeq = 0, pointer = null, pressed = false, scrollMode = false;
let lastPoint = { x: 0, y: 0 }, queue = Promise.resolve(), pending = 0, epoch = 0, busy = false, lateApi = false;
let pressedKey = null, lastClick = null, clickCount = 1;
let state = { status: 'OFF', reason: '학생 탭을 선택하세요.' };
function call(start, onLateSuccess) {
  return new Promise((resolve, reject) => {
    let expired = false;
    const timer = setTimeout(() => { expired = true; lateApi = true; reject(new Error('브라우저 응답이 멈췄습니다. 확장앱을 다시 로드하세요.')); }, 2500);
    try { start(value => {
      clearTimeout(timer); const error = api.runtime.lastError;
      if (expired) { if (!error) onLateSuccess?.(); return; }
      if (error) reject(new Error(error.message)); else resolve(value);
    }); } catch (error) { clearTimeout(timer); reject(error); }
  });
}
const tabMessage = value => target ? call(done => api.tabs.sendMessage(target.id, { ...value, session }, done)) : Promise.resolve();
const command = (method, params) => call(done => api.debugger.sendCommand({ tabId: target.id }, method, params, done));
function native(value) { if (!port) throw new Error('Windows 프로그램을 연결하세요.'); port.postMessage({ ...value, session }); }
function publish(status, reason = '') {
  state = { status, reason, tabId: target?.id, windowId: target?.windowId, nativeConnected: !!port, nativeReady, scrollMode };
  api.runtime.sendMessage({ type: 'wdi-state', state }, () => { void api.runtime.lastError; });
}

export function stop(reason = 'OFF') {
  if (stopping) return stopping;
  epoch++; pointer = null; pending = 0; publish('STOPPING', reason);
  clearInterval(heartbeat); heartbeat = null;
  try { if (port && session) native({ type: 'stop' }); } catch { /* Host watchdog removes an orphaned overlay. */ }
  stopping = (async () => {
    if (attached && target) {
      // Cleanup only if the selected document still exists, never into a navigated page.
      try {
        const page = await tabMessage({ type: 'describe' });
        if (page?.token === token) {
          await command('Input.imeSetComposition', { text: '', selectionStart: 0, selectionEnd: 0 });
          if (pressed) await command('Input.dispatchMouseEvent', { type: 'mouseReleased', ...lastPoint, button: 'left', buttons: 0 });
          if (pressedKey) await command('Input.dispatchKeyEvent', { ...pressedKey, type: 'keyUp' });
        }
      } catch { /* Target may be gone. */ }
    }
    pressed = false; pressedKey = null; lastClick = null;
    try { await tabMessage({ type: 'state', active: false }); } catch { /* Content may be unloaded. */ }
    if (attached && target) {
      attached = false;
      try { await call(done => api.debugger.detach({ tabId: target.id }, done)); } catch { lateApi = true; }
    }
    publish('OFF', reason);
  })().finally(() => { stopping = null; });
  return stopping;
}
async function validate() {
  if (!target) throw new Error('학생 탭을 선택하세요.');
  const tab = await call(done => api.tabs.get(target.id, done));
  if (tab.windowId !== target.windowId || tab.url !== target.url || !tab.active) throw new Error('학생 탭이 변경되었습니다. 다시 선택하세요.');
  const page = await tabMessage({ type: 'describe' });
  if (page?.token !== token || page.width !== target.width || page.height !== target.height || !page.visible) throw new Error('페이지·크기·표시 상태가 변경되었습니다.');
  return page;
}
function enqueue(task) {
  const generation = epoch;
  if (++pending > 64) { void stop('입력 큐가 지연되어 해제했습니다.'); return; }
  queue = queue.catch(() => {}).then(async () => {
    if (generation !== epoch || state.status !== 'ACTIVE') return;
    await validate();
    if (generation !== epoch || state.status !== 'ACTIVE') return;
    const dispatch = async (method, params) => {
      if (generation !== epoch || state.status !== 'ACTIVE') throw new Error('종료된 입력 세션');
      return command(method, params);
    };
    await task(dispatch);
  }).catch(error => { if (generation === epoch) return stop(error.message); }).finally(() => { if (generation === epoch) pending = Math.max(0, pending - 1); });
}
function receivePointer(value) {
  if (value.session !== session || state.status !== 'ACTIVE') return;
  if (!Number.isSafeInteger(value.seq) || value.seq <= lastSeq || !Number.isInteger(value.pointer) ||
      ![value.x, value.y, value.delta].every(Number.isFinite) || value.x < 0 || value.y < 0 || value.x >= target.width || value.y >= target.height || Math.abs(value.delta) > 2000 ||
      !['down', 'move', 'up', 'cancel', 'scroll'].includes(value.phase)) {
    void stop('잘못된 입력 메시지'); return;
  }
  lastSeq = value.seq;
  enqueue(async dispatch => {
    if (value.phase === 'down') {
      if (pointer !== null) return; pointer = value.pointer;
      clickCount = lastClick && Date.now() - lastClick.time < 400 && Math.hypot(value.x - lastClick.x, value.y - lastClick.y) < 4 ? 2 : 1;
    }
    if (value.phase !== 'scroll' && pointer !== value.pointer) return;
    const type = value.phase === 'down' ? 'mousePressed' : ['up', 'cancel'].includes(value.phase) ? 'mouseReleased' : value.phase === 'scroll' ? 'mouseWheel' : 'mouseMoved';
    await dispatch('Input.dispatchMouseEvent', { type, x: value.x, y: value.y,
      ...(type === 'mouseWheel' ? { deltaX: 0, deltaY: value.delta } : { button: 'left', buttons: type === 'mouseReleased' ? 0 : 1, clickCount }) });
    lastPoint = { x: value.x, y: value.y };
    pressed = type === 'mousePressed' ? true : type === 'mouseReleased' ? false : pressed;
    if (type === 'mouseReleased') { pointer = null; lastClick = clickCount === 2 ? null : { time: Date.now(), x: value.x, y: value.y }; }
    void tabMessage({ type: 'pointer', x: value.x, y: value.y }).catch(() => {});
  });
}
function connect() {
  if (port) return;
  port = api.runtime.connectNative(hostName);
  port.onMessage.addListener(value => {
    if (value.type === 'hello') {
      publish('OFF', 'Windows 연결됨. 학생 탭과 영역을 지정하세요.');
      if (target) native({ type: 'configure', width: target.width, height: target.height });
    } else if (value.session === session) {
      if (value.type === 'ready') { nativeReady = true; publish('READY', '영역 지정 완료. ON을 누르세요.'); }
      if (value.type === 'active' && state.status === 'STARTING') { publish('ACTIVE'); void tabMessage({ type: 'state', active: true }).catch(error => stop(error.message)); }
      if (value.type === 'off' && !['OFF', 'STOPPING'].includes(state.status)) void stop(value.reason ?? 'Windows 해제');
      if (value.type === 'pointer') receivePointer(value);
    }
  });
  port.onDisconnect.addListener(() => {
    const error = api.runtime.lastError?.message;
    port = null; nativeReady = false; void stop(error ?? 'Windows 연결이 종료되었습니다.');
  });
}
async function select(tabId) {
  await stop(); nativeReady = false;
  const tab = await call(done => api.tabs.get(tabId, done));
  if (!Number.isInteger(tab.id) || !tab.active || !/^https?:\/\//.test(tab.url ?? '')) throw new Error('학생 페이지가 선택된 웨일 탭을 지정하세요.');
  target = { id: tab.id, windowId: tab.windowId, url: tab.url };
  await call(done => api.scripting.executeScript({ target: { tabId }, files: ['content/hangul.js', 'content/content-script.js'] }, done));
  const page = await tabMessage({ type: 'describe' });
  if (!page?.visible || !Number.isInteger(page.width) || !Number.isInteger(page.height)) throw new Error('현재 페이지에서 실행할 수 없습니다.');
  target.width = page.width; target.height = page.height; token = page.token;
  session = crypto.randomUUID(); lastSeq = 0;
  await tabMessage({ type: 'configure' });
  if (port) native({ type: 'configure', width: target.width, height: target.height });
  publish('PREPARING', 'Windows 제어창에서 같은 웨일 창의 웹페이지 영역을 지정하세요.');
}
async function start() {
  if (lateApi) throw new Error('이전 연결 오류가 있습니다. 확장앱을 다시 로드하세요.');
  if (!nativeReady || !port) throw new Error('Windows 연결과 웹페이지 영역 지정을 완료하세요.');
  await validate();
  const tabId = target.id;
  await call(done => api.debugger.attach({ tabId }, '1.3', done), () => api.debugger.detach({ tabId }, () => { void api.runtime.lastError; }));
  attached = true; scrollMode = false; epoch++; publish('STARTING');
  native({ type: 'start' });
  const generation = epoch;
  heartbeat = setInterval(() => {
    try { native({ type: 'tick' }); } catch (error) { void stop(error.message); }
  }, 1000);
  setTimeout(() => { if (generation === epoch && state.status === 'STARTING') void stop('Windows 활성화 응답이 없습니다.'); }, 3000);
}
function keyboard(value) {
  if (!Number.isSafeInteger(value.editorId) || value.editorId < 0) throw new Error('잘못된 입력란 식별자');
  if (value.key && !['Backspace', 'Enter'].includes(value.key)) throw new Error('지원하지 않는 키');
  if (value.commit != null && (typeof value.commit !== 'string' || value.commit.length > 20)) throw new Error('잘못된 문자 입력');
  if (value.pending != null && (typeof value.pending !== 'string' || value.pending.length > 4)) throw new Error('잘못된 한글 조합');
  enqueue(async dispatch => {
    const editor = await tabMessage({ type: 'describe' });
    if (!editor?.editable || editor.editorId !== value.editorId) throw new Error('입력란이 변경되었습니다. 다시 선택하세요.');
    if (value.commit) { await dispatch('Input.imeSetComposition', { text: '', selectionStart: 0, selectionEnd: 0 }); await dispatch('Input.insertText', { text: value.commit }); }
    if (value.pending != null) await dispatch('Input.imeSetComposition', { text: value.pending, selectionStart: value.pending.length, selectionEnd: value.pending.length });
    if (value.key) {
      const enter = value.key === 'Enter';
      await dispatch('Input.dispatchKeyEvent', { type: enter ? 'keyDown' : 'rawKeyDown', key: value.key, code: value.key, windowsVirtualKeyCode: enter ? 13 : 8, ...(enter ? { text: '\r', unmodifiedText: '\r' } : {}) });
      pressedKey = { key: value.key, code: value.key, windowsVirtualKeyCode: enter ? 13 : 8 };
      await dispatch('Input.dispatchKeyEvent', { type: 'keyUp', key: value.key, code: value.key, windowsVirtualKeyCode: enter ? 13 : 8 });
      pressedKey = null;
    }
  });
}
api.runtime.onMessage.addListener((value, sender, reply) => {
  if (!value || typeof value !== 'object') { reply({ error: '잘못된 요청' }); return false; }
  const panel = sender.id === api.runtime.id && sender.url === panelUrl;
  const content = sender.id === api.runtime.id && sender.tab?.id === target?.id && sender.frameId === 0 && value.session === session && value.token === token;
  if (!panel && !content) { reply({ error: '허용되지 않은 요청' }); return false; }
  const task = async () => {
    if (value.type === 'status' && panel) return state;
    if (value.type === 'pause' && content) { await stop(String(value.reason ?? '일시 중단').slice(0, 200)); return state; }
    if (value.type === 'keyboard' && content && state.status === 'ACTIVE') { keyboard(value); return { ok: true }; }
    if (value.type === 'scroll-mode' && state.status === 'ACTIVE') { scrollMode = value.scroll === true; native({ type: 'mode', scroll: scrollMode }); publish('ACTIVE'); return state; }
    if (!panel) throw new Error('실행할 수 없는 상태');
    if (busy) throw new Error('이전 작업이 진행 중입니다.');
    busy = true;
    try {
      if (value.type === 'connect') connect();
      else if (value.type === 'select') await select(value.tabId);
      else if (value.type === 'start') { if (['ACTIVE', 'STARTING'].includes(state.status)) throw new Error('이미 실행 중입니다.'); await start(); }
      else if (value.type === 'stop') await stop('사용자가 OFF를 선택했습니다.');
      else throw new Error('지원하지 않는 요청');
      return state;
    } finally { busy = false; }
  };
  task().then(reply, async error => { if (!busy && !['OFF', 'STOPPING'].includes(state.status)) await stop(error.message); reply({ error: error.message }); }); return true;
});
api.debugger.onDetach.addListener(source => { if (source.tabId === target?.id && attached) { attached = false; void stop('디버거 연결이 해제되었습니다.'); } });
api.tabs.onRemoved.addListener(id => { if (id === target?.id) void stop('학생 탭 종료'); });
api.tabs.onUpdated.addListener((id, change) => { if (id === target?.id && (change.status === 'loading' || change.url)) { nativeReady = false; void stop('학생 페이지 이동. 다시 선택하세요.'); } });
