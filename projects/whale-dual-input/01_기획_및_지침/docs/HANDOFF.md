# 다른 기기에서 이어받기

작성일: 2026-10-07. 현재 단계: 0.2.0 통합 구현 진행, 핵심 Windows 입력 분리 미완성.

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

다음 에이전트에게는 “AGENTS.md와 Whale Dual Input HANDOFF·v4 계획을 읽고, 교사 입력 보호를 최우선으로 P1a/P1b 기술 검증부터 진행해 줘”라고 요청하면 된다.
