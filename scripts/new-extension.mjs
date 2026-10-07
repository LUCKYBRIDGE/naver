/**
 * @file new-extension.mjs
 * @description 새로운 웨일스페이스 확장 프로그램 보일러플레이트를 자동 생성하는 스크립트
 * 사용법: node scripts/new-extension.mjs <폴더명> <앱이름>
 * 예시: node scripts/new-extension.mjs 04-timer "학습 뽀모도로 타이머"
 */

import fs from 'node:fs';
import path from 'node:path';
import { generateIconsForDir } from './generate-icons.mjs';

const [,, folderName, appName = '웨일스페이스 확장앱'] = process.argv;

if (!folderName) {
  console.error('사용법: node scripts/new-extension.mjs <폴더명> [앱이름]');
  console.error('예: node scripts/new-extension.mjs 04-timer "집중 타이머"');
  process.exit(1);
}

const targetDir = path.resolve('extensions', folderName);

if (fs.existsSync(targetDir)) {
  console.error(`오류: 이미 존재하는 폴더입니다 -> ${targetDir}`);
  process.exit(1);
}

fs.mkdirSync(path.join(targetDir, 'background'), { recursive: true });
fs.mkdirSync(path.join(targetDir, 'content'), { recursive: true });
fs.mkdirSync(path.join(targetDir, 'sidebar'), { recursive: true });
fs.mkdirSync(path.join(targetDir, 'icons'), { recursive: true });

// 1. manifest.json
const manifest = {
  manifest_version: 3,
  name: `웨일스페이스 ${appName}`,
  version: "1.0.0",
  description: `${appName} 확장 프로그램`,
  icons: {
    "16": "icons/icon-16.png",
    "48": "icons/icon-48.png",
    "128": "icons/icon-128.png"
  },
  background: {
    service_worker: "background/service-worker.js",
    type: "module"
  },
  sidebar_action: {
    default_page: "sidebar/sidebar.html",
    default_icon: {
      "16": "icons/icon-16.png",
      "48": "icons/icon-48.png",
      "128": "icons/icon-128.png"
    },
    default_title: appName
  },
  permissions: ["storage", "activeTab"],
  content_scripts: [
    {
      matches: ["https://*/*", "http://*/*"],
      js: ["content/content-script.js"],
      run_at: "document_idle"
    }
  ]
};

fs.writeFileSync(path.join(targetDir, 'manifest.json'), JSON.stringify(manifest, null, 2), 'utf-8');

// 2. background/service-worker.js
fs.writeFileSync(
  path.join(targetDir, 'background/service-worker.js'),
`/**
 * @file service-worker.js
 * @description ${appName} 백그라운드 서비스 워커
 */
chrome.runtime.onInstalled.addListener(() => {
  console.log('[${appName}] 설치 완료');
});
`,
  'utf-8'
);

// 3. content/content-script.js
fs.writeFileSync(
  path.join(targetDir, 'content/content-script.js'),
`/**
 * @file content-script.js
 * @description ${appName} 컨텐츠 스크립트
 */
console.log('[${appName}] 컨텐츠 스크립트 로드됨');
`,
  'utf-8'
);

// 4. sidebar/sidebar.html
fs.writeFileSync(
  path.join(targetDir, 'sidebar/sidebar.html'),
`<!DOCTYPE html>
<html lang="ko">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>${appName}</title>
  <link rel="stylesheet" href="sidebar.css">
</head>
<body>
  <div class="sidebar-layout">
    <header class="app-header">
      <h1 class="brand-title">${appName}</h1>
    </header>
    <main class="card">
      <h2 class="card-title">기본 기능</h2>
      <p style="font-size: 13px; color: #686e74;">새 확장 프로그램을 개발해보세요.</p>
    </main>
  </div>
  <script src="sidebar.js"></script>
</body>
</html>
`,
  'utf-8'
);

// 5. sidebar/sidebar.css
fs.writeFileSync(
  path.join(targetDir, 'sidebar/sidebar.css'),
`* { box-sizing: border-box; margin: 0; padding: 0; }
body {
  font-family: -apple-system, BlinkMacSystemFont, "Apple SD Gothic Neo", sans-serif;
  font-size: 14px;
  background-color: #f8f9fa;
  padding: 16px;
}
.sidebar-layout { display: flex; flex-direction: column; gap: 12px; }
.app-header { padding-bottom: 8px; border-bottom: 1px solid #e4e8eb; }
.brand-title { font-size: 16px; font-weight: 700; }
.card { background: #fff; border: 1px solid #e4e8eb; border-radius: 8px; padding: 14px; }
.card-title { font-size: 13px; font-weight: 700; margin-bottom: 8px; color: #686e74; }
`,
  'utf-8'
);

// 6. sidebar/sidebar.js
fs.writeFileSync(
  path.join(targetDir, 'sidebar/sidebar.js'),
`/**
 * @file sidebar.js
 * @description ${appName} 사이드바 제어 로직
 */
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') {
    console.log('[${appName}] 사이드바 활성화');
  }
});
`,
  'utf-8'
);

// 7. icons 생성
generateIconsForDir(path.join(targetDir, 'icons'), [0, 199, 60]);

console.log(`\n🎉 새 확장 프로그램 템플릿이 성공적으로 생성되었습니다: extensions/${folderName}`);
console.log(`웨일 브라우저의 whale://extensions 에서 [압축해제된 확장앱 로드]로 위 폴더를 선택하세요.\n`);
