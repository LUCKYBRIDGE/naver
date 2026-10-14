# 원본 포인터 격리 연구 엔진 — 구현 기록

2026-10-14. Tier A 후보의 입력 소비 엔진을 구현했다. **아직 실기기 G1 통과·학교 배포 후보가 아니다.** UIAccess 목적 적합성/서명/설치 게이트는 [정책 판단](UIACCESS_POLICY_DECISION.md)을 따른다. 타입 전체 리디렉션이므로 장치별 HID 필터와 다르다.

## 실제 코드 경로

- `PointerIsolationExperiment`: 전용 STA 메시지 스레드와 message-only 수신 창을 만든다. 화면 위 투명 오버레이·수동 영역 선택을 사용하지 않는다.
- `PointerIsolationNative`: TokenUIAccess를 읽는다. 관리자 실행 여부 대신 실제 실행 토큰을 확인하며, 자격이 없으면 등록하지 않는다.
- `PointerRegistration`: PT_TOUCH(2)/PT_PEN(3)만 등록한다. PT_MOUSE/PT_POINTER를 금지한다. 두 타입 중 후속 등록 실패 시 이미 등록한 타입을 역순 해제한다. 실패를 충돌로 단정하지 않고 Win32 error를 반환한다.
- 수신 창은 WM_POINTERDOWN/UPDATE/UP/CAPTURECHANGED를 같은 스레드에서 즉시 읽는다. 시작 시 확인한 sourceDevice 및 포인터 타입·학생 화면 RECT가 맞는지 확인한다. 다른 장치/잘못된 좌표·수신 오류·큐 혼잡이면 시험을 중단한다.
- 입력을 소비하고 256개 상한의 메모리 채널에 기록한다. 학생 웹 전달·전역 SendInput·터치 재주입·교사 커서/포커스 사후 복원은 없다. 큐는 UI에서 익명 로그로 배출한다. DOWN/UP을 손실 후 계속 실행하는 대신 전체 시험을 종료한다.
- 주 화면의 OFF, 물리 F9, 최대 30초, UI 응답 중단(2초), 실제 장치/화면/세션 변경에 종료·해제한다. 등록/해제는 창 소유 스레드에서 수행한다. 해제 API 실패를 정상 OFF로 숨기지 않는다.
- [보호 상태 관리자](PROTECTION_STATE_CONTRACT.md)에 등록/해제 결과를 연결했다. Windows 등록 성공은 EXPERIMENTAL·HardwareVerified=false·AdapterReady=false여서 제품 ACTIVE로 발급되지 않는다. backend 연구 표시와 제품 보호 판정을 분리한다.

## 빌드와 자격

```powershell
npm run build:input-probe
npm run build:isolation-research
```

일반 Probe는 원본 격리 코드가 있어도 실행 토큰에 UIAccess가 없으면 버튼을 비활성화한다. `--isolation-capabilities`는 실제 토큰/매핑 자격만 조회하며 **등록 API를 호출하지 않는다**.

별도 Isolation.A 프로젝트는 uiAccess=true의 앱 manifest를 포함한 연구용 빌드다. 출력은 `dist/whale-dual-input/isolation-research/WhaleDualInput.Isolation.A.exe`다. 빌드 스크립트는 서명·설치·실행·레지스트리 변경을 하지 않는다. **현재 바이너리는 서명되지 않아 그대로 실행 가능하다고 안내하면 안 된다.**

연구 실행 전에 목적/정책 판단, 신뢰 체인의 Authenticode 서명, 보호된 위치 설치, UIAccess 실행 토큰 및 시험 기기의 명확한 장치 구성을 갖춰야 한다. 인증서/비밀키를 소스에 넣지 않는다. 보안 정책을 꺼서 자격 검사를 우회하지 않는다. 서명·설치가 해결되기 전 기능 성공/배포 준비로 표시하지 않는다.

일반 Probe에서 `--self-test`로 등록 트랜잭션·정책·부분 실패·해제 실패를 fake API에 대해 검사할 수 있다. 이는 실제 포인터 차단 검사가 아니다. Isolation.A의 UIAccess 실행 조건을 합성 토큰이나 fake API로 실제 GUI에서 우회하는 옵션은 없다.

## UI 시험 흐름

실행 자격과 매핑을 모두 확인한 연구 구성에서만 `원본 터치 격리 (연구 30초)`를 누를 수 있다. 기본 확인은 취소다. 학생 팝업을 띄우지 않고 타입 리디렉션으로 전역 터치를 받아 **소비만** 한다. 교사 마우스·키보드는 재주입하지 않는다. 학생은 Whale를 조작할 수 없는 것이 이 단계의 의도된 동작이다.

현재 기기는 보조 source의 복제 출력, 주 화면 터치·펜 공존이 관측되어 시작 조건을 충족하지 않는다. 이 구현에서 다른 장치도 차단되는 것을 교사 모니터 2 제한 허용과 동일시하지 않는다. 화면/장치 설정을 임의로 변경하지 않는다. 마우스 에뮬레이션 장치는 이 API로 지원하지 않으며 별도 장치 필터 연구가 필요하다.

## 남은 구현 및 검증

- 이 엔진에는 독립 프로세스 watchdog/인증 IPC/등록 소유권 외부 조회가 없다. 전용 스레드가 멈추면 timer도 멈출 수 있다. UI 하트비트는 완전한 fail-closed를 보장하지 않는다.
- 종료/창 파괴로 리디렉션이 풀리는 보호 공백이 있다. UAC·잠금·다른 데스크톱에서 연속 보호를 보장하지 않는다.
- pointer message를 개별 처리하며 프레임/history 복원 및 좌표 epoch·복합 장치 대조는 후속이다. 연구 도구의 고정 큐/단순 종료 정책을 제품 저지연 구현으로 확정하지 않는다.
- 실제 칠판의 호환 마우스 누출·시스템 커서/전경/내부 포커스·동시 타이핑·F9·해제 및 보호 공백은 미실행이다. 모니터 1 ADR의 M1P 검사를 사용한다.
- 원본 소비 시험의 G1 통과 이후 학생 SeatEvent·NativeBridge·Whale 어댑터에 전달한다. 현재 기존 v0.2.0 Host/Worker에 연결하지 않았다.

## 공식 근거

- [RegisterPointerInputTarget](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerpointerinputtarget): 타입 전체 리디렉션·UIAccess·스레드 소유·창 파괴 시 해제.
- [UnregisterPointerInputTarget](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unregisterpointerinputtarget): 타입별 해제 조건.
- [UIAccess 보안 조건](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-securityoverview): 목적·서명·설치 위치.

원본 설계·인계 파일은 보존했다. Manifest 권한 추가 없음. G1/G2 NOT TESTED.
