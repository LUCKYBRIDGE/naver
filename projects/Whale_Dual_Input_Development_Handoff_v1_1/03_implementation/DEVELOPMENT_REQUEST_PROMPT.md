# 코딩 AI / 개발자에게 전달할 단일 개발 요청문

아래 **[복사용 요청문]**부터 끝까지 복사하거나 이 파일과 전체 ZIP을 첨부하면 된다. 기존 GitHub `LUCKYBRIDGE/naver` 프로젝트를 새 저장소로 바꾸라는 지시가 아니다.

---

## [복사용 요청문]

`LUCKYBRIDGE/naver` 저장소의 **Whale Dual Input** 프로그램을 개발해 주세요. 첨부한 **Whale_Dual_Input_Development_Handoff_v1_1** 폴더의 문서를 먼저 전부 확인하고, 실제 저장소의 `AGENTS.md`, `projects/whale-dual-input/01_기획_및_지침/docs/HANDOFF.md`, `manifest.json`, 사이드바/백그라운드/콘텐츠/Windows Native 소스와 빌드 스크립트를 대조해 주세요.

### 1. 확정 제품 방향

- **한 대의 Windows PC**, 확장 모드 모니터 1(교사 업무), 모니터 2(전자칠판).
- **모니터 2에 연결된 물리 터치 장치의 모든 접촉을 학생 전용 논리 입력으로 격리**하는 것이 목표. 화면 사각형 좌표를 드래그·선택하는 방식은 사용하지 않습니다.
- 입력 전달 대상은 **모니터 2의 Whale 일반 웹페이지와 학생용 별도 탐색 UI**로 한정합니다. PowerPoint, 그림판 등 Windows 앱 전체에 독립 포커스를 만들었다고 주장하지 마세요.
- 교사의 물리 마우스는 모니터 1·2 어디서든 원래 Windows 커서로 정상 움직이고 클릭할 수 있어야 합니다. 교사의 실제 키보드 입력란은 학생 터치 때문에 바뀌거나 글자가 섞이면 안 됩니다.
- ON 중 학생 터치가 교사 커서를 움직였다가 뒤늦게 복원하는 기술은 **실패**입니다. 마우스 좌표 기준 `WH_MOUSE_LL` 전체 차단, 전역 `SendInput`, `SetCursorPos`, `SetForegroundWindow` 등의 임시 포커스 탈취/복원도 금지합니다.
- **매 사이트 URL 등록·페이지 영역 수동 보정·드래그 선택·제조사별 모델 선택을 일반 사용자 흐름에서 제거**합니다.
- 장치나 Windows 보호 경로가 불확실할 때는 `ACTIVE`를 거절하고 안전한 사유와 조치를 표시하세요. 제조사 상관없이 100% 지원한다고 주장하지 마세요.

### 2. 웨일 확장 프로그램 UI

기존 `manifest.json`의 **MV3 + `sidebar_action`** 구조를 우선 유지하고 `action`을 추가로 동시에 선언하지 마세요. `02_ui/ON_OFF_UI_SPEC.md`, `02_ui/design.md`, `06_mockup/index.html`에 따라 **교사용 사이드바 ON/OFF 인터페이스**를 구현합니다.

기본 화면: `웨일 듀얼 인풋` 제목, 현재 입력 보호 상태, `모니터 2/전자칠판 터치/학생 웨일 웹` 3행 연결 상태, **주 행동 버튼 1개**, `교사 일시 해제`(ACTIVE만), 펼치는 진단 상세.

`OFF`, `DISCOVERING`, `PREPARING`, `ACTIVE`, `PAUSED_BY_TEACHER`, `SAFE_HOLD`, `LOST_PROTECTION`, `UNSUPPORTED`를 모두 구현하세요. Native 미설치/복제 화면/터치 매핑 오류/학교 정책 충돌/CDP 장애는 reasonCode별 교사에게 이해되는 문구로 안내합니다. `ACTIVE`는 물리 원본 격리 및 Web Adapter의 **실제 ACK/상태 확인** 후에만 표시합니다. 확장앱 Worker 재시작/사이드바 재오픈 때는 Native 상태를 다시 읽고, 저장된 마지막 상태로 성공을 추정하지 마세요.

교사 일시 해제는 위험 확인 후 Windows 일반 터치로 복귀하며, 일시 해제 중에는 교사 커서에 영향을 줄 수 있음을 표시해야 합니다. `LOST_PROTECTION`에서는 터치 중단을 강조하고 `SAFE_HOLD`로 속여서는 안 됩니다.

### 3. 기술 기준과 순서

**최상위 기술 기준:** `05_references/ARCHITECTURE_V7_3_MONITOR2.md`  
**실행계획:** `05_references/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md`  
**UI/작업/QA:** `01_product/PRODUCT_SCOPE.md`, `02_ui/*`, `03_implementation/IMPLEMENTATION_TASKS.md`, `04_quality/ACCEPTANCE_TESTS.md`.

- **M0 / M0.4:** 현재 코드 보존 및 재사용 점검, UIAccess 접근성 용도·서명·Windows 보안정책 적합성, Tier B 필요 시 드라이버 서명/설치 현실성 조사.
- **M0.5:** 실제 터치 장치·모니터 2 매핑·포인터 타입·Raw Input·교사 커서/포커스 변화를 관측하는 진단 도구와 양성 대조군 구현.
- **M1:** 모니터 2를 자동 식별하고 불확실한 장치/복제 화면이면 격리 ON 금지.
- **M2a:** Tier A가 기술·정책상 후보일 때만 `RegisterPointerInputTarget` 실기기 시험; 먼저 입력을 **소비만** 하여 G1 교사 보호부터 검증. Tier A 실패/부적합 시 Tier B의 서명/안전 설치 결정 게이트 또는 NO-GO 선택. 보호 실패를 임의 우회하지 마세요.
- **M2b / M3:** 실제 보호 경로 확인 후 `SeatEventV1`, 논리적 마우스 2, 접촉 ID/멀티터치, 이벤트 큐·재연결·상태 관리.
- **M4a:** 실제 Whale CDP/Native Messaging/Web Adapter 가능성을 **합성 입력으로 선행 시험 가능**. 다만 이를 물리 G1 성공으로 보고하지 마세요.
- **M4b:** 격리한 실제 전자칠판 이벤트를 Whale 웹페이지와 학생 탐색/가상 키보드에 전달. 교사의 실제 포커스/커서 보호를 함께 실증.
- **M5:** Windows Native 설치·제거·복구, MV3 패키징, 가벼운 실행, 로그 개인정보 최소화, 실기기/저사양/다른 장치 호환성 및 공모전 제출 규정 점검.

### 4. 개발·저작권·보안 규칙

- 모든 기존 소스를 임의 삭제·전면 교체하지 않고 **작업 브랜치/워크트리**에서 점진적으로 수정합니다. 현재 `HANDOFF.md`에 남은 과거 ON 직후 OFF 실패 기록을 보존하되 최신 설계가 우선함을 적으세요.
- 공개 MouseMux V2 MIT SDK 예제의 추상 구조는 참고 가능하지만, **상용 MouseMux 엔진·드라이버·비허가 Chromium 패치의 코드/프로토콜/바이너리는 사용하지 마세요**. 공식 Windows/Chromium/Whale API 기반 자체 구현이어야 합니다.
- 사용자 키 입력·페이지 URL·학생/교사 개인정보를 외부 전송하지 않고, 기본 진단 로그에도 저장하지 마세요.
- 프로그램 상태를 UI에 표시하려면 **실제 Native 격리 상태가 권위 있는 출처**여야 합니다. `SAFE_HOLD`는 보호 격리가 실제 살아 있는 때만, 격리 소멸은 `LOST_PROTECTION`으로 구분하세요.
- `npm test`, `npm run build`, `npm run build:windows`는 실제 파일 확인 후 사용하되 하드웨어 G1 시험 통과를 증명하는 것으로 간주하지 마세요.

### 5. 요구하는 개발 산출물과 매 작업 보고

각 단계별로 수정 소스·기능 설명·자동 검사 결과·Windows 실기기 검증 여부·G1/G2 PASS/FAIL/NOT TESTED·알려진 제약·다음 작업을 기록해 주세요. 실제 전자칠판이 연결되지 않은 환경에서 작업했다면 그 사실과 미실행 시험을 명확히 남겨 주세요.

**최종 완료 기준:** 지원 장치에서 최초 Windows 프로그램 설치 후, 교사는 웨일 확장 사이드바의 ON/OFF만 사용하면 되고 **수동 영역 드래그 없이**, 학생이 모니터 2의 웨일 웹페이지를 조작하는 동안 교사는 기존 마우스와 키보드로 계속 업무할 수 있어야 합니다. 이 조건이 실제로 검증되기 전에는 구현 완료·무간섭 보장을 발표하지 마세요.

## [요청문 끝]
