# Whale Dual Input 구현 준비 및 개발 계획 v4.0

- 작성일: 2026-10-07
- 상태: 기획 / 기술 검증 준비. 독립 입력 기능의 구현·실기기 검증 전.
- 기준 환경: 교사가 업무하는 Windows PC 한 대 + 업무용 모니터 + HDMI·USB로 연결된 전자칠판 + 해당 PC에서 실행되는 NAVER Whale.
- 이 문서는 현재 구현 계획의 기준이다. 기존 개발기획안 v3는 기획 이력으로 보존한다.

## 1. 해결할 문제와 성공 조건

> 교사와 학생이 같은 PC에 연결된 모니터와 전자칠판을 함께 사용하는 상황에서 발생하는 커서·키보드 포커스 충돌을, 학생 입력을 지정된 웨일 웹페이지로만 전달하는 독립 입력 경로로 해결한다.

교사는 한글·PPT·메모장·다른 웨일 창 등에서 하던 일을 계속한다. 학생은 전자칠판에 보이는 지정된 웨일 창에서 클릭·드래그·스크롤하고 가상 키보드로 입력한다. 전자칠판 자체 OS의 브라우저는 대상이 아니다.

| ID | 반드시 만족해야 하는 조건 |
|---|---|
| R1 | 학생 터치 때문에 교사의 시스템 커서가 이동하지 않는다. 이동 후 복원도 실패다. |
| R2 | 학생 터치 때문에 교사의 커서가 숨겨지거나 억제되지 않는다. |
| R3 | 학생 입력 때문에 교사 작업 창과 키보드 입력 대상이 바뀌지 않는다. 잠깐 탈취 후 복원도 실패다. |
| R4 | 교사 문자 누락·중복·변조와 학생 문자의 교사 문서 유입이 없다. 교사의 한글 IME 조합을 유지한다. |
| R5 | 학생은 대상 웨일 웹페이지에서 독립 클릭·드래그·스크롤과 한글·영문·숫자 입력을 수행한다. |
| R6 | OFF·F9·연결 끊김·대상 소실 시 독립 입력을 종료하고 일반 입력 상태로 돌아간다. |

교사가 스스로 창을 바꾸거나 마우스를 움직이는 행동은 정상 입력이다. 이를 학생에 의한 납치와 구분한다. 교사가 업무하는 웨일 창과 학생 대상 웨일 창은 별도로 선택한다. 같은 페이지의 같은 입력란을 두 사람이 동시에 편집하는 기능은 포함하지 않는다.

## 2. 현재 상태

`02_제작_결과물/manifest.json`은 MV3, 모듈 Service Worker, `sidebar_action`, `storage`, `activeTab`을 선언한다. Content Script는 모든 HTTP/HTTPS 페이지에 자동 주입된다.

실제 코드는 설치·실행 로그와 기본 사이드바뿐이다. Windows 프로그램, 통신, 가상 키보드, 한글 조합기, 독립 클릭, F9, 시뮬레이션은 아직 없다. 버전 문자열 `1.0.0`은 기능 완성을 의미하지 않는다. 이번 준비 작업에서는 실행 코드와 Manifest를 변경하지 않는다.

## 3. v3에서 검증 가설로 바꾸는 부분

1. **마우스 훅 차단만으로 모든 터치 문제를 해결한다고 단정하지 않는다.** `WH_MOUSE_LL`은 마우스 이벤트를 다룬다. 원래 터치·포인터 입력과 활성화 경로까지 제어되는지는 별도 실험이 필요하다. [Microsoft LowLevelMouseProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelmouseproc)
2. **터치 서명은 특정 전자칠판의 장치 ID가 아니다.** `0xFF515700` 비교는 펜·터치 계열을 판별하며, 터치 구분에는 `0x80` 비트도 확인해야 한다. 대상 장치와 디스플레이 매핑은 별도 수단으로 식별한다. [Microsoft 입력 구분](https://learn.microsoft.com/en-us/windows/win32/tablet/system-events-and-mouse-messages)
3. **커서 숨김은 독립 검사 항목이다.** 좌표가 그대로여도 `CURSOR_SUPPRESSED`가 발생하면 R2 실패다. [Microsoft CURSORINFO](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-cursorinfo)
4. **DOM 이벤트를 보내는 것과 실제 브라우저 입력은 다르다.** 단순 `dispatchEvent`를 모든 사이트·캔버스·편집기에서 동작하는 입력 엔진으로 간주하지 않는다.
5. **무지연은 목표이며 측정값은 아니다.** 교사 입력 경로의 부가 작업을 최소화하고, 기준 상태 대비 지연과 이벤트 누락을 측정한다.
6. **대상 창 자동 포커싱은 제거한다.** 선택·좌표 확인 과정에서도 교사 작업 창을 강제로 활성화하거나 입력 포커스를 복원하는 보정 방식을 쓰지 않는다.
7. **우클릭 시뮬레이션은 통신·UI 시험에만 쓴다.** 실제 USB 터치 차단·커서 보호 검증을 대체할 수 없다.

## 4. MVP 범위

핵심 기능 하나는 **교사 작업을 보호하는 웨일 독립 입력 세션**이다. 클릭·스크롤·드래그·가상 키보드는 이 경험에 필요한 구성 요소다.

| 구분 | 범위 |
|---|---|
| Must Have | 대상 웨일 창·탭 선택, 입력 격리, 독립 클릭·단일 포인터 드래그·스크롤, 기본 입력란 한글/영문/숫자 입력, OFF/F9/장애 해제 |
| Should Have | 좌표 교정, 학생용 시각 포인터, 연결·지원 여부 표시, 선택 범위 교체, 입력 취소, 최소 진단 카운터 |
| Could Have | 확인된 사이트용 contenteditable 어댑터, OOPIF 지원 확대, 검증된 터치 제스처 |
| Not Now | OS 전체 두 번째 마우스, 브라우저 주소창·탭바·시스템 파일창 조작, OSK/SendInput, 드라이버 개발, AI·로그인·서버·학급 관리 |

DOM 기반 일반 input·textarea를 첫 입력 대상으로 한다. password·결제·인증·파일 입력은 제외하고, 지원하지 않는 편집기와 페이지는 안내한다. 팀보드·웨일 클래스는 후속 실제 호환성 검사 대상이며 지원 완료로 표기하지 않는다. 캔버스 드래그는 시험 페이지에서 먼저 검증한다.

### 화면 연결 방식

- **확장 화면:** 업무 모니터와 전자칠판에 서로 다른 창을 계속 표시할 수 있어 첫 실기기 기준으로 사용한다.
- **복제 화면:** 두 화면에 같은 화면이 보이므로 입력 분리만으로 서로 다른 전체 화면을 제공할 수는 없다. 웨일과 업무 창이 동시에 보이는 배치에서 시험한다. 다른 창에 가려진 웨일을 학생이 계속 볼 수 있다는 약속은 하지 않는다.
- 복제 환경도 요구사항으로 유지한다. 실제 설치 방식 확인 전 임의로 화면 설정을 변경하거나 복제 지원을 완료 처리하지 않는다.

## 5. 기술 후보와 결정 순서

### 5.1 학생 입력을 받는 Windows 경로

| 후보 | 용도 | 채택 조건 |
|---|---|---|
| A. 저수준 마우스 훅 | 터치에서 합성된 마우스 식별·차단 시험 | 원래 포인터 입력·활성화·커서 숨김까지 R1~R4를 모두 통과한 기기에서만 채택 |
| B. 비활성 입력 수신 창 | 웨일 콘텐츠 영역 위에 입력을 받는 창을 두고 원래 터치를 처리 | 교사 물리 마우스를 방해하지 않는 hit-test, 화면 표시, 터치 수신, R1~R4를 모두 통과해야 채택 |
| C. 장치·드라이버 수준 제어 | 사용자 모드 후보 실패 시 추가 조사 | 학교 설치 부담과 별도 드라이버 요구를 검토한 뒤 별도 결정. MVP에 자동 편입하지 않음 |

B는 `WS_EX_NOACTIVATE`, `WM_POINTERACTIVATE`의 `PA_NOACTIVATE`, `WM_MOUSEACTIVATE`의 비활성 처리를 시험하는 후보다. 이 설정들만으로 커서 숨김 방지까지 보장되지는 않는다. 외부 웨일 창의 메시지 처리를 직접 수정한다고 가정하지 않고, 우리가 소유한 수신 창에서 처리한다. [창 스타일](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles), [포인터 활성화](https://learn.microsoft.com/en-us/windows/win32/inputmsg/wm-pointeractivate), [마우스 활성화](https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-mouseactivate)

수신 창의 시각적 투명성과 입력 통과는 별도 문제다. `WS_EX_TRANSPARENT` 하나로 터치만 받고 물리 마우스만 통과시킬 수 있다고 가정하지 않는다. 이 분리가 불가능하면 B는 실패로 기록한다. 장치 식별이 모호하면 독립모드 진입을 거부하며 일반 마우스를 터치로 추정해 차단하지 않는다.

### 5.2 웨일 웹페이지에 입력을 전달하는 경로

**우선 실험 후보:** 확장앱의 `debugger` API를 통한 탭 단위 CDP 입력. 웨일 공식 API 목록은 `whale.debugger`와 Chrome API 호환을 안내한다. 실제 Whale 버전의 지원 범위는 실험으로 확인한다. [Whale 확장앱 API](https://developers.whale.naver.com/api/extensions/)

- `Input.dispatchMouseEvent`: 클릭·버튼 상태·이동·휠 시험.
- `Input.insertText`, `Input.dispatchKeyEvent`: 글자 삽입·삭제·Enter 등 페이지 내부 편집 시험.
- `Input.imeSetComposition`: 조합 중 표시·확정·취소를 위한 추가 실험 후보. 버전별 지원과 편집기 동작을 먼저 확인.
- 명령은 선택된 학생 탭에만 보낸다. 실제 이벤트 신뢰 속성, 기본 동작, 페이지 내부 포커스, Windows 포커스에 미치는 영향은 모두 기록한다.

CDP는 페이지 입력 명령을 제공하지만 **비활성 Whale에서 교사 포커스를 유지한다는 보장은 이번 계획의 검증 가설**이다. `SetForegroundWindow`, `BringToFront`, 전역 키 입력으로 이를 보정하지 않는다. 입력 좌표는 주 프레임 viewport의 CSS 픽셀을 기준으로 한다. [CDP Input 원문](https://raw.githubusercontent.com/ChromeDevTools/devtools-protocol/master/pdl/domains/Input.pdl)

`debugger`는 권한 범위가 크고 브라우저 안내·정책 제한·DevTools 연결 충돌이 발생할 수 있다. 필요한 Input 명령만 허용하고 학교 환경에서 시험한다. 허용되지 않으면 제한된 DOM 어댑터를 별도로 평가한다. 일반 웹페이지 조작이라는 핵심 범위를 조용히 축소하지 않는다. [debugger API](https://developer.chrome.com/docs/extensions/reference/api/debugger)

`PostMessage`를 Chromium 창으로 보내는 방법도 기본 입력과 동일하게 동작한다고 가정하지 않는다. 필요할 때 비교 실험만 한다. 독립 입력을 위해 원격 디버깅 TCP 포트를 기본으로 열지 않는다.

### 5.3 Windows ↔ 확장앱 연결

**우선 후보:** Native Messaging. Windows 호스트와 확장앱 Service Worker 간 연결을 시험하고, 등록 경로·확장앱 ID·학교 정책 호환을 확인한 후 확정한다. Chrome 등록 레지스트리 경로를 Whale 경로라고 추정하지 않는다.

호스트 프로토콜은 길이 헤더가 있는 UTF-8 JSON이고 stdout은 프로토콜에만 사용한다. 로그는 별도 경로로 분리한다. Content Script는 Worker를 통해 통신한다. Worker에서 호출한 호스트의 부모 창 핸들이 유효하다고 가정하지 않는다. [Native Messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging)

호스트가 하나의 제어창·입력 수신부를 소유하는 구조를 먼저 시험한다. 별도 컨트롤러 프로세스가 필요해지면 사용자 권한으로 제한한 Named Pipe를 후보로 삼고 중복 실행을 막는다. localhost WebSocket은 Native Messaging이 불가능할 때만 재검토하며 인증·Origin 검증 없이 대체하지 않는다.

## 6. 책임 분리와 데이터 흐름

```text
교사 물리 마우스·키보드 ───────────────→ 기존 Windows 입력 경로 → 교사 작업

학생 전자칠판 입력
  → Windows 입력 수신·판별·격리(기술 검증 후 선택)
  → 대상 확인 / 좌표 변환 / 포인터 상태 관리
  → Native Messaging 호스트
  → MV3 Service Worker의 세션·메시지 검증
  → 대상 탭 CDP 입력 엔진
  → 지정된 웨일 웹페이지

Content Script: Shadow DOM 가상 키보드 / 학생 입력 대상 / 한글 조합
Sidebar: 준비 상태 / 대상 선택 / ON·OFF / 오류 안내
```

- Windows: 디스플레이·장치·HWND 식별, 교정, 비활성 입력 수신, F9, 연결 상태에 따른 격리 해제.
- Worker: Native 연결, debugger 세션, tabId·windowId 검증, 허용 명령 전달, 상태 변경 이벤트.
- Content Script: 필요한 페이지 UI와 편집 대상만 다룸. 전체 DOM·방문 기록을 수집하지 않음.
- Sidebar: 한 열 반응형 UI, 첫 화면 ON/OFF·상태, 390px 기준 확인. 실제 입력 라우팅은 사이드바 열림 여부에 의존하지 않음.
- 사용하지 않는 상태에서는 터치 감시·커서 샘플링·화면 반복 작업을 종료. 숨겨진 사이드바의 애니메이션·반복 UI 갱신도 종료.

## 7. 대상 선택과 좌표 변환

1. 교사가 웨일에서 학생 대상 창·탭을 명시적으로 선택한다. 이후 전역 활성 탭을 따라가지 않는다.
2. Native HWND와 확장앱 windowId·tabId를 직접 동일시하지 않는다. 선택된 브라우저 창의 위치 정보, 콘텐츠 영역 탐색, 사용자 확인용 교정점을 묶어 연결을 검증한다. 정확한 매핑 수단은 P1 산출물이다.
3. screen 물리 픽셀 → 선택된 콘텐츠 영역 → viewport CSS 픽셀로 변환한다. DPI, 브라우저 확대, 툴바·사이드바, 음수 좌표, 화면 회전·복제를 반영한다. `devicePixelRatio`로 한 번 나누는 식만으로 완료하지 않는다.
4. 네 모서리와 중심 교정점을 시험한다. 좌표가 영역 밖이면 웹페이지 명령을 보내지 않는다.
5. 창 이동·크기·확대·탭·문서 변경 시 geometry/document 세대를 바꾸고 기존 이벤트를 폐기한다. 영역·대상 식별이 불확실하면 PAUSED 상태로 전환한다.
6. 입력 중 바깥으로 이동한 포인터는 해당 시퀀스만 추적해 release/cancel을 보장한다. 교사의 별도 마우스를 포인터 시퀀스에 끼워 넣지 않는다.
7. 대상 창 최소화·다른 창에 가림·화면 분리 시 즉시 중단한다. 보이지 않는 페이지를 좌표만으로 조작하지 않는다.

## 8. 가상 키보드와 한글 조합

- 학생 입력으로 선택한 편집 대상을 따로 기억한다. 가상 키보드 버튼을 누를 때 페이지의 입력 대상이 키보드 버튼으로 바뀌지 않도록 한다.
- 키보드는 페이지 안 Shadow DOM에 표시한다. 학생 가상 Shift·한/영 모드는 교사의 물리 키 상태·Windows IME와 공유하지 않는다.
- 조합 엔진은 DOM에서 분리한 순수 JavaScript 모듈로 만든다. 초성 19·중성 21·종성 없음 포함 28 상태, 이중모음, 겹받침, 받침 이동, 단계별 Backspace를 시험한다.
- 입력 반영은 CDP 경로를 먼저 평가한다. 일반 input·textarea의 DOM 어댑터는 사이트 호환 비교 후보다. 두 경로를 동시에 보내 중복 입력하지 않는다.
- `beforeinput`·`input`·조합 이벤트, 선택 범위, 프레임워크 제어 입력, maxlength·읽기 전용 상태를 시험한다. 임의의 `change` 이벤트를 모든 키마다 발생시키지 않는다.
- 조합 중 대상 전환·탭 이동·페이지 이동·OFF 시 확정/취소 규칙을 명시한다. 마지막 정상 확정 문자열을 보존하고 미확정 상태를 다른 입력란에 옮기지 않는다.
- 웹 키보드 입력을 OS 전역으로 보내지 않는다. Clipboard 전역 변경·OSK·SendInput·Windows IME 전환을 사용하지 않는다.

## 9. 세션, 장애 처리, 성능

상태는 `OFF → PREPARING → READY → ACTIVE → PAUSED/STOPPING → OFF`로 관리한다. ACTIVE는 장치·대상·geometry·통신·입력 엔진이 모두 준비된 뒤에만 가능하다.

- 영구 저장: 사용자 설정과 교정값만. 저장된 ON 상태로 재시작하지 않는다.
- 세션 식별자: 실행별 nonce, 문서 세대, 좌표 세대, 단조 증가 seq. 재연결 때 새 세션을 만들고 교사가 다시 ON해야 한다.
- 입력 메시지: 세션, seq, 대상 ID, 포인터 ID, phase, 좌표, 시각. 웹사이트에서 임의 CDP 메서드·HWND·셸 명령을 지정할 수 없게 한다.
- move는 최신 값으로 합칠 수 있지만 down/up/cancel과 글자 명령은 순서를 유지한다. 큐 상한 초과·ACK 지연 시 학생 세션을 중단하며 교사 경로를 막지 않는다.
- 정상 해제: 신규 입력 차단 → 학생 포인터·조합 정리 → 격리 제거 → debugger 분리 → OFF. 페이지가 없어도 OS 격리 제거는 수행한다.
- 활성 세션에서만 저빈도 연결 생존 확인을 사용한다. 초기 후보는 1초 간격, 2초 무응답 시 해제이며 실험 후 조정한다. 일반 페이지·OFF 상태의 polling은 하지 않는다.
- Native 프로세스 종료 시 훅·수신 창이 남지 않는지 확인한다. 프로세스 멈춤까지 대응해야 하면 별도 경량 감시부를 P2에서 검토한다. 강제 종료로 되돌릴 수 없는 글로벌 설정을 변경하지 않는다.
- F9는 Native 전역 단축키로 시험한다. 등록 충돌 시 ACTIVE 진입을 막고 오류·다음 행동을 표시한다. 학생 입력 경로에서는 일반 키를 가로채지 않는다.
- 훅 콜백 후보에서는 판별·최소 큐 작업만 하고 파일·JSON·UI·네트워크·동기 대기를 하지 않는다. 저수준 훅은 오래 걸리면 해제될 수 있다. [Microsoft 훅 처리 지침](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelmouseproc)
- Worker는 상시 생존을 전제로 하지 않는다. Native/debugger 연결 종료·Worker 재시작을 검출하고 유효한 세션을 재확인한다. Chrome의 수명주기 동작은 참고이며 Whale에서 직접 재시험한다. [Worker 수명주기](https://developer.chrome.com/docs/extensions/develop/concepts/service-workers/lifecycle)

## 10. 구현 순서와 진입·종료 기준

기간은 실기기 준비 후 작업량 추정이며 완료 약속이 아니다. 검증 실패 시 다음 단계로 진행하지 않는다.

| 단계 | 작업 / 산출물 | 통과 조건 | 추정 작업일 |
|---|---|---|---|
| P0 준비·기준 기록 | PREPARATION 체크, 장치·화면·Whale 버전, baseline 기록 | 실기기·비상 종료·시험 입력 확보 | 1 |
| P1a 웨일 전달 실험 | 시험 페이지 + 소형 CDP 입력 프로브 + HWND/탭 매핑 기록 | 다른 앱/다른 Whale 창에서 타이핑 중 대상 페이지 클릭·스크롤·입력 성공, 포커스 탈취 0 | 1~2 |
| P1b Windows 격리 실험 | 입력 관찰 도구 + A/B 후보 비교 | 실제 USB 터치에서 R1~R4 통과, 물리 마우스 간섭 0 | 2~4 |
| P2 연결·긴급 해제 | Native 호스트 + Worker 상태 머신 + ON/OFF/F9 | 통합 클릭·스크롤 및 모든 장애 해제 시험 통과 | 2~3 |
| P3 마우스 기능 | 포인터 상태·좌표 교정·드래그·휠 | DPI/창 이동/탭 변경/영역 이탈에서 오조작·stuck button 0 | 2~3 |
| P4 키보드 | Shadow DOM 키보드 + 한글 모듈 + 편집 어댑터 | 기본 입력란 한글·영문·숫자/삭제/선택 편집, 교사 IME 간섭 0 | 3~5 |
| P5 실제 수업·배포 | 사이트 호환표, 60초/10분/30분 시험, 설치·제거 안내 | TEST 필수 항목 통과, Whale 실기기 결과 기록 | 2~4 |

P1a와 P1b는 별도 프로브로 수행해 실패 원인을 구분한다. 둘 다 통과해야 입력 엔진·격리 방식을 확정한다. 한쪽이 실패하면 기술 결정 기록을 작성하고 해당 후보를 바꿔 재검증한다. 드라이버나 다른 브라우저/별도 기기로 전환해야 한다면 요구 범위 변경을 별도로 논의한다.

### 첫 구현에서 만들 산출물

1. 순수 웹 시험 페이지: 버튼, 링크, 스크롤, 드래그 표면, input, textarea, 이벤트 카운터.
2. Whale MV3 CDP 프로브: 교사 포커스를 옮기지 않고 대상 탭에 명령 전송.
3. Native 관찰 프로브: 장치·포인터·합성 마우스·foreground/cursor 기록. 관찰 모드는 입력을 차단하지 않음.
4. Native 격리 프로브: A/B를 한 번에 하나씩 명시적으로 실행. F9·종료 경로부터 구현.
5. 실제 터치 시험 결과와 채택/실패 결정. 가상 키보드 UI 장식은 이 판정 이후 진행.

## 11. 예정 코드 구조와 배포

현재 파일은 유지한다. P1에서는 기존 `02_제작_결과물/` 안에 `native/`, `tests/`를 추가하는 정도로 시작한다. 아래 구조는 P2에서 빌드·배포 분리를 할 때의 목표이며 아직 생성된 파일이 아니다.

```text
02_제작_결과물/
  extension/                 # 현재 Manifest/content/background/sidebar를 검증 후 이동
    manifest.json
    background/             # native-bridge, session, input-dispatch
    content/                # virtual-keyboard, editor-adapter
    shared/                 # protocol, hangul-composer
    sidebar/
    icons/
  native/
    WhaleDualInput.Core/    # 장치·좌표·입력 수신, 최소 Win32 interop
    WhaleDualInput.Host/    # Native 프로토콜, 상태·긴급 해제
    WhaleDualInput.UI/      # 간단한 WinForms 제어창 후보
  tests/                    # 브라우저 시험 페이지·순수 모듈·Native 검증
  release/                  # extension 패키지 / Windows 호스트 별도 출력
```

프런트엔드는 Vanilla JavaScript/HTML/CSS를 유지한다. Native는 C#·Win32 interop·간단한 WinForms를 우선 검토하며 대형 UI 프레임워크는 추가하지 않는다. 생산 배포의 런타임 후보는 .NET 10 LTS, 자체 포함 win-x64 배포다. 지원 Windows와 학교 보안 정책·파일 크기·메모리를 확인한다. 현재 설치된 .NET 8 SDK는 관찰용 초기 실험 후보이나 생산 버전의 기본으로 고정하지 않는다. [Microsoft .NET 지원 정책](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

Native Messaging은 호스트 등록을 필요로 하므로 “exe 하나를 실행하면 모든 준비 완료”라고 약속하지 않는다. 실제 등록·제거·권한·확장앱 ID 변경 절차를 P2/P5에서 구현한다. 기존 package-project 스크립트는 폴더 전체 ZIP 도구이며 exe 빌드나 호스트 등록을 수행하지 않는다. 배포 분리 시 확장앱 ZIP과 Windows 호스트 패키지를 각각 출력하도록 조정한다.

## 12. 관련 문서와 완료 정의

- [PREPARATION](docs/PREPARATION.md): 현재 개발환경·실기기 준비·첫 실험 순서.
- [TEST](docs/TEST.md): 측정 방법·검증 단계·합격 기준·미검증 상태.
- [PRIVACY](docs/PRIVACY.md): 현재·후보 권한, 데이터·삭제·Native 연결 보안.
- [IDEA](docs/IDEA.md): 교육 문제·확장앱 가치·MVP 판단.
- [UX](docs/UX.md): 준비·실행·독립 입력·오류·해제 흐름.

R1~R6, Whale 실기기 동작, 필요 최소 권한, 기존 페이지 비간섭, 성능, 장애 해제, 문서 일치를 모두 확인해야 기능 완료다. 논리적으로 그럴듯한 설명이나 우클릭 시뮬레이션만으로 독립 입력 성공을 선언하지 않는다.
