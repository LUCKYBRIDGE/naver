# 다른 기기에서 이어받기

작성일: 2026-10-07. 현재 단계: 기획·기술 검증 준비.

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

사용 안내서는 목표 시나리오 초안이다. exe·F9·가상 키보드·시뮬레이션이 현재 동작한다고 판단하지 않는다.

## 현재 구현과 첫 과제

`02_제작_결과물/`에 MV3 Manifest, Service Worker, Content Script, Sidebar, 아이콘만 있다. 스크립트는 기본 로그 수준이고 Native 호스트·독립 입력 엔진·가상 키보드·한글 조합기는 아직 없다.

첫 과제는 다음 두 기술 검증이다.

1. **P1a:** 다른 앱과 다른 웨일 창에서 교사가 타이핑하는 동안, 비활성 학생 웨일 탭에 CDP 클릭·스크롤·문자를 전달해도 교사 포커스가 유지되는지 확인한다.
2. **P1b:** 실제 전자칠판 USB 터치에서 합성 마우스 훅과 비활성 입력 수신 창 후보를 각각 시험하여 커서 이동·숨김·포커스 탈취를 막을 수 있는지 확인한다.

두 검증을 통과하면 Native Messaging 연결·세션·OFF/F9, 좌표·마우스 조작, 가상 키보드·한글 조합 순으로 구현한다. 등록 레지스트리 위치와 HWND/windowId/tabId 연결은 Whale에서 직접 확인한다.

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
