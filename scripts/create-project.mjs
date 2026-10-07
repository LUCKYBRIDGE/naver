/**
 * @file create-project.mjs
 * @description 공모전 규격에 맞춘 완전 독립형 프로젝트 슬롯 생성 스크립트
 * 사용법: node scripts/create-project.mjs <폴더명> "<프로젝트명>"
 * 예시: node scripts/create-project.mjs team-board-helper "팀보드 퀵 수집기"
 */

import fs from 'node:fs';
import path from 'node:path';
import { generateIconsForDir } from './generate-icons.mjs';

const [,, folderName, appName = '웨일스페이스 교육 도구'] = process.argv;

if (!folderName) {
  console.error('\n❌ 사용법: node scripts/create-project.mjs <폴더명> [프로젝트명]');
  console.error('예: node scripts/create-project.mjs project-01 "수업 루틴 오케스트레이터"\n');
  process.exit(1);
}

const projectRoot = path.resolve('projects', folderName);

if (fs.existsSync(projectRoot)) {
  console.error(`\n❌ 오류: 이미 존재하는 프로젝트 폴더입니다 -> ${projectRoot}\n`);
  process.exit(1);
}

const dir01 = path.join(projectRoot, '01_기획_및_지침');
const dir02 = path.join(projectRoot, '02_제작_결과물');
const dir03 = path.join(projectRoot, '03_사용방법_심사안내');
const dir04 = path.join(projectRoot, '04_최종_배포_제출');

fs.mkdirSync(dir01, { recursive: true });
fs.mkdirSync(dir02, { recursive: true });
fs.mkdirSync(dir03, { recursive: true });
fs.mkdirSync(dir04, { recursive: true });

fs.mkdirSync(path.join(dir02, 'background'), { recursive: true });
fs.mkdirSync(path.join(dir02, 'content'), { recursive: true });
fs.mkdirSync(path.join(dir02, 'sidebar'), { recursive: true });
fs.mkdirSync(path.join(dir02, 'icons'), { recursive: true });

// ============================================================
// 1. 01_기획_및_지침 파일 생성
// ============================================================
fs.writeFileSync(
  path.join(dir01, 'README.md'),
`# [${appName}] 프로젝트 전용 개발 지침

이 문서는 **${appName}** 프로젝트만을 위한 독립 지침입니다.
다른 프로젝트와 섞이지 않도록 이 폴더 내부의 설정과 명세만 참조합니다.

---

## 🎯 프로젝트 기본 정보
- **프로젝트명:** ${appName}
- **폴더 경로:** \`projects/${folderName}/\`
- **공모 분야:** [ ] 분야 1: 수업 지원·학습 활동 / [ ] 분야 2: 평가·피드백 혁신 / [ ] 분야 3: 학급 운영·교사 업무 효율화
- **연계 웨일 서비스:** [ ] 웨일 클래스 / [ ] 팀보드 / [ ] 웨일 UBT / [ ] 웨일온

---

## 🔒 격리 규칙
1. 이 프로젝트의 모든 소스코드는 \`02_제작_결과물/\` 내부에만 작성합니다.
2. 다른 프로젝트와 상호 참조가 필요할 경우, 직접 참조하지 않고 \`packages/shared/\`의 공통 모듈을 통합니다.
3. 제출 문서는 이 폴더 내부의 \`기획의도_A4_1페이지.md\`와 \`03_사용방법_심사안내/\`에서만 관리합니다.
`,
  'utf-8'
);

fs.writeFileSync(
  path.join(dir01, '기획의도_A4_1페이지.md'),
`# [제출물 01] 기획의도 1페이지

> **작성 안내:** 심사 제출 시 A4 1장 이내 PDF로 변환하여 제출합니다.

---

### 1. 작품 개요
- **작품명:** ${appName}
- **출품 분야:** [분야 선택]
- **연계 서비스:** 웨일 클래스 / 팀보드 / 웨일 UBT / 웨일온 (해당 항목 기재)

---

### 2. 해결하려는 문제 (Problem)
- *어떤 수업/학급 운영 상황에서 발생하는 문제인가?*
- *선생님 또는 학생들이 기존에 겪던 구체적인 불편함은 무엇인가?*

---

### 3. 핵심 기능 (Solution & Key Features)
1. **[핵심 기능 1]:** 
2. **[핵심 기능 2]:** 
3. **[핵심 기능 3]:** 

---

### 4. 기대 효과 및 웨일 연계성 (Impact)
- **수업 루틴 개선:** 
- **웨일 교육 서비스와의 연계 시너지:** 
- **확장성 및 범용성:** (타 학교급, 전 교과 적용 가능성)
`,
  'utf-8'
);

// ============================================================
// 2. 02_제작_결과물 (확장 프로그램 뼈대)
// ============================================================
const manifest = {
  manifest_version: 3,
  name: `웨일스페이스 ${appName}`,
  version: "1.0.0",
  description: `${appName} - 웨일스페이스 교육용 확장 프로그램`,
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
    "default_page": "sidebar/sidebar.html",
    "default_icon": {
      "16": "icons/icon-16.png",
      "48": "icons/icon-48.png",
      "128": "icons/icon-128.png"
    },
    "default_title": appName
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

fs.writeFileSync(path.join(dir02, 'manifest.json'), JSON.stringify(manifest, null, 2), 'utf-8');

fs.writeFileSync(
  path.join(dir02, 'background/service-worker.js'),
`/**
 * @file service-worker.js
 * @description ${appName} 백그라운드 서비스 워커 (MV3)
 */
chrome.runtime.onInstalled.addListener(() => {
  console.log('[${appName}] 서비스 워커 설치 완료');
});
`,
  'utf-8'
);

fs.writeFileSync(
  path.join(dir02, 'content/content-script.js'),
`/**
 * @file content-script.js
 * @description ${appName} 웹페이지 주입 스크립트
 */
console.log('[${appName}] 컨텐츠 스크립트 실행됨');
`,
  'utf-8'
);

fs.writeFileSync(
  path.join(dir02, 'sidebar/sidebar.html'),
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
      <h2 class="card-title">도구 시작하기</h2>
      <p style="font-size: 13px; color: #686e74;">수업에 필요한 기능을 여기에 개발합니다.</p>
    </main>
    <footer class="app-footer">
      <span>🔒 기기 내에서 안전하게 동작합니다.</span>
    </footer>
  </div>
  <script src="sidebar.js"></script>
</body>
</html>
`,
  'utf-8'
);

fs.writeFileSync(
  path.join(dir02, 'sidebar/sidebar.css'),
`* { box-sizing: border-box; margin: 0; padding: 0; }
body {
  font-family: -apple-system, BlinkMacSystemFont, "Apple SD Gothic Neo", sans-serif;
  font-size: 14px;
  background-color: #f8f9fa;
  color: #1e2022;
  overflow-x: hidden;
}
.sidebar-layout { display: flex; flex-direction: column; min-height: 100vh; padding: 16px; gap: 12px; }
.app-header { padding-bottom: 8px; border-bottom: 1px solid #e4e8eb; }
.brand-title { font-size: 16px; font-weight: 700; }
.card { background: #fff; border: 1px solid #e4e8eb; border-radius: 8px; padding: 14px; }
.card-title { font-size: 13px; font-weight: 700; margin-bottom: 8px; color: #686e74; }
.app-footer { margin-top: auto; text-align: center; font-size: 11px; color: #8e959d; }
`,
  'utf-8'
);

fs.writeFileSync(
  path.join(dir02, 'sidebar/sidebar.js'),
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

generateIconsForDir(path.join(dir02, 'icons'), [0, 199, 60]);

// ============================================================
// 3. 03_사용방법_심사안내
// ============================================================
fs.writeFileSync(
  path.join(dir03, '03_사용방법_심사안내.md'),
`# [제출물 03] 사용 방법 또는 심사용 안내

> **작성 안내:** 심사위원이 직접 확장 프로그램을 설치하고 테스트할 수 있도록 작성하는 안내서입니다.

---

### 1. 확장 프로그램 설치 및 실행 방법
1. **네이버 웨일 브라우저**를 실행합니다.
2. 주소창에 \`whale://extensions\`를 입력하고 이동합니다.
3. 우측 상단의 **[개발자 모드]** 토글을 켭니다.
4. 좌측 상단의 **[압축해제된 확장앱 로드]** 버튼을 클릭합니다.
5. 제출물 ZIP 파일 압축 해제 폴더(또는 \`02_제작_결과물\` 폴더)를 선택합니다.
6. 웨일 우측 **사이드바 툴바**에 생성된 \`${appName}\` 아이콘을 클릭하여 실행합니다.

---

### 2. 로그인 및 계정 필요 여부
- **로그인 필요 여부:** 필요 없음 (또는 필요 시 테스트 계정 안내)
- **테스트 계정 정보:** (필요한 경우 기재)

---

### 3. 심사위원을 위한 주요 시나리오 체험 가이드
1. **1단계 (시작):** 
2. **2단계 (핵심 기능 실행):** 
3. **3단계 (결과 확인):** 
`,
  'utf-8'
);

// ============================================================
// 4. 04_최종_배포_제출 안내
// ============================================================
fs.writeFileSync(
  path.join(dir04, 'README.md'),
`# [제출물 패키지 보관함]

이 폴더는 최종 제출 시 필요한 산출물을 모아두는 곳입니다.

- \`[제출물 01] 기획의도_${folderName}.pdf\`
- \`[제출물 02] 제작결과물_${folderName}.zip\`
- \`[제출물 03] 사용방법_${folderName}.pdf\`

---

### 📦 제작 결과물 자동 압축 명령어:
\`\`\`bash
node scripts/package-project.mjs ${folderName}
\`\`\`
위 명령어를 실행하면 \`02_제작_결과물\` 폴더가 \`${folderName}_제작결과물.zip\`으로 자동 패키징됩니다.
`,
  'utf-8'
);

console.log(`\n🎉 완전 독립 프로젝트 슬롯이 생성되었습니다: projects/${folderName}`);
console.log(`\n📂 폴더 구성:`);
console.log(`   ├── 01_기획_및_지침/        (기획의도 A4 1페이지 초안)`);
console.log(`   ├── 02_제작_결과물/        (실제 실행 가능한 확장앱 소스 - whale://extensions 로드 대상)`);
console.log(`   ├── 03_사용방법_심사안내/  (심사용 테스트 가이드 초안)`);
console.log(`   └── 04_최종_배포_제출/     (제출용 ZIP 및 PDF 보관함)\n`);
