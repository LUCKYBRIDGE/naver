(() => {
  const documentId = crypto.randomUUID();
  const events = [];
  let clicks = 0;
  let drags = 0;
  let start = null;
  let moved = false;
  const canvas = document.querySelector('#drag');
  const context = canvas.getContext('2d');
  const draw = event => {
    const rect = canvas.getBoundingClientRect();
    context.fillStyle = '#136c97';
    context.fillRect((event.clientX - rect.left) * canvas.width / rect.width,
      (event.clientY - rect.top) * canvas.height / rect.height, 6, 6);
  };
  document.querySelector('#click').addEventListener('click', () => {
    document.querySelector('#click-count').textContent = `${++clicks}회`;
  });
  canvas.addEventListener('pointerdown', event => {
    start = { x: event.clientX, y: event.clientY };
    moved = false;
    canvas.setPointerCapture(event.pointerId);
    draw(event);
  });
  canvas.addEventListener('pointermove', event => {
    if (!start) return;
    moved ||= Math.hypot(event.clientX - start.x, event.clientY - start.y) > 5;
    draw(event);
  });
  canvas.addEventListener('pointerup', () => {
    if (start && moved) document.querySelector('#drag-count').textContent = `완료된 드래그 ${++drags}회`;
    start = null;
  });
  canvas.addEventListener('pointercancel', () => { start = null; });
  for (const type of ['click', 'pointerdown', 'pointermove', 'pointerup', 'wheel', 'beforeinput', 'input', 'keydown', 'keyup', 'compositionstart', 'compositionend']) {
    document.addEventListener(type, event => {
      if (!['click', 'drag', 'scroll', 'input', 'textarea'].includes(event.target.id)) return;
      // Do not record arbitrary key/data contents. Only fixture fields are inspected below.
      events.push({ type, target: event.target.id, isTrusted: event.isTrusted });
      if (events.length > 40) events.shift();
      document.querySelector('#events').textContent = JSON.stringify(events, null, 2);
    }, { capture: true, passive: true });
  }
  window.p1aSnapshot = () => {
    const points = {};
    for (const id of ['click', 'drag', 'scroll', 'input', 'textarea']) {
      const rect = document.getElementById(id).getBoundingClientRect();
      points[id] = { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
    }
    return { documentId, viewport: { width: innerWidth, height: innerHeight }, points,
      observed: { clicks, drags, scrollTop: document.querySelector('#scroll').scrollTop,
        input: document.querySelector('#input').value, textarea: document.querySelector('#textarea').value,
        pageHasFocus: document.hasFocus(), visibility: document.visibilityState }, events: [...events] };
  };
})();
