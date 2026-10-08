# 🐳 웨일스페이스 수업도구 공모전 멀티 프로젝트 워크스페이스

네이버 웨일 / 웨일스페이스(WhaleSpace) 수업도구 공모전을 위해, **각 결과물(프로젝트)별로 기획서, 소스코드, 심사안내서가 서로 절대 뒤섞이지 않도록 완전히 격리된 멀티 프로젝트 개발 환경**입니다.

## 다른 기기에서 이어서 개발하기

```powershell
git clone https://github.com/LUCKYBRIDGE/naver.git
cd naver
```

현재 작업 프로젝트는 **Whale Dual Input**입니다. 교사 Windows PC에 연결된 전자칠판에서 학생이 웨일을 조작하는 동안 교사의 커서·키보드 포커스·타이핑을 보호하는 기능을 기획하고 있습니다.

현재 단계는 **2026-10-08 개편 계획 v1.1의 개발 준비 완료, 기능 개편 착수 전**입니다. 최신 목표는 화면 확장 모드에서 모니터 2의 전자칠판 터치 원본을 자동 식별·격리하고 Whale 웹 콘텐츠에 전달하는 것입니다. 수동 영역 교정과 사이트별 주소 등록을 일상 사용에서 없앱니다. [개발 준비 현황·다음 구현 과제](projects/whale-dual-input/01_기획_및_지침/docs/DEVELOPMENT_PREPARATION_V1_1.md)부터 읽으세요. 현재 v0.2.0 코드는 재사용 기준선이며 실제 입력 보호(G1)와 물리 터치 웹 동작(G2)은 이번 준비에서 검증하지 않았습니다.

Node.js 24 이상에서 `npm test`, `npm run build`로 검사합니다. `npm run p1a:serve`로 로컬 시험 페이지를 제공하며, 실행 절차는 [P1a 실험 안내](projects/whale-dual-input/01_기획_및_지침/docs/P1A_RUNBOOK.md)를 따릅니다.

Windows 프로그램과 확장앱의 실제 설치는 [Windows 설치·사용 안내](projects/whale-dual-input/01_기획_및_지침/docs/INSTALL_WINDOWS.md)를 따릅니다. `.NET 10 SDK`와 Node.js 24 이상에서 `npm run build:windows`를 실행하면 자체 포함 win-x64 실행 파일과 제품 확장앱을 별도 ZIP으로 생성합니다. 출력 위치는 `projects/whale-dual-input/04_최종_배포_제출/whale-dual-input-0.2.0-<빌드시각>.zip`입니다.

Windows 시험용 ZIP은 [GitHub Releases](https://github.com/LUCKYBRIDGE/naver/releases)에서 받습니다. ZIP 안에 EXE·extension·설치 안내가 포함됩니다. 개발 SDK 없이 받을 수 있습니다. 현재 핵심 입력 분리는 [미해결 사항](projects/whale-dual-input/01_기획_및_지침/docs/INPUT_ENGINE_DECISION.md)이 있어 완성본으로 표시하지 않습니다.

처음 이어받을 때 다음 순서로 읽어 주세요.

1. [루트 개발 지침](AGENTS.md)
2. [프로젝트 최신 지침](projects/whale-dual-input/AGENTS.md)과 [개발 준비 현황](projects/whale-dual-input/01_기획_및_지침/docs/DEVELOPMENT_PREPARATION_V1_1.md)
3. [아키텍처 v7.3](projects/whale-dual-input/01_기획_및_지침/docs/ARCHITECTURE_V7_3_MONITOR2.md)와 [최신 실행계획](projects/whale-dual-input/01_기획_및_지침/docs/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md)
4. [작업 인계·과거 실패 기록](projects/whale-dual-input/01_기획_및_지침/docs/HANDOFF.md)과 [최신 판정 시험](projects/whale-dual-input/01_기획_및_지침/docs/ACCEPTANCE_TESTS_V1_1.md)

기획 문서를 읽는 데 추가 설치는 필요하지 않습니다. 현재 v0.2.0 확장앱은 패키지의 `extension` 폴더 또는 개발 빌드의 `dist/whale-dual-input/extension`을 로드합니다. 이 과거 구현에는 Windows 연결 등록과 영역 교정이 남아 있어 최신 UX를 충족하지 않습니다. 원래 기기와 같은 절대 경로로 복제할 필요는 없습니다. 이번 Windows 개발 PC에는 사용자 로컬 .NET 10 SDK를 준비했으며 `build:windows`가 자동으로 찾아 사용합니다. 다른 PC에서는 .NET 10을 별도로 준비해야 합니다.

작업 전 `git pull --ff-only`로 최신 변경을 받습니다. 작업을 마치면 변경 사항을 커밋하고 `git push`한 뒤 다른 기기에서 이어갑니다.

---

## 🗂️ 결과물별 완전 격리 구조

각 프로젝트는 `projects/<프로젝트명>/` 아래에 독립적으로 생성되며, **공모전 필수 제출물 3종 규격과 1:1로 정확히 대응**됩니다:

```text
projects/<프로젝트명>/
├── 01_기획_및_지침/
│   ├── README.md               # 📌 이 프로젝트만의 전용 목표 및 개발 지침
│   └── 기획의도_A4_1페이지.md   # 📄 [제출물 01] 문제의식, 핵심기능, 기대효과 (A4 1장 규격)
│
├── 02_제작_결과물/              # 🚀 [제출물 02] 실제 실행 가능한 확장앱 소스코드
│   ├── manifest.json            # ⚙️ 독립 실행 매니페스트 (MV3, sidebar_action)
│   ├── background/service-worker.js
│   ├── content/content-script.js
│   ├── sidebar/sidebar.html, sidebar.js, sidebar.css
│   └── icons/ (16, 48, 128 PNG)
│
├── 03_사용방법_심사안내/        # 🔍 [제출물 03] 심사위원을 위한 평가 및 사용 가이드
│   └── 03_사용방법_심사안내.md   # 📄 설치 절차, 테스트 계정, 시나리오 안내
│
└── 04_최종_배포_제출/           # 📦 최종 제출용 보관함
    └── (제출용 ZIP 파일 및 변환된 PDF가 보관되는 공간)
```

---

## 🚀 새 프로젝트 생성 명령어

명령어 한 줄로 위 격리 폴더 4종과 필수 템플릿 파일이 한 번에 자동 생성됩니다:

```powershell
node scripts/create-project.mjs <폴더명> "<프로젝트이름>"
```

*예시:*
```powershell
node scripts/create-project.mjs my-tool "웨일 클래스 과제 도우미"
```

생성 직후:
1. `01_기획_및_지침/기획의도_A4_1페이지.md`에서 기획 내용을 작성합니다.
2. `02_제작_결과물/` 폴더를 웨일 브라우저(`whale://extensions`)의 [압축해제된 확장앱 로드]로 열어 즉시 실행 및 개발합니다.
3. 작업이 끝나면 `node scripts/package-project.mjs my-tool`을 실행하여 제출용 ZIP을 생성합니다. Whale Dual Input은 실험 소스를 제외하고 Windows 호스트와 제품 확장앱을 분리하는 `npm run build:windows`를 사용합니다.

---

## 🔒 작업 뒤섞임 방지 규칙

1. **지침 격리:** 각 프로젝트의 고유 기획과 세부 지침은 해당 프로젝트의 `01_기획_및_지침/` 내에만 둡니다.
2. **코드 격리:** 확장 프로그램 코드는 오직 `02_제작_결과물/` 내에서만 동작하며 다른 프로젝트의 파일을 직접 임포트하지 않습니다.
3. **공통 기능 재사용:** 스토리지, 메시징 등 기반 유틸이 필요할 때만 `packages/shared/`를 참조합니다.
4. **추후 프로그램 통합(Merge) 시:** 두 프로그램을 합친 새로운 통합 결과물이 필요할 때도 기존 프로젝트를 건드리지 않고, 새로운 독립 슬롯(예: `projects/unified-tool/`)을 별도로 만들어 합치므로 기존 작업물이 절대 훼손되지 않습니다.
