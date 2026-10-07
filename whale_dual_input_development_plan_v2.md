# Whale Dual Input 개발기획안
## 지정된 Student Whale 브라우저 창의 독립 입력 컨텍스트 구현 계획

- 문서 버전: v2.0
- 작성일: 2026-10-07
- 기준 환경: Windows 11 + NAVER Whale + 터치 디스플레이
- 기준 문서: `whale_dual_input_technical_summary_v2.md`
- 프로젝트 성격: Whale 기반 교육용 듀얼 입력 시스템
- 핵심 개념: **Student Whale Isolated Input Context**
- 최우선 원칙: **Teacher Physical Input과 Student Whale Input은 동시에, 서로 침범하지 않고 독립적으로 작동한다.**

---

# 1. 프로젝트 한 줄 정의

**Windows 전체나 Monitor 2 자체를 독립된 사용자 공간으로 만드는 것이 아니라, 지정된 Student Whale 브라우저 창에 학생 전용 터치·가상 키보드 입력 경로를 부여하고, 교사의 물리 마우스·키보드는 기존 Windows 입력 체계에서 그대로 계속 작동하게 한다.**

---

# 2. 가장 중요한 개념 정정

이 프로젝트의 독립성 대상은 다음이 아니다.

```text
Monitor 2 전체
```

독립성의 실제 대상은 다음이다.

```text
Student Whale Window
```

Monitor 2는 Student Whale이 위치하는 대표적인 물리 디스플레이일 뿐이다.

따라서 시스템을 다음처럼 이해한다.

```text
Windows PC
│
├─ Teacher Physical Input
│   ├─ Physical Mouse
│   └─ Physical Keyboard
│
│   → 기존 Windows 방식 그대로 사용
│   → 어떤 정상 프로그램도 조작 가능
│
└─ Student Whale Input Context
    ├─ Physical Touch
    └─ Virtual Keyboard

    → 지정된 Student Whale Window에서만 작동
```

---

# 3. 최종 사용자 경험

## 3.1 교사

교사는 기존 Windows를 평소와 완전히 동일하게 사용한다.

가능한 작업:

- 한글 작성
- PowerPoint 조작
- Excel
- 일반 웹브라우징
- 다른 Whale 창
- 파일 탐색기
- 그림/영상 편집
- 마우스 drag
- 키보드 shortcut
- system cursor를 여러 모니터 사이에서 자유롭게 이동

본 프로그램은 교사의 물리 입력에 불필요하게 개입하지 않는다.

## 3.2 학생

학생은 특정 Student Whale Window에서 다음을 사용한다.

- 터치
- 스크롤
- 링크 선택
- 버튼 선택
- Drag
- 가능하면 Pinch
- Student Search
- Virtual Keyboard
- 한글 입력
- 숫자/영문 입력

학생 입력은 Student Whale만 대상으로 한다.

---

# 4. 성공 상태

성공 상태는 다음과 같다.

```text
시간 ──────────────────────────────────────────────→

Teacher Mouse      ───────────────────────────────>
Teacher Keyboard   ───────────────────────────────>

Student Touch            ─────────────────────────>
Student Virtual Keyboard ─────────────────────────>
```

네 입력 stream은 동시에 존재할 수 있다.

각 입력이 도달하는 곳:

| 입력 | 대상 |
|---|---|
| 교사 물리 마우스 | Windows system cursor 및 교사가 현재 조작하는 프로그램 |
| 교사 물리 키보드 | 현재 Windows foreground / keyboard focus |
| 학생 실제 터치 | 지정된 Student Whale Window |
| 학생 가상 키보드 | Student Whale 내부의 Student Logical Focus |

---

# 5. 절대 실패 조건

다음은 모두 실패로 본다.

- 학생이 Student Whale을 터치했더니 system cursor가 터치 위치로 이동
- cursor가 이동했다가 다시 돌아옴
- Student Whale이 잠깐 Windows foreground가 됨
- 이후 교사 창으로 foreground를 복원함
- 교사 keyboard focus가 잠깐이라도 Student Whale로 넘어감
- 교사 입력 문자가 누락됨
- 학생 가상 키보드 문자가 교사 프로그램에 들어감
- 교사 drag가 끊김
- teacher mouse capture가 해제됨
- 교사가 마우스를 사용하는 동안 학생 touch 처리가 멈춤
- 학생이 touch를 사용하는 동안 교사 mouse/keyboard가 멈춤

즉 **납치 후 복원은 성공이 아니다.**

---

# 6. 핵심 독립성 원칙

## 6.1 Teacher Mouse Independence

교사의 물리 마우스는 기존 Windows 방식 그대로 동작한다.

학생이 Student Whale을 터치하더라도 다음은 유지된다.

- system cursor는 교사 물리 마우스에 의해서만 이동
- click 정상
- drag 정상
- text selection 정상
- resize 정상
- wheel scroll 정상
- mouse capture 유지

Student touch는 teacher mouse path에 들어가지 않는다.

---

## 6.2 Teacher Keyboard Independence

교사 keyboard는 기존 Windows foreground / focus 모델을 그대로 사용한다.

Student Whale에서 어떤 일이 일어나도:

- foreground window 불변
- keyboard focus 불변
- key loss 0
- 학생 virtual key가 teacher app에 전달되지 않음

---

## 6.3 Student Touch Independence

학생 touch는 특정 Student Whale Window에만 귀속한다.

Primary Path의 목표:

```text
Student Finger
     ↓
Windows PT_TOUCH
     ↓
Student Whale Window
     ↓
Chromium Native Touch Pipeline
     ↓
Whale Web Content
```

가능하면 touch를 가상 mouse click으로 재구성하지 않는다.

---

## 6.4 Student Virtual Keyboard Independence

학생 keyboard는 Windows keyboard가 아니다.

```text
Student Virtual Keyboard
        ↓
Virtual Keyboard Engine
        ↓
Hangul / English Composer
        ↓
Student Logical Focus
        ↓
Student Whale Web Input
```

사용하지 않는 것:

- SendInput
- Windows OSK
- foreground 변경
- 교사 keyboard stream 재사용
- system-wide key injection

---

# 7. Primary Architecture — Student Whale Isolated Input Context

```text
                         Windows Session
                               │
        ┌──────────────────────┴───────────────────────┐
        │                                              │
 Teacher Physical Input                     Student Whale Input
        │                                              │
 Physical Mouse                               Physical Touch
 Physical Keyboard                                  │
        │                                              ▼
        │                                      Student Whale HWND
        │                                     + NOACTIVATE Policy
        │                                              │
        ▼                                              ▼
 Normal Windows Input                        Chromium Native Touch
        │                                              │
 Current Foreground App                        Web Interaction
                                                       │
                                               Virtual Keyboard
                                                       │
                                                Virtual Input
                                                       │
                                                Web Input Target
```

핵심은 다음 두 요소이다.

1. Teacher 입력 계통은 수정하지 않는다.
2. Student Whale에만 특수 입력 컨텍스트를 만든다.

---

# 8. Student Whale 식별 방식

시스템의 핵심 식별자는 monitor 번호가 아니라 다음이다.

```text
StudentWhaleHwnd
```

Agent는 특정 Whale top-level window를 Student Whale로 지정한다.

예:

```text
Teacher current app
HWND = 0x001234

Student Whale
HWND = 0x00ABCD
```

특수 정책은 `0x00ABCD`에만 적용한다.

---

# 9. Monitor 2의 역할

Monitor 2는 Student Whale이 주로 배치되는 위치이다.

전자칠판 환경에서는 UX를 단순하게 하기 위해:

```text
Monitor 2
┌──────────────────────────┐
│                          │
│    Student Whale         │
│    Fullscreen            │
│                          │
└──────────────────────────┘
```

형태가 가장 안정적이다.

그러나 기술적으로 독립된 것은 Monitor 2가 아니라 Student Whale Window다.

따라서 향후에는 Student Whale을 다른 touch monitor로 옮기는 것도 가능하도록 설계할 수 있다.

---

# 10. 교사 마우스에 대한 정확한 정책

교사 mouse는 Monitor 1에 고정되지 않는다.

교사는 system cursor를:

```text
Monitor 1 → Monitor 2 → Monitor 1
```

처럼 자유롭게 이동할 수 있다.

중요한 것은 **Student Touch가 system cursor를 움직이지 않는 것**이다.

따라서 다음 구분이 필요하다.

```text
Teacher Physical Mouse
→ Windows System Cursor
→ 일반 Windows 동작

Student Physical Touch
→ Student Whale
→ system cursor와 무관
```

---

# 11. 교사 키보드에 대한 정확한 정책

교사 keyboard도 Monitor 1 전용이 아니다.

대상은:

```text
Current Windows Foreground / Keyboard Focus
```

이다.

예:

```text
Teacher foreground = HWP
Physical keyboard → HWP
```

또는:

```text
Teacher foreground = PowerPoint
Physical keyboard → PowerPoint
```

Student Whale은 교사의 physical keyboard target이 되지 않는다.

---

# 12. Path A — 최우선 구현 전략

## 12.1 개념

지정된 Student Whale top-level HWND에 `WS_EX_NOACTIVATE` 계열 정책을 적용한다.

목표:

```text
Student touch
    ↓
Whale receives native touch
    ↓
Whale DOES NOT become Windows foreground
```

## 12.2 기대 효과

성공하면 Chromium이 원래 구현한 다음 기능을 활용할 수 있다.

- tap
- scroll
- drag
- link
- button
- pinch
- pointer event
- touch event

별도의 Virtual Pointer가 거의 필요하지 않는다.

## 12.3 가장 중요한 검증

아래 행동마다 별도로 확인한다.

- blank area tap
- link
- button
- scroll
- drag
- input
- textarea
- contenteditable
- pinch

각 시험 동안 Teacher foreground는 한 번도 변경되면 안 된다.

---

# 13. 기술 장벽

## R1. Whale/Chromium 자체 Activation

Student Whale HWND에 NOACTIVATE 정책을 줘도 Chromium 내부에서 window activation을 다시 요청할 수 있다.

### 해결 순서

1. top-level HWND 정확히 식별
2. `WS_EX_NOACTIVATE` 적용
3. `WM_MOUSEACTIVATE`
4. `WM_POINTERACTIVATE`
5. style 적용 시점 확인
6. child HWND 구조 분석

DLL injection은 기본 해법으로 사용하지 않는다.

---

## R2. Touch → Mouse Promotion

학생 touch가 mouse message로 승격되면 system cursor를 이동시킬 수 있다.

이 경우 실패다.

검증:

- GetCursorPos
- low-level mouse hook
- actual touch pointer log

목표:

```text
student touch caused cursor movement = 0
```

Cursor Restore는 사용하지 않는다.

---

## R3. Teacher Mouse Capture 손실

Teacher가 drag 중일 때 student touch가 들어와도:

- drag 유지
- mouse capture 유지
- mouse up 오인식 없음

이어야 한다.

---

## R4. 비활성 Whale의 Native Touch 동작

Student Whale은 Windows foreground가 아니다.

이 상태에서 다음 기능이 정상인지 확인한다.

- scroll
- link
- button
- drag
- selection
- media control
- pinch

---

## R5. Student Web Input Focus

학생이 input을 선택했을 때 Windows keyboard focus가 Student Whale로 넘어가면 안 된다.

### Mode A

DOM input focus를 가져도 window activation이 발생하지 않는 경우:

```text
DOM activeElement = Student Input
Windows foreground = Teacher App
```

이 구조를 활용한다.

### Mode B

실제 DOM focus가 activation을 발생시키면:

```text
Student Touch
→ Virtual Focus Manager
→ Target Element 저장
→ Visual Caret / Highlight
→ Virtual Keyboard Input
```

으로 처리한다.

즉:

```text
Student Logical Focus
≠
Windows Keyboard Focus
```

로 만든다.

---

# 14. Student Virtual Keyboard

Virtual Keyboard는 Student Whale 전용이다.

## 지원 단계

### 1단계
- 숫자
- 영문
- Space
- Backspace
- Enter

### 2단계
- 한글
- Shift
- 기호

### 3단계
- selection replacement
- cursor movement
- contenteditable
- React/Vue controlled input

---

# 15. Hangul Composer

Windows IME를 학생 입력의 기본 구조로 사용하지 않는다.

자체 모듈:

```text
HangulComposer
```

지원:

- 초성
- 중성
- 종성
- 쌍자음
- 겹모음
- 겹받침
- Backspace 분해
- 한/영
- 숫자/기호
- cursor 이동
- 선택 치환

UI와 분리된 pure module로 개발한다.

---

# 16. Web Input Adapter

가상 키보드 입력을 다양한 웹 input에 전달하는 계층을 만든다.

```text
Student Virtual Text
       ↓
WebInputAdapter
       ├─ Standard input
       ├─ textarea
       ├─ contenteditable
       ├─ React/Vue
       └─ site adapter
```

지원 우선순위:

1. Student Search
2. 표준 HTML input
3. Whale Space 핵심 화면
4. 주요 교육 사이트
5. 범용 확대

---

# 17. Student Search

Student Whale의 실제 omnibox를 학생 키보드로 직접 조작하는 것을 기본 목표로 하지 않는다.

Student Search UI:

```text
┌──────────────────────────────┐
│ 🔎 검색                      │
│ [세종대왕 업적____________] │
│                      [검색] │
└──────────────────────────────┘
```

입력:

```text
Virtual Keyboard
→ Student Search
→ Extension
→ Student Whale Tab navigation
```

---

# 18. Windows Agent

권장:

- C#
- .NET 10 LTS
- CsWin32
- PerMonitorV2 DPI awareness
- User-session Agent

역할:

- Student Whale window 탐지/지정
- touch monitor 탐지
- PT_TOUCH 판별
- Student Whale window policy 적용
- state restore on exit
- foreground monitoring
- cursor monitoring
- mouse capture monitoring
- Native Messaging relay
- tray UI
- kill switch

---

# 19. Hardware Gate

전자칠판 touch가 실제 Windows Touch Pointer로 들어와야 한다.

필수:

```text
PointerType = PT_TOUCH
```

만약:

```text
PointerType = PT_MOUSE
```

라면 touch 자체가 system mouse stream에 속하므로 프로젝트의 완전 분리 목표와 충돌한다.

이 경우:

- driver mode 변경 확인
- touch digitizer mapping 확인
- 지원 device 조건 명시
- 필요 시 비지원 처리

복원 방식으로 해결하지 않는다.

---

# 20. 개발 단계

## Phase 0 — Environment Probe

확인:

- Monitor 목록
- touch monitor
- Whale windows
- DPI/scaling
- pointer device
- `PT_TOUCH`
- Windows build
- Whale version

---

## Phase 1 — Student Whale Path A Spike

### Teacher 측

TeacherSim으로:

- 계속 mouse movement
- 계속 typing
- click
- drag
- foreground log
- capture log

### Student 측

지정된 Student Whale에서:

- tap
- scroll
- link
- button
- drag
- input

### 동시 시험

반드시 동시에 실행한다.

```text
Teacher keeps typing + moving mouse
Student keeps touching + scrolling
```

### PASS

```text
Teacher foreground change        0
Teacher keyboard focus loss      0
Student-caused cursor movement   0
Teacher key loss                 0
Teacher mouse capture loss       0

Student touch                    정상
Student scroll                   정상
Student click                    정상
```

---

## Phase 2 — Concurrent Interaction Stress Test

60초 이상:

```text
Teacher mouse      계속 사용
Teacher keyboard   계속 사용

Student touch      계속 사용
Student scroll     계속 사용
```

중단 0이어야 한다.

---

## Phase 3 — Virtual Keyboard

Teacher physical keyboard와 Student virtual keyboard를 동시에 사용한다.

예:

```text
Teacher → "교사용 입력"
Student → "학생 검색"
```

각 문장이 정확한 대상에만 기록되어야 한다.

---

## Phase 4 — Hangul Composer

한글 가상 입력 구현.

---

## Phase 5 — Whale Extension Bridge

구조:

```text
WhaleDualInput.Agent
      ↕
Named Pipe
      ↕
WhaleDualInput.NmHost
      ↕
Native Messaging
      ↕
Whale Extension MV3
```

---

## Phase 6 — Student Search

검색 UI 구현.

---

## Phase 7 — Whale Space Compatibility

실제 Whale Space 핵심 사용 흐름 검증.

---

## Phase 8 — Educational UX

- Student Mode
- Search
- Virtual Keyboard
- Back/Forward
- Home
- Refresh
- 수업용 기능
- 교사 설정

---

# 21. Fallback Path

## Path B — NOACTIVATE Touch Capture + CDP

Path A가 네이티브 touch와 no-activation을 동시에 달성하지 못할 때 검토한다.

```text
Student Touch
→ NoActivate Capture
→ Agent
→ Extension
→ CDP Input
→ Student Whale Renderer
```

Teacher cursor/focus를 침범하면 이 경로도 실패다.

## Path C — Controlled Student Workspace

범용 웹 호환성이 어렵다면 Student Whale 내부에 통제된 Workspace를 제공한다.

독립되는 대상은 여전히 Student Whale Window다.

---

# 22. 절대 금지 구현

다음은 제품 코드에서 금지한다.

1. Student touch 후 foreground 복원
2. Student touch 후 cursor 복원
3. Student virtual keyboard에 SendInput 사용
4. Windows OSK를 Student Keyboard로 사용
5. Student touch를 system mouse click으로 변환
6. Teacher physical keyboard를 Student 입력에 재사용
7. SetForegroundWindow로 Teacher/Student를 번갈아 활성화
8. Whale DLL injection
9. Chromium API hooking
10. Monitor 2 전체 입력을 무조건 가로채는 방식
11. “사용자가 눈치채지 못할 만큼 빠른 복원”을 성공으로 판단

---

# 23. 실패 지점별 의사결정

| 실패 | 대응 |
|---|---|
| Student Whale touch 시 foreground 변경 | Path A 보완 → B 검토 |
| Student touch로 cursor 이동 | promotion 경로 조사 / hardware 제한 |
| Teacher drag 해제 | pointer/capture 구조 재검토 |
| Student input touch에서만 activation | Virtual Focus Mode |
| Student virtual key가 teacher app에 들어감 | system key injection 제거 |
| PT_MOUSE device | 지원 device 제외 또는 driver 설정 |
| 웹 호환성 부족 | Supported Sites / Workspace |
| Native Messaging 불안정 | IPC 대안 검토 |

---

# 24. 프로젝트 폴더 구조

```text
whale-dual-input/
│
├─ AGENTS.md
├─ README.md
│
├─ docs/
│  ├─ PRODUCT.md
│  ├─ ARCHITECTURE.md
│  ├─ DECISIONS.md
│  ├─ POC.md
│  ├─ TEST.md
│  └─ COMPATIBILITY.md
│
├─ native/
│  ├─ WhaleDualInput.Core/
│  ├─ WhaleDualInput.Agent/
│  ├─ WhaleDualInput.Probe/
│  └─ WhaleDualInput.NmHost/
│
├─ extension/
│  ├─ manifest.json
│  ├─ service-worker.js
│  ├─ content/
│  └─ student/
│
└─ tests/
   ├─ TeacherSim/
   ├─ ConcurrentInputHarness/
   ├─ FocusMonitor/
   └─ Core.Tests/
```

---

# 25. AGENTS.md 최상위 규칙

```markdown
## Primary Goal

지정된 Student Whale Window의 Touch / Virtual Keyboard Input과
Teacher Physical Mouse / Keyboard Input을 동일 Windows 세션에서
동시에, 서로 침범하지 않고 독립적으로 작동하게 한다.

## Non-negotiable Invariants

1. 독립 대상은 Monitor 2 전체가 아니라 Student Whale Window다.
2. Student input은 Teacher foreground를 변경하지 않는다.
3. Student touch는 system cursor를 이동시키지 않는다.
4. Student input은 Teacher mouse capture를 해제하지 않는다.
5. Student input은 Teacher keyboard focus를 변경하지 않는다.
6. Teacher mouse/keyboard는 기존 Windows 방식 그대로 계속 작동한다.
7. Teacher input 중에도 Student Whale input은 계속 작동한다.
8. Student input 중에도 Teacher input은 계속 작동한다.
9. Focus Restore / Cursor Restore는 성공으로 인정하지 않는다.
10. Student Virtual Keyboard에 SendInput을 사용하지 않는다.
11. Student Whale 외 Windows 입력 환경에는 최소한으로만 개입한다.
12. 모든 주요 기능은 concurrent input test를 통과해야 한다.
```

---

# 26. 첫 번째 개발 작업

첫 개발은 다음 하나다.

```text
WhaleDualInput.StudentWhaleIsolationSpike
```

구현:

1. Whale window 목록 표시
2. Student Whale 지정
3. Student Whale HWND 저장
4. touch display 식별
5. `PT_TOUCH` 확인
6. Student Whale에 Path A 적용
7. foreground monitor
8. cursor monitor
9. mouse capture monitor
10. TeacherSim
11. Student test page
12. kill switch

시험:

```text
Teacher:
Windows에서 평소처럼 mouse + keyboard 계속 사용

Student:
지정된 Student Whale에서 touch + scroll 계속 사용
```

Student Whale을 전자칠판 전체화면으로 두어도 좋지만, 기술적으로는 그 Whale HWND만 특수 처리한다.

---

# 27. 공모전 설명

> **Whale Dual Input은 보조 모니터 전체를 별도의 PC처럼 만드는 프로그램이 아니다. 특정 Whale 브라우저 창을 학생 전용 입력 컨텍스트로 전환한다. 교사의 물리 마우스와 키보드는 기존 Windows에서 그대로 사용할 수 있으며, 학생은 전자칠판에 띄운 Student Whale에서 터치·스크롤·검색·가상 키보드 입력을 동시에 수행한다. Student Whale의 조작은 교사의 system cursor, keyboard focus, foreground window, mouse capture를 침범하지 않는다. 이를 통해 한 대의 교사용 PC에서도 교사와 학생이 동시에 상호작용할 수 있는 Whale 기반 수업 환경을 구현한다.**

---

# 28. 최종 기술 정의

본 프로젝트는:

```text
Monitor 1 vs Monitor 2
```

의 분리 프로젝트가 아니다.

정확한 구조는:

```text
Teacher Windows Input
          vs
Student Whale Input Context
```

이다.

Teacher는 기존 Windows를 그대로 사용한다.

Student에게만 다음 독립 입력 경로를 제공한다.

```text
Student Touch
→ Student Whale Native Touch

Student Virtual Keyboard
→ Student Whale Logical Input
```

성공이란 네 입력 스트림이 동시에 존재하면서 각자 지정된 대상에만 도달하는 것이다.

```text
Teacher Mouse      ─────────────────────────────>
Teacher Keyboard   ─────────────────────────────>
Student Touch      ─────────────────────────────>
Student Virtual KB ─────────────────────────────>
```

**입력이 충돌한 뒤 복원되는 방식은 성공이 아니다.  
Student Whale 자체가 독립적인 학생 입력 컨텍스트로 작동해야 한다.**
