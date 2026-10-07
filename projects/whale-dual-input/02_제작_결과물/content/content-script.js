/**
 * @file content-script.js
 * @description 웨일 듀얼 인풋 웹페이지 주입 스크립트
 */
(() => {
  if (globalThis.__wdiContent) return;
  globalThis.__wdiContent = true;
  const api = globalThis.whale ?? globalThis.chrome;
  const token = crypto.randomUUID?.() ?? [...crypto.getRandomValues(new Uint8Array(16))].map(value => value.toString(16).padStart(2, '0')).join('');
  const composer = new globalThis.WDIHangul();
  let session = '', active = false, editable = null, korean = true, shifted = false, scroll = false;
  let editorId = 0;
  const host = document.createElement('div'); host.id = 'wdi-student-tools';
  const shadow = host.attachShadow({ mode: 'closed' });
  const style = document.createElement('style');
  style.textContent = `:host{all:initial;position:fixed;left:12px;right:12px;bottom:12px;z-index:2147483647;font:16px system-ui;color:#162e3a} .box{max-width:900px;margin:auto;padding:12px;background:#f6fbff;border:2px solid #176a88;border-radius:14px;box-shadow:0 8px 30px #0003}.row{display:flex;gap:5px;margin:5px 0}button{flex:1;min-width:0;min-height:44px;font:inherit;border:1px solid #bbcfd9;border-radius:7px;background:white;color:inherit;touch-action:manipulation}.notice{display:flex;justify-content:space-between;gap:8px;margin-bottom:6px}.pointer{position:fixed;width:18px;height:18px;border:3px solid #008ac0;border-radius:50%;pointer-events:none;display:none}.off{display:none} button:focus-visible{outline:3px solid #008ac0}`;
  const box = document.createElement('div'); box.className = 'box off';
  const notice = document.createElement('div'); notice.className = 'notice';
  const label = document.createElement('span'); label.textContent = '학생 키보드 · 입력란을 먼저 터치하세요';
  const close = document.createElement('button'); close.textContent = '키보드 접기'; close.style.flex = '0 0 auto';
  notice.append(label, close); box.append(notice);
  const pointer = document.createElement('div'); pointer.className = 'pointer';
  const rows = document.createElement('div'); box.append(rows);
  shadow.append(style, box, pointer); document.documentElement.append(host);
  function message(value) {
    return new Promise((resolve, reject) => api.runtime.sendMessage({ ...value, session, token }, reply => {
      const error = api.runtime.lastError;
      if (error || reply?.error) reject(new Error(error?.message ?? reply.error)); else resolve(reply);
    }));
  }
  function allowed(element) {
    return element instanceof HTMLTextAreaElement || (element instanceof HTMLInputElement && ['text', 'search', 'url', 'tel', 'email', ''].includes(element.type));
  }
  async function edit(value) {
    if (!active || !editable?.isConnected || document.activeElement !== editable || !allowed(editable) || editable.disabled || editable.readOnly) {
      label.textContent = '지원되는 입력란을 먼저 터치하세요.'; composer.reset(); return;
    }
    try { await message({ type: 'keyboard', editorId, ...value }); label.textContent = composer.pending || '학생 키보드'; }
    catch (error) { label.textContent = error.message; composer.reset(); }
  }
  function render() {
    rows.replaceChildren();
    const latin = ['qwertyuiop', 'asdfghjkl', 'zxcvbnm'];
    const normal = ['ㅂㅈㄷㄱㅅㅛㅕㅑㅐㅔ', 'ㅁㄴㅇㄹㅎㅗㅓㅏㅣ', 'ㅋㅌㅊㅍㅠㅜㅡ'];
    const upper = ['ㅃㅉㄸㄲㅆㅛㅕㅑㅒㅖ', normal[1], normal[2]];
    function button(parent, text, action) {
      const node = document.createElement('button'); node.type = 'button'; node.textContent = text;
      node.addEventListener('pointerdown', event => event.preventDefault());
      node.addEventListener('click', action); parent.append(node);
    }
    const digits = document.createElement('div'); digits.className = 'row';
    for (const char of '1234567890') button(digits, char, () => edit({ commit: composer.flush() + char, pending: '' }));
    rows.append(digits);
    for (let i = 0; i < latin.length; i++) {
      const row = document.createElement('div'); row.className = 'row';
      const chars = korean ? (shifted ? upper[i] : normal[i]) : (shifted ? latin[i].toUpperCase() : latin[i]);
      for (const char of chars) button(row, char, () => edit(korean ? composer.push(char) : { commit: composer.flush() + char, pending: '' }));
      rows.append(row);
    }
    const tools = document.createElement('div'); tools.className = 'row';
    button(tools, korean ? '한 → 영' : '영 → 한', () => { void edit({ commit: composer.flush(), pending: '' }); korean = !korean; render(); });
    button(tools, shifted ? 'Shift ●' : 'Shift', () => { shifted = !shifted; render(); });
    button(tools, '지우기', () => { const changed = composer.backspace(); void edit(changed ? { commit: '', pending: composer.pending } : { key: 'Backspace' }); });
    button(tools, '공백', () => edit({ commit: composer.flush() + ' ', pending: '' }));
    button(tools, 'Enter', () => edit({ commit: composer.flush(), pending: '', key: 'Enter' }));
    rows.append(tools);
    const mode = document.createElement('div'); mode.className = 'row';
    button(mode, scroll ? '스크롤 모드 ●' : '클릭·드래그 모드', async () => {
      scroll = !scroll; try { await message({ type: 'scroll-mode', scroll }); render(); } catch (error) { label.textContent = error.message; }
    });
    rows.append(mode);
  }
  close.addEventListener('pointerdown', event => event.preventDefault());
  close.addEventListener('click', () => rows.classList.toggle('off'));
  document.addEventListener('focusin', event => {
    if (event.target !== editable) editorId++;
    if (allowed(event.target) && event.target !== editable) {
      composer.reset(); editable = event.target;
    } else if (!allowed(event.target)) { composer.reset(); editable = null; }
  }, true);
  document.addEventListener('visibilitychange', () => { if (active && document.hidden) void message({ type: 'pause', reason: '학생 탭이 보이지 않습니다.' }).catch(() => {}); });
  window.addEventListener('resize', () => { if (active) void message({ type: 'pause', reason: '페이지 크기·배율이 변경되었습니다.' }).catch(() => {}); });
  window.addEventListener('pagehide', () => { if (active) void message({ type: 'pause', reason: '페이지를 이동했습니다.' }).catch(() => {}); });
  api.runtime.onMessage.addListener((value, sender, reply) => {
    if (sender.id !== api.runtime.id) return false;
    if (value.type === 'describe') { reply({ token, width: innerWidth, height: innerHeight, visible: !document.hidden,
      editorId, editable: !!editable && document.activeElement === editable && allowed(editable) && !editable.readOnly && !editable.disabled }); return false; }
    if (value.type === 'configure') { session = value.session; composer.reset(); editable = null; reply({ token }); return false; }
    if (value.session !== session) return false;
    if (value.type === 'state') {
      active = value.active; box.classList.toggle('off', !active); pointer.style.display = 'none';
      if (!active) { composer.reset(); scroll = false; }
      render(); reply({ ok: true });
    }
    if (value.type === 'pointer') { pointer.style.display = active ? 'block' : 'none'; pointer.style.left = `${value.x - 9}px`; pointer.style.top = `${value.y - 9}px`; }
    return false;
  });
  render();
})();
