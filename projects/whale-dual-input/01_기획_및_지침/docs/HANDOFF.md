# 다른 기기에서 이어받기

> **2026-10-08 최신 인계:** [개발 준비 v1.1](DEVELOPMENT_PREPARATION_V1_1.md) → [아키텍처 v7.3](ARCHITECTURE_V7_3_MONITOR2.md) → [실행계획](DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md)를 먼저 읽는다. 아래 2026-10-07 기록·v4·수동 교정 결정은 보존된 과거 이력이다. 현재 사용자 요청은 새 계획 확인 및 개발 준비이며, 과거의 '오류를 수정하지 말고 GitHub에 올린다' 지시를 이번 작업 지시로 재사용하지 않는다. 이번 세션 결과는 문서 끝에 추가했다.

작성일: 2026-10-07. 현재 단계: 0.2.0 통합 구현 진행, 핵심 Windows 입력 분리 미완성.

## 최신 사용자 지시 — 다음 기기에서 가장 먼저 읽기

2026-10-07 Windows Whale 시험 후 사용자가 요구사항을 다시 명확히 했다. **사이트 주소 지정과 웹페이지 영역 수동 드래그를 요구하는 현재 흐름은 원하는 제품이 아니다.** 아래 내용이 기존 v4 및 이 문서의 사이트별 선택·수동 교정 결정에 우선한다.

- 학생용 웨일 브라우저 창에서 어느 웹사이트로 이동하든 터치와 페이지 내 가상 키보드를 사용할 수 있어야 한다. 사이트마다 주소를 입력하거나 허용·선택하는 과정을 정상 사용 흐름으로 요구하지 않는다.
- 사용자가 웹페이지 영역을 드래그하여 좌표를 교정하는 과정을 요구하지 않는다. 대상 창과 웹페이지 영역의 식별·좌표 변환은 구현에서 처리해야 한다.
- 교사의 기존 물리 마우스 커서·키보드·업무 앱 포커스를 처음부터 보존해야 한다. 학생의 웹페이지 입력과 교사의 업무 입력이 동시에 독립적으로 동작하는 것이 핵심이다.
- 페이지 이동, 창 크기, 브라우저 배율, 사이드바 및 브라우저 안내줄로 인한 영역 변화도 설계에서 다뤄야 한다. 현재의 수동 지정 절차를 조금 편하게 만드는 것으로 목표를 대체하지 않는다.
- 임의 사이트 지원이 브라우저 내부 페이지·시스템 대화상자까지 제어한다는 뜻은 아니다. 지원 가능 범위와 브라우저 보안 제약을 확인하고 명시한다. 필요한 권한은 최소 범위와 사용자 설명을 검토한 뒤 결정한다.

**현재 사용자의 작업 지시:** 지금 오류를 바로 수정하지 말고 현재 소스와 이 요구사항·시험 결과를 GitHub에 올린다. 사용자가 다른 컴퓨터에서 내려받아 이어서 개발한다. 이번 인계에서는 제품 코드·Manifest 권한을 변경하지 않았다.

### 이 컴퓨터에서 확인한 상태와 미해결 현상

- GitHub 기준 소스: `c4b2b1c740ed690a6797d855583533b9d0a2cd54`. 시험 패키지: `v0.2.0-preview.1`, `whale-dual-input-0.2.0-20261007-073438`.
- 확장앱 로드와 Windows Native 프로그램 연결에 성공했고 Windows 제어창이 표시됐다. 최초 로드 실패는 패키지 최상위가 아닌 `extension` 하위 폴더를 선택하여 해결했다.
- 사이트 선택·영역 지정 후 ON을 누르자 웨일 상단에 디버깅 안내줄이 나타났고, 상태가 `OFF · 페이지 크기·배율이 변경되었습니다.`로 표시됐다. 독립 입력이 정상 실행된 것으로 볼 수 없다.
- 코드상 `background/session.js`는 선택 시 viewport 크기를 기록하고 수동 교정 후 `start()`에서 디버거를 연결한다. `content/content-script.js`는 ACTIVE 상태의 resize에 자동 해제를 요청한다. 디버깅 안내줄로 인한 높이 변화가 원인 후보지만 실기기 재현으로 확정하지 않았으며 수정도 하지 않았다.
- 교사 커서·키보드 보호와 전자칠판의 실제 독립 터치 성공은 확인하지 않았다. Native 입력 엔진의 기존 한계도 남아 있다.
- 이전 코드 확인 시 `npm test` 14개 및 `npm run build`는 통과했다. 이는 실제 입력 격리 검증이 아니다. 이번 인계 변경은 문서뿐이다.

### 다음 기기에 전달할 요청문

> AGENTS.md와 HANDOFF.md의 최신 사용자 지시를 먼저 읽어 주세요. 현재 사이트 지정·수동 영역 드래그 중심 구현을 확정 설계로 취급하지 마세요. 교사 PC의 학생용 웨일 창에서 사이트를 이동해도 터치·가상 키보드가 교사 커서·키보드·업무 포커스와 독립적으로 동작하고, 사용자가 웹페이지 좌표를 교정하지 않아도 되는 구조를 검토한 뒤 구현해 주세요. 현재 ON 직후 자동 OFF 현상과 입력 엔진의 미해결 사항도 함께 확인해 주세요.

## 사용자가 확정한 핵심 요구사항

교사가 업무하는 Windows PC 한 대에 업무용 모니터와 전자칠판이 연결되어 있다. 전자칠판 자체 OS의 브라우저가 아니라 **해당 교사 PC에서 실행되는 NAVER Whale**을 조작한다.

학생이 전자칠판에서 지정된 웨일 창에 터치·가상 키보드 입력을 해도 교사는 기존 마우스·키보드로 하던 일을 계속해야 한다.

- 교사 커서 이동·숨김·키보드 포커스 탈취·타이핑 누락·학생 문자 혼입 금지.
- 학생 입력은 대상 웨일 웹페이지에 한정. 클릭·드래그·스크롤과 한글·영문·숫자 입력 지원.
- 커서·포커스를 탈취했다가 복원하는 방법은 요구사항을 충족하지 않음.
- 전역 SendInput·OSK·교사 IME 전환 없이 구현 가능성을 검증.
- 전자칠판은 HDMI/USB 외부 화면 모드. 화면 복제·확장을 구분하여 검증.

## 문서 기준

1. [루트 AGENTS.md](../../../../AGENTS.md)와 [프로젝트 README](../README.md)를 읽는다.
2. 현재 기술 기준은 [구현 계획 v4](../whale_dual_input_implementation_plan_v4.md)다.
3. [PREPARATION](PREPARATION.md), [TEST](TEST.md), [PRIVACY](PRIVACY.md), [UX](UX.md)를 참조한다.
4. v3와 루트 v2는 과거 기획 이력이다. 훅만으로 완전 분리를 보장한다는 표현은 실증 결과가 아니다.

Windows EXE·F9·가상 키보드 코드는 생성되었으나 Windows 실행과 핵심 입력 격리는 아직 미완료다. [입력 엔진 판단](INPUT_ENGINE_DECISION.md)을 먼저 읽는다. 설치 흐름은 [INSTALL_WINDOWS](INSTALL_WINDOWS.md), 실험 도구는 [P1A_RUNBOOK](P1A_RUNBOOK.md)을 따른다. 시뮬레이션 모드는 없다.

## 현재 구현과 첫 과제

`background/session.js`, `content/hangul.js`, 페이지 키보드, 사이드바, `native/WhaleDualInput.Host/`를 연결했다. Native 호스트는 .NET 10 WinForms/Win32 기반이며 Native 등록·viewport 지정·터치 수신 후보·F9·감시 종료를 구현했다. `npm run build:windows`는 win-x64 EXE와 제품 확장앱 ZIP을 만든다. 핵심 입력 분리 후보의 한계가 확인되어 실제 완성본으로 부르지 않는다.

첫 과제는 다음 두 기술 검증이다.

1. **P1a:** 다른 앱과 다른 웨일 창에서 교사가 타이핑하는 동안, 비활성 학생 웨일 탭에 CDP 클릭·스크롤·문자를 전달해도 교사 포커스가 유지되는지 확인한다.
2. **P1b:** 실제 전자칠판 USB 터치에서 합성 마우스 훅과 비활성 입력 수신 창 후보를 각각 시험하여 커서 이동·숨김·포커스 탈취를 막을 수 있는지 확인한다.

### 이어받을 현재 상태

- 목표: 교사 커서·포커스·문자 입력을 처음부터 보존하는 실제 독립 입력. 현재 마일스톤: 통합 구현 코드·배포 생성, 입력 수신 방식 보완 필요.
- 사용자 지시: 테스트용 골격에서 끝내지 않고 본 프로그램을 실제로 완성한다. 기능 구현을 먼저 진행하고 완성된 동작에 대해 검사한다. Windows PC와 전자칠판을 모두 보유함.
- 변경 파일: 제품 Manifest/background/content/sidebar, Windows Native 호스트, `build-windows.mjs`, 기존 P1a 도구, 설치·권한·현재 판단 문서.
- 의사결정: .NET 10 자체 포함 win-x64, 사이트별 선택 권한, 수동 viewport 교정, CDP Input, 학생 한글 조합. 터치 수신 창 B는 확정된 독립 입력 엔진이 아니다. HTTRANSPARENT와 커서 억제의 한계를 보완해야 한다.
- 다음 즉시 작업: Windows 접근 경로 확보 후 입력 엔진·Native 등록 경로를 확정하고 제품 코드 수정. 현재 ZIP/빌드 통과를 완성으로 보고하지 않는다.
- 사용자 요청으로 소스는 GitHub main에 커밋·push하고 Windows ZIP은 시험용 prerelease에 첨부한다. 다음 작업 전에 원격 최신 커밋과 Releases를 확인한다.

최신 사용자 지시로 통합 기능을 먼저 구현했다. 기존 P1a/P1b 계획은 후속 검증 기준으로 유지한다. 등록 레지스트리 위치와 HWND/windowId/tabId 연결, 교사 입력 보호는 Windows에서 확인하고 보완한다.

단순 dispatchEvent, 우클릭 시뮬레이션, API 성공 응답만으로 실기기 성공을 선언하지 않는다. 기술 후보가 실패하면 결과를 기록하고 후보를 재검토한다.

## 새 기기 준비

```powershell
git clone https://github.com/LUCKYBRIDGE/naver.git
cd naver
```

문서·코드는 상대 경로로 관리한다. 새 기기에 `C:\ai_dev\apps\naver` 경로를 똑같이 만들 필요는 없다. PREPARATION의 개발 PC 정보는 작성 당시의 관찰값이며 새 기기에서는 다시 확인한다.

- 기획 열람: 추가 의존성 설치 없음.
- 현재 확장앱 로드: NAVER Whale에서 `projects/whale-dual-input/02_제작_결과물` 선택.
- 개발 도구: Git·Node.js, Native 구현 시 .NET SDK와 Windows 환경. 생산 런타임 후보는 v4 확인.
- 실증에 필요한 정보: 전자칠판 모델·USB 장치 인식·학교 PC Windows 버전·실제 Whale 버전·복제/확장·화면 배율. 현재 미확인.
- 기존 package-project 스크립트는 ZIP 도구이며 exe를 빌드하지 않음.

## 기기 간 변경 동기화

작업 시작 전에 저장소 루트에서 실행한다.

```powershell
git pull --ff-only
git status --short
```

작업 후에는 변경 파일을 확인하고 커밋·업로드한다.

```powershell
git add <변경한 파일 또는 폴더>
git commit -m "Describe the change"
git push
```

로컬 변경이 있거나 pull 충돌이 나면 내용을 먼저 보존·확인한다. force push나 reset으로 다른 기기 작업을 덮어쓰지 않는다. 비밀키·환경 파일·학생 데이터는 저장소에 넣지 않는다.

다음 에이전트에게는 이 문서 상단의 **다음 기기에 전달할 요청문**을 전달한다. 아래에 보존된 기존 구현 결정과 v4는 최신 사용자 지시에 맞춰 재검토한다.

## 2026-10-08 개발 준비 v1.1 세션

- 기준선: `main` / `a5f0d23dae251d2ccb57494001c3b40b73241b56`. 로컬 변경만 수행, 커밋·push 없음. 제공 인계 폴더와 기존 untracked ARCHITECTURE_V5.md 보존.
- 문서: 최신 기술 원문 2개를 SHA-256 일치로 복사, UI/design/tasks/QA 4개 연결본 생성. [준비 현황](DEVELOPMENT_PREPARATION_V1_1.md), [범위·충돌 결정](IMPLEMENTATION_SCOPE.md), [파일별 점검](CODE_REUSE_AUDIT.md), [UIAccess 판단](UIACCESS_POLICY_DECISION.md), [설치 여건](INSTALLATION_FEASIBILITY.md), [공식 자료](RESEARCH_REFERENCES.md) 작성. 프로젝트 AGENTS와 진입 문서를 갱신했다.
- 구현 변경: 제품 기능·Manifest는 변경하지 않았다. `scripts/build-windows.mjs`의 사용자 로컬 SDK 탐색이 Windows의 dotnet.exe를 찾도록 수정했다.
- 환경: Node v24.15.0, npm 11.12.1. 시스템 .NET SDK 8.0.424는 유지하고 `%USERPROFILE%\.local\share\naver-dotnet`에 10.0.401 준비. dotnet.exe Microsoft 서명 Valid·실행 확인. Whale 실행 파일 5.39.412.57 확인, 브라우저 시험 미수행.
- 화면 관측: Windows.Forms가 DISPLAY2 primary 1920×1080 (0,0), DISPLAY3 1920×1080 (1920,0)를 반환했다. Windows 표시 번호·전자칠판 장치 매핑과 동일하다고 가정하지 않는다.
- 검사: `npm test` 14개 PASS/0개 FAIL/종료 코드 0. `npm run build:windows` 종료 코드 0(내부 JS/MV3 제품·프로브 분리 빌드, .NET win-x64 자체 포함 publish 및 ZIP 생성 포함).
- 문서 검사: 22개 문서의 로컬 Markdown 참조 101개 정상, 기술 원문/사본 4개 SHA-256 일치, `git diff --check` 통과. 제품 소스 경로의 Git diff는 비어 있다.
- 생성 패키지: `04_최종_배포_제출/whale-dual-input-0.2.0-20261008-074003.zip`. 기존 v0.2.0 기준선 빌드이며 새 원본 격리·자동 매핑 구현/검증 패키지가 아니다. EXE를 실행하거나 Native 등록하지 않았다.
- 실제 칠판 시험: 미수행. **G1 NOT TESTED / G2 NOT TESTED**. 기존 ON 직후 OFF 현상은 재현·수정하지 않았다. 자동 검사 PASS가 Native 교사 보호 또는 실제 Whale 호환을 뜻하지 않는다.
- 남은 장애: UIAccess 목적·서명·학교 설치 조건 미확인, 실제 포인터 타입·장치 매핑 미확인, 새 Probe·ProtectionStateManager·자동 어댑터 미구현. 기존 ACTIVE 표시와 OFF 추정은 최신 보호 증거 계약을 충족하지 않는다.
- 다음 작업: codex/ 구현 브랜치에서 관찰 전용 WhaleDualInput.Probe(T-010/011)를 구현한다. M0.4 정책/설치 조사를 함께 진행하고 M1 자동 매핑·M4a 실제 Whale 최소 실험을 준비한다. Tier 선택 및 C2 소비 전용 PoC 뒤 실제 G1을 판단한다.

### 후속 GitHub 반영 요청

2026-10-08 사용자가 준비 완료 후 GitHub 저장소 반영을 요청했다. `origin/main`과 준비 기준선의 일치를 fetch로 확인했다. 제공 인계 v1.1 원본·준비 문서·진입 지침과 Windows 로컬 SDK 탐색 수정을 반영 대상으로 한다. 문서 준비와 빌드 도구 수정은 별도 커밋으로 구분한다. 준비 전부터 존재한 미추적 `ARCHITECTURE_V5.md`는 이번 반영에 포함하지 않고 로컬에 보존한다. SDK·EXE·ZIP은 로컬 산출물이며 소스 커밋에 넣지 않는다. 실제 G1/G2 미검증 상태는 그대로다.
