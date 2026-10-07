// Optional integration check. Playwright lives outside the exFAT workspace.
import assert from 'node:assert/strict';
import { pathToFileURL } from 'node:url';
import { resolve } from 'node:path';
import { runProbe, FIXTURE_URL } from './probe/engine.js';

if (!process.argv[2]) throw new Error('외부에 설치된 playwright/index.mjs 절대 경로를 전달하세요.');
const { chromium } = await import(pathToFileURL(resolve(process.argv[2])).href);
const browser = await chromium.launch({ channel: 'chrome', headless: true });
try {
  const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const student = await context.newPage();
  const errors = [];
  student.on('pageerror', error => errors.push(error.message));
  student.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  await student.goto(FIXTURE_URL);
  const teacher = await context.newPage();
  await teacher.setContent('<textarea id="teacher" aria-label="교사 입력"></textarea>');
  await teacher.locator('#teacher').focus();
  const cdp = await context.newCDPSession(student);
  const bridge = {
    getTab: async () => ({ id: 7, url: student.url() }), attach: async () => {},
    detach: async () => cdp.detach(), command: async (id, method, params) => cdp.send(method, params)
  };
  const trial = runProbe(bridge, 7);
  await teacher.keyboard.type('Teacher input remains here 123', { delay: 200 });
  const report = await trial;
  const observed = report.after.observed;
  assert.equal(observed.clicks, 1);
  assert.equal(observed.drags, 1);
  assert.ok(observed.scrollTop > 0);
  assert.equal(observed.input, '한글 ABC 12');
  assert.equal(observed.textarea, '한글 ABC 12\n');
  assert.equal(await teacher.locator('#teacher').inputValue(), 'Teacher input remains here 123');
  assert.deepEqual(errors, []);
  console.log(JSON.stringify({ browser: browser.version(), observed, teacherTextPreserved: true, errors,
    windowsWhaleFocus: 'UNVERIFIED', transport: 'Playwright CDP; chrome.debugger extension not exercised' }, null, 2));
} finally { await browser.close(); }
