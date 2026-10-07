const api = globalThis.whale ?? globalThis.chrome;
const target = document.querySelector('#target');
const result = document.querySelector('#result');
function request(message) {
  return new Promise((resolve, reject) => api.runtime.sendMessage(message, reply => {
    const error = api.runtime.lastError;
    if (error) reject(new Error(error.message));
    else if (reply?.error) reject(new Error(reply.error));
    else resolve(reply);
  }));
}
async function refresh() {
  const tabs = await request({ type: 'targets' });
  target.replaceChildren();
  for (const tab of tabs) {
    const option = document.createElement('option');
    option.value = String(tab.id);
    option.textContent = `학생 시험 탭 ${tab.id} / 창 ${tab.windowId}`;
    target.append(option);
  }
  document.querySelector('#run').disabled = tabs.length === 0;
  result.textContent = tabs.length ? '대상 탭을 선택하세요.' : 'http://127.0.0.1:8765/p1a.html 시험 탭을 먼저 여세요.';
}
function display(task) {
  Promise.resolve().then(task).then(value => { if (value) result.textContent = JSON.stringify(value, null, 2); })
    .catch(error => { result.textContent = error.message; });
}
document.querySelector('#refresh').addEventListener('click', () => display(refresh));
document.querySelector('#run').addEventListener('click', () => display(() => {
  if (!target.value) throw new Error('대상 탭을 선택하세요.');
  return request({ type: 'run', tabId: Number(target.value) });
}));
document.querySelector('#stop').addEventListener('click', () => display(() => request({ type: 'stop' })));
document.querySelector('#status').addEventListener('click', () => display(() => request({ type: 'status' })));
display(refresh);
