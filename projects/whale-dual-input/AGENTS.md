# Whale Dual Input 최신 개발 기준

2026-10-08 사용자 제공 개발 인계 v1.1을 이 프로젝트의 최신 기준으로 적용한다. 루트 AGENTS.md의 안전·개인정보·MV3 규칙을 함께 따른다.

**2026-10-13 사용자 변경 요구가 우선한다:** ON 중 교사가 모니터 2를 조작하지 못해도 된다. 교사는 모니터 1에서 작업하고 ON/OFF·비상 해제를 실행할 수 있어야 한다. [모니터 1 보호 모드 ADR](01_기획_및_지침/docs/ADR_MONITOR1_PROTECTION_MODE.md)을 먼저 읽는다. v7.3의 '교사 실마우스 두 화면 자유 사용' 조건을 이 모드에 적용하지 않는다. 커서 제한·화면 기준 차단은 검토 후보이며 원본 입력 격리 성공의 증거가 아니다.

최종 입력 계약: 칠판 입력 전체를 원래 Windows 커서·클릭·포커스 경로에서 차단하고 별도 학생 논리 마우스/멀티터치 채널로만 전달한다. 학생 가상 키보드는 학생 웹 대상만 편집한다. 임시 관찰 팝업 내부 수신이나 독립 커서 표시만으로 이 목표를 충족했다고 하지 않는다. [현재 관찰 결과](01_기획_및_지침/docs/M0_5_MEASUREMENT.md)와 [연구 도구 안내](01_기획_및_지침/docs/INPUT_PROBE_RUNBOOK.md)를 확인한다.

## 먼저 읽기

2026-10-14 [Native 보호 상태 계약](01_기획_및_지침/docs/PROTECTION_STATE_CONTRACT.md)을 추가했다. 순수 상태 코어이며 물리 격리 backend/제품 Worker에 연결 전이다. 합성 증거를 실제 ACTIVE 발급에 쓰지 않는다.

2026-10-14 [Tier A 원본 소비 backend](01_기획_및_지침/docs/ISOLATION_A_IMPLEMENTATION.md)를 구현해 연구용 등록/해제 결과만 상태 코어에 연결했다. 제품 Worker·Whale 전달은 미연결이다. `build:isolation-research`는 서명되지 않은 uiAccess=true 연구 빌드만 생성하고 설치/실행하지 않는다. 정책 적합성·서명·보호 위치·실제 토큰·장치 매핑 전에는 활성화하지 않는다. 등록 성공만으로 G1 PASS/ACTIVE를 발급하지 않는다.

1. [개발 준비 현황과 다음 작업](01_기획_및_지침/docs/DEVELOPMENT_PREPARATION_V1_1.md)
2. [기술 아키텍처 v7.3](01_기획_및_지침/docs/ARCHITECTURE_V7_3_MONITOR2.md)
3. [실행계획 v1.0](01_기획_및_지침/docs/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md)
4. [구현 범위·문서 충돌 결정](01_기획_및_지침/docs/IMPLEMENTATION_SCOPE.md)
5. [UI 계약](01_기획_및_지침/docs/UI_ON_OFF_SPEC_V1_1.md), [파일별 재사용 점검](01_기획_및_지침/docs/CODE_REUSE_AUDIT.md), [과거 실패·인계](01_기획_및_지침/docs/HANDOFF.md)

## 구현 불변식

- 확장 모드의 단일 Windows PC에서 모니터 2 전자칠판 터치 원본을 격리한다. 입력 전달은 Whale 일반 HTTP(S) 콘텐츠와 별도 학생 탐색 UI로 제한한다.
- ON 중 교사 실마우스의 모니터 2 접근·조작 제한을 허용한다. 모니터 1 작업 및 ON/OFF·비상 해제는 유지해야 한다. ClipCursor·화면 기준 WH_MOUSE_LL 차단은 새 요구 아래 연구 후보로 검토할 수 있으나 클릭·포커스·터치 경로를 함께 검증한다. 커서 사후 복원, 전역 SendInput, SetCursorPos, SetForegroundWindow로 간섭을 숨기는 방식은 계속 금지한다.
- 수동 사각형 선택·사이트별 URL 등록·제조사 선택을 정상 사용 흐름에 넣지 않는다. 장치나 매핑이 모호하면 시작을 거절한다.
- C1 장치 식별, C2 원본 격리, C3 학생 채널, C4 웹 어댑터를 분리한다. CDP나 가상 커서만으로 C2 성공을 선언하지 않는다.
- ACTIVE는 Native 보호 관리자의 유효한 격리 증거와 매핑·학생 채널·어댑터 확인 이후에만 표시한다. 연결 불명은 UI의 '연결 확인 중'으로 처리하며 SAFE_HOLD나 OFF를 추정하지 않는다.
- SAFE_HOLD는 격리 생존을 확인한 전달 장애, LOST_PROTECTION은 보호 소멸이다. STOP/PAUSE 뒤 실제 해제 ACK 전에 OFF/PAUSED를 확정하지 않는다. 재개는 선행검사와 PREPARING을 거친다.
- 사이드바 숨김 시 UI 반복 작업은 중단하되 Native 보호 엔진을 UI 수명에 묶지 않는다. Worker 재시작·재오픈 때 권위 있는 Native 상태를 재조회한다.
- Tier A는 UIAccess 목적·서명·설치 게이트 뒤의 후보다. 다른 터치 장치가 공존하면 v1은 UNSUPPORTED다. Tier B는 서명·HVCI·설치/복구 여건을 확인한 후 연구한다. 드라이버 설치는 별도 사용자 승인 없이 진행하지 않는다.
- 기존 소스는 보존하고 기능별로 개편한다. 원본 인계 폴더와 ARCHITECTURE_V5.md는 임의 변경하지 않는다. 기능 변경은 codex/ 작업 브랜치에서 시작한다.
- 확장 소스는 02_제작_결과물/에 둔다. 신규 Windows 모듈도 우선 그 아래 native/에 둔다. 목표 트리의 프로젝트 최상위 windows/ 이관은 별도 ADR과 빌드 수정으로 처리한다.
- 실제 칠판 G1 통과 전 G2 부가 기능에 우선 투자하지 않는다. 진단 구현·M4a 최소 Whale 실험·UI 상태 표현은 선행 가능하며 실기기 성공으로 보고하지 않는다.

## 기록과 검사

기능 구현 후 적절한 자동 검사를 실행한다. 테스트를 구현 시작의 선행 조건으로 삼지 않는다. 원문 해시·문서 준비·빌드·실기기 합격은 별도로 기록한다. 매 세션은 수정 파일, 기능, 검사 명령/결과, 실제 칠판 여부, G1/G2, 남은 장애, 다음 작업을 보고한다.

현재 명령: npm test / npm run build / npm run build:windows / npm run p1a:serve. build:windows는 DOTNET_EXE → 사용자 로컬 .NET → 시스템 dotnet 순서로 사용한다. 빌드 산출물은 현재 v0.2.0이며 v1.1 기능 완료를 의미하지 않는다.

신규 `npm run build:input-probe`: Windows 연구 도구를 `dist/whale-dual-input/input-probe/`에 별도 게시. `--self-test` 정책/ABI/채널 경계, `--snapshot` 읽기 전용 진단, `--smoke-observe` 읽기 전용 수명주기 검사. 관찰 중에는 원본 입력이 살아 있어 교사 커서 납치가 가능하다. 차단 후보는 명시적 시험만, ACTIVE 증거로 쓰지 않는다. 제품 패키지에 연구 도구를 섞지 않는다.
