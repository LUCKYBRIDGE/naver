/**
 * @file sidebar.js
 * @description 웨일 듀얼 인풋 사이드바 제어 로직
 */
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') {
    console.log('[웨일 듀얼 인풋] 사이드바 활성화');
  }
});

const api = globalThis.whale ?? globalThis.chrome;
const status = document.querySelector('#status');
let current = { status: 'OFF' };
function display(value) {
  current = value;
  status.textContent = `${value.status} · ${value.reason || (value.status === 'ACTIVE' ? '학생 입력 실행 중' : '대기 중')}${value.tabId != null ? ` / 학생 탭 ${value.tabId}` : ''}`;
  document.querySelector('#start').disabled = !value.nativeReady || ['ACTIVE', 'STARTING', 'STOPPING'].includes(value.status);
  document.querySelector('#scroll').disabled = value.status !== 'ACTIVE';
}
function request(value) {
  return new Promise((resolve, reject) => api.runtime.sendMessage(value, reply => {
    const error = api.runtime.lastError;
    if (error || reply?.error) reject(new Error(error?.message ?? reply.error)); else resolve(reply);
  }));
}
function execute(task) { Promise.resolve().then(task).then(display).catch(error => { status.textContent = error.message; }); }
document.querySelector('#select').addEventListener('click', () => {
  let origin;
  try {
    const url = new URL(document.querySelector('#site').value);
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password || url.hostname.includes('*')) throw new Error('http 또는 https 사이트 주소를 입력하세요.');
    origin = `${url.protocol}//${url.hostname}/*`;
  } catch (error) { status.textContent = error.message; return; }
  // Request inside the user gesture; only the entered site's origin is requested.
  api.permissions.request({ origins: [origin] }, granted => {
    const error = api.runtime.lastError;
    if (error || !granted) { status.textContent = error?.message ?? '사이트 접근이 허용되지 않았습니다.'; return; }
    api.tabs.query({ active: true, currentWindow: true }, tabs => {
      const queryError = api.runtime.lastError;
      if (queryError || !tabs[0]) { status.textContent = queryError?.message ?? '현재 탭을 찾을 수 없습니다.'; return; }
      execute(() => request({ type: 'select', tabId: tabs[0].id }));
    });
  });
});
for (const [id, type] of [['connect', 'connect'], ['start', 'start'], ['stop', 'stop'], ['refresh', 'status']]) {
  document.querySelector(`#${id}`).addEventListener('click', () => execute(() => request({ type })));
}
document.querySelector('#scroll').addEventListener('click', () => execute(() => request({ type: 'scroll-mode', scroll: !current.scrollMode })));
api.runtime.onMessage.addListener((value, sender) => { if (sender.id === api.runtime.id && value.type === 'wdi-state') display(value.state); });
execute(() => request({ type: 'status' }));
