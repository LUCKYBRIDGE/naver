# 개발 준비 현황 — Whale Dual Input v1.1

2026-10-08. 사용자 제공 새 개발계획을 현행 저장소에 연결했다. **준비 작업 완료, 기능 개편 착수 전. G1/G2 NOT TESTED.** 아래 완료는 문서 검토·코드 점검·개발 도구 준비를 뜻한다.

## 바로 이어서 읽기

1. [프로젝트 개발 지침](../../AGENTS.md)과 [구현 범위/결정](IMPLEMENTATION_SCOPE.md)
2. [기술 아키텍처 v7.3](ARCHITECTURE_V7_3_MONITOR2.md) → [실행계획 v1.0](DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md)
3. [파일별 재사용 점검](CODE_REUSE_AUDIT.md)
4. [UI 계약](UI_ON_OFF_SPEC_V1_1.md), [design](design.md), [구현 과제](IMPLEMENTATION_TASKS_V1_1.md), [판정 시험](ACCEPTANCE_TESTS_V1_1.md)
5. [UIAccess 판단](UIACCESS_POLICY_DECISION.md), [설치·개발환경](INSTALLATION_FEASIBILITY.md), [공식 참고 자료](RESEARCH_REFERENCES.md)
6. [HANDOFF의 과거 실패와 이번 준비 기록](HANDOFF.md)

v4·v5·기존 P1b의 훅/오버레이는 과거 검토 이력이다. 모니터 2 장치 원본 격리와 자동 웹 대상/좌표가 최신 방향이다. [제공 인계 원본](../../../Whale_Dual_Input_Development_Handoff_v1_1/README_START_HERE.md)은 수정하지 않았다.

## 준비 결과

| 작업 | 상태 | 결과 |
|---|---|---|
| T-001 / M0 정적 재사용 점검 | 완료 | Manifest·Native·background/content/sidebar·빌드/프로브 비교, 기존 실패 기록 보존 |
| T-002 / 최신 기준 연결 | 완료 | 기술 원문 2개와 UI/디자인/작업/QA 연결본 4개, scoped AGENTS, 진입 문서 안내 |
| T-003 / M0.4 정책 조사 | 사전 조사 완료, 게이트 미통과 | 공식 정책 확인. Tier A 연구 후보/제품 미승인, 서명·학교 조건 미확인 |
| 개발 SDK | 준비 완료 | Node 24·로컬 .NET 10.0.401·Whale 실행 파일 존재 확인 |
| M0.5 / Probe·양성 대조군 | 다음 구현 | 신규 Native 프로브 미구현, 실제 칠판 타입/매핑 미확인 |
| M1 자동 매핑 / M2a 원본 격리 | 미구현·미검증 | 기존 overlay 활성 응답은 증거로 인정하지 않음 |
| M4a / 실제 Whale 최소 호환 | 미실행 | 기존 P1a 재사용 후보. 일반 CDP/headless 결과와 구분 |
| G1 / G2 | NOT TESTED | 이번 세션에서 칠판·교사 물리 동시 입력 시험 없음 |

## 기준선 및 보존

- 시작 브랜치/HEAD: `main` / `a5f0d23dae251d2ccb57494001c3b40b73241b56`.
- 준비 전 untracked: 제공 `projects/Whale_Dual_Input_Development_Handoff_v1_1/`와 `docs/ARCHITECTURE_V5.md`. 삭제·변경하지 않았다.
- 이번 작업은 로컬 문서와 빌드 도구 준비다. 커밋·push·원격 변경은 수행하지 않았다. 코드의 기능 변경은 `codex/` 브랜치에서 시작하고 문서 준비와 구현 변경을 구분한다.
- 2026-10-07 ON 직후 OFF 현상은 [HANDOFF](HANDOFF.md)대로 유지한다. 디버거 배너에 따른 viewport 변화는 아직 원인 후보다.

## 원문 및 연결본

| 제공 파일 | 프로젝트 문서 | 처리 |
|---|---|---|
| `05_references/ARCHITECTURE_V7_3_MONITOR2.md` | `ARCHITECTURE_V7_3_MONITOR2.md` | 바이트 그대로 복사 |
| `05_references/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md` | `DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md` | 바이트 그대로 복사 |
| `02_ui/ON_OFF_UI_SPEC.md` | `UI_ON_OFF_SPEC_V1_1.md` | 출처 안내·상대 참조 위치만 변경 |
| `02_ui/design.md` | `design.md` | 출처 안내·목업 상대 참조만 변경 |
| `03_implementation/IMPLEMENTATION_TASKS.md` | `IMPLEMENTATION_TASKS_V1_1.md` | 출처 안내·기준 문서 상대 참조만 변경 |
| `04_quality/ACCEPTANCE_TESTS.md` | `ACCEPTANCE_TESTS_V1_1.md` | 출처 안내 추가 |

원문 SHA-256 확인값:

```text
ARCHITECTURE_V7_3_MONITOR2.md
cc8ebe746d0b33513100b5852688fc34a8c0b5e9d00ee9143d912cca74f31731
DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md
a7653c8d8096abbbef95f872e9da6b2394e1ad82f626c5012e7bc30680c33658
```

제공 해시 대장과 원본·프로젝트 사본이 모두 일치한다. [목업](../../../Whale_Dual_Input_Development_Handoff_v1_1/06_mockup/index.html)은 상태 시연 전용이다. 성공 상태 자동 전환 코드를 제품 로직으로 복사하지 않는다.

## 다음 개발 작업 묶음

### 1. M0.5: 관찰 전용 Probe (T-010/011)

`02_제작_결과물/native/WhaleDualInput.Probe/`에서 별도 명시적 실행 도구로 구현한다. 시작 단계에 차단·재주입·UIAccess 등록·레지스트리 변경을 넣지 않는다.

- Display: QueryDisplayConfig·GetMonitorInfo로 경로/확장·복제·RECT·회전·DPI를 기록. 배열의 두 번째 Screen이나 DISPLAY2 이름을 Windows 2번 화면으로 단정하지 않는다.
- Identity: GetPointerDevices/GetPointerDeviceRects·Raw Input/HID를 대조하고 PT_TOUCH/PT_PEN/PT_MOUSE·sourceDevice·복수 장치 여부를 분류한다.
- Teacher: GetCursorPos·Raw Input 마우스·EVENT_SYSTEM_FOREGROUND·EVENT_OBJECT_FOCUS·GetGUIThreadInfo의 시간 상관을 계측한다. 진단 훅은 읽기 전용이며 입력을 차단하지 않는다.
- 양성 대조군: OFF 상태에서 실제 학생 터치 간섭을 검출하는지 확인한다. 검출하지 못한 로거의 0건으로 G1 통과를 판단하지 않는다.
- 개인정보: 문자·URL·창 제목·전체 HID 경로를 저장하지 않는다. 기본은 메모리 관찰, 명시적 내보내기에는 세션별 익명 장치/창 ID·타입·카운터·시간을 사용한다.
- 산출물: 실행 코드와 `M0_5_MEASUREMENT.md`. 코드 완성 후 실기기 로그·관측 한계·오탐/미탐을 기록한다.

### 2. M0.4: 경로 적합성 (T-003)

Probe 개발과 함께 UIAccess 목적·학교 정책·서명·설치 위치·실행 자격을 조사한다. [판단 문서](UIACCESS_POLICY_DECISION.md)의 빈 증거를 채운다. Tier A를 제품 후보로 확정하기 전 이 조건이 필요하다. Tier B는 서명/HVCI/복구 가능성 조사 후 별도 결정한다.

### 3. M1: DisplayResolver (T-012)

Probe 결과에 기반해 화면/장치 자동 매핑과 실패 사유를 구현한다. 복제·복수 후보·다른 터치 장치·출처 미확인은 UNSUPPORTED. 해상도·DPI·음수 좌표·USB 재열거를 처리한다. 수동 영역 선택을 대체 구현으로 남기지 않는다.

### 4. M4a: Whale 최소 실험 (T-040)

P1a를 제품과 별도 패키지로 유지하며 실제 Whale에서 debugger/CDP·Native Messaging·비전경 페이지 입력을 확인한다. 디버거 연결 **후** viewport를 읽는다. touch·포커스 에뮬레이션 명령은 설치 버전에서 성공/실패/미지원으로 기록한다. 시험 결과는 `WHALE_COMPATIBILITY_PROBE.md`에 남긴다. G1 PASS는 아님.

### 5. M2a 이후

M0.4 및 장치 경로 판정 후 최소 '소비만' 격리를 구현하고 G1을 실기기로 판단한다. 실패하면 Tier B 게이트/NO-GO. 통과 후 SeatEventV1·ProtectionStateManager·C4 통합·학생 탐색과 한글 품질로 진행한다. UI 상태 표현(T-050)은 먼저 구현 가능하지만 실제 보호 상태를 발급하는 기능으로 취급하지 않는다.

## 상태·메시지 계약: 구현 전에 확정할 항목

현재 type/session 메시지와 Native active/off 응답을 그대로 v1 계약으로 사용할 수 없다. 제공 snapshot은 제안이며 아직 프로토콜로 구현하지 않았다.

- GET_STATUS/START/STOP/PAUSE/RESUME/RECHECK의 requestId·ACK/ERR·중복 명령 처리.
- Native 프로세스/브리지 재연결 epoch, snapshot revision·증거 유효 기간과 ACK 관측 시점. 서로 다른 프로세스의 monotonic clock 숫자를 직접 빼지 않음.
- 격리·장치 매핑·하드웨어 검증·어댑터 상태의 별도 증거. enum 문자열 ACTIVE만 수신해서 성공 처리하지 않음.
- 보호 상태 불명 시 UI 확인 중 및 경고, 실제 해제 ACK 없는 OFF 금지.
- contact/sourceDevice/sequence/isolationEpoch/geometryEpoch/navigationEpoch와 큐 상한·취소 순서.
- Worker 재시작 시 저장한 마지막 성공을 복원하지 않고 실제 Native 상태 조회. 원본 보호 프로세스와 Bridge 수명 분리.

숫자 TTL·큐 용량·watchdog 간격은 여기서 임의 확정하지 않았다. 입력·결함 관측 결과와 목표 지연에 맞춰 ADR로 확정한다.

## 이번 준비 검사

실제 실행 결과는 [HANDOFF의 2026-10-08 세션 기록](HANDOFF.md)에 남긴다. 빌드/단위 검사는 현행 v0.2.0의 개발 기준선이다. 제품 기능·Native 보호 경로·UI 계약·하드웨어 G1 검증을 대체하지 않는다.

## 개발 착수 상태

문서·도구·변경 경계가 준비되어 관찰 프로브 개발을 바로 시작할 수 있다. Tier A/B 제품 선택, 칠판 매핑, G1/G2, 학교 일반 배포는 미확인이다. 이번 준비에서 제품 Manifest 권한 변경은 0개다.
