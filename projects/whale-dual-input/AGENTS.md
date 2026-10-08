# Whale Dual Input 최신 개발 기준

2026-10-08 사용자 제공 개발 인계 v1.1을 이 프로젝트의 최신 기준으로 적용한다. 루트 AGENTS.md의 안전·개인정보·MV3 규칙을 함께 따른다.

## 먼저 읽기

1. [개발 준비 현황과 다음 작업](01_기획_및_지침/docs/DEVELOPMENT_PREPARATION_V1_1.md)
2. [기술 아키텍처 v7.3](01_기획_및_지침/docs/ARCHITECTURE_V7_3_MONITOR2.md)
3. [실행계획 v1.0](01_기획_및_지침/docs/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md)
4. [구현 범위·문서 충돌 결정](01_기획_및_지침/docs/IMPLEMENTATION_SCOPE.md)
5. [UI 계약](01_기획_및_지침/docs/UI_ON_OFF_SPEC_V1_1.md), [파일별 재사용 점검](01_기획_및_지침/docs/CODE_REUSE_AUDIT.md), [과거 실패·인계](01_기획_및_지침/docs/HANDOFF.md)

## 구현 불변식

- 확장 모드의 단일 Windows PC에서 모니터 2 전자칠판 터치 원본을 격리한다. 입력 전달은 Whale 일반 HTTP(S) 콘텐츠와 별도 학생 탐색 UI로 제한한다.
- 교사 실마우스는 두 화면에서 자유롭게 동작해야 한다. 커서 사후 복원, 전역 SendInput, SetCursorPos, SetForegroundWindow, 좌표만 기준으로 한 WH_MOUSE_LL 차단은 해결책으로 사용하지 않는다.
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
