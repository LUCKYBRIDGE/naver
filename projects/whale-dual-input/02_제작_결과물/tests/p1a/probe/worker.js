import { FIXTURE_URL, runProbe, validateTarget } from './engine.js';

const api = globalThis.whale ?? globalThis.chrome;
const panelUrl = api.runtime.getURL('panel.html');
let controller = null;
let needsReload = false;
let state = { status: 'IDLE', note: '워커 재시작 시 이전 시험은 복원하지 않습니다.' };

function call(start, onLateSuccess) {
  return new Promise((resolve, reject) => {
    let timedOut = false;
    const timer = setTimeout(() => {
      timedOut = true;
      needsReload = true;
      reject(new Error('브라우저 API 응답 시간 초과. 시험을 중단하고 디버거 연결을 확인하세요.'));
    }, 3000);
    try {
      start(result => {
        clearTimeout(timer);
        const error = api.runtime.lastError;
        if (timedOut) { if (!error) onLateSuccess?.(); return; }
        if (error) reject(new Error(error.message));
        else resolve(result);
      });
    } catch (error) { clearTimeout(timer); reject(error); }
  });
}
const bridge = {
  getTab: id => call(done => api.tabs.get(id, done)),
  attach: id => call(done => api.debugger.attach({ tabId: id }, '1.3', done), () => {
    // A late attach must not leave a debugger attached after the timeout.
    api.debugger.detach({ tabId: id }, () => { void api.runtime.lastError; });
  }),
  detach: id => call(done => api.debugger.detach({ tabId: id }, done)),
  command: (id, method, params) => call(done => api.debugger.sendCommand({ tabId: id }, method, params, done))
};

api.action.onClicked.addListener(() => api.tabs.create({ url: panelUrl }));
api.debugger.onDetach.addListener(source => {
  if (controller && source.tabId === state.tabId) controller.abort();
});
api.tabs.onUpdated.addListener((id, change) => {
  if (controller && id === state.tabId && (change.status === 'loading' || change.url)) controller.abort();
});
api.tabs.onRemoved.addListener(id => {
  if (controller && id === state.tabId) controller.abort();
});

async function handle(message) {
  if (message?.type === 'status') return state;
  if (message?.type === 'targets') {
    const tabs = await call(done => api.tabs.query({ url: 'http://127.0.0.1/*' }, done));
    return tabs.filter(tab => tab.url === FIXTURE_URL).map(({ id, windowId }) => ({ id, windowId }));
  }
  if (message?.type === 'stop') {
    controller?.abort();
    return { status: controller ? 'STOPPING' : state.status };
  }
  if (message?.type !== 'run') throw new Error('지원하지 않는 요청입니다.');
  if (needsReload) throw new Error('API 시간 초과 후에는 실험 확장앱을 다시 로드하고 디버거 해제를 확인하세요.');
  if (controller) throw new Error('이미 시험이 실행 중입니다.');
  const tabId = validateTarget({ id: message.tabId, url: FIXTURE_URL });
  controller = new AbortController();
  state = { status: 'RUNNING', tabId, note: '5초 안에 교사 작업 앱으로 이동하세요. 자동 포커스 변경은 하지 않습니다.' };
  void runProbe(bridge, tabId, { signal: controller.signal }).then(report => {
    state = { status: 'OBSERVED', report };
  }).catch(error => {
    state = { status: 'STOPPED', error: error.message, teacherFocus: 'UNVERIFIED' };
  }).finally(() => { controller = null; });
  return state;
}

api.runtime.onMessage.addListener((message, sender, reply) => {
  if (sender.id !== api.runtime.id || sender.url !== panelUrl) {
    reply({ error: '제어 화면에서만 실행할 수 있습니다.' });
    return false;
  }
  handle(message).then(reply, error => reply({ error: error.message }));
  return true;
});
