# 웨일 듀얼 인풋 — 개발 요청 패키지 v1.1

> 작성일: 2026-10-08  
> 프로젝트: `LUCKYBRIDGE/naver` / Whale Dual Input  
> 목적: 기존 v7.3 기술 설계와 v1.0 개발계획을 **실행 가능한 개발 요청·확장 UI 설계·검증 명세**로 연결한다.  
> 상태: 기획/인계 패키지. 실제 입력 격리의 성공이나 배포 가능성을 입증한 자료가 아니다.

## 30초 요약

**Windows PC 한 대, 확장 디스플레이 두 대:** 교사의 물리 마우스와 키보드는 모니터 1뿐 아니라 모니터 2에서도 기존 Windows 방식으로 동작한다. 모니터 2에 연결된 전자칠판의 물리 터치만 **학생 논리 입력 2**로 독립 처리한다. 

**입력 격리 범위:** 모니터 2의 전자칠판 터치 장치 전체.  
**독립 입력 전달 범위:** 1차 버전은 모니터 2의 Whale 일반 웹 콘텐츠 + 별도 학생용 탐색 UI.  
**실사용 UX:** 최초 Windows 구성요소 설치 후 웨일 사이드바에서 ON/OFF만 누른다. **사용자 영역 드래그, 웹사이트마다 URL 등록, 화면 사각형 수동 보정 없음.**  
**최우선 합격:** 학생 터치가 교사 OS 커서·키보드 포커스·타이핑에 영향을 미치지 않아야 한다(G1). 이 목표는 아직 실기기로 증명되지 않았다.

## 이 ZIP에 들어 있는 파일

| 순서 | 파일 | 역할 |
|---|---|---|
| 1 | `README_START_HERE.md` | 개발자가 가장 먼저 읽을 인계 요약 |
| 2 | `01_product/PRODUCT_SCOPE.md` | 지원/비지원 범위, 사용자 경험, 금지 사항 |
| 3 | `02_ui/ON_OFF_UI_SPEC.md` | 사이드바 ON/OFF 화면·전 상태별 흐름·상세 문구 |
| 4 | `02_ui/design.md` | 화면 규격, 정보 구조, 컬러/타이포/반응형/접근성 |
| 5 | `06_mockup/index.html` | **로컬 브라우저에서 열리는 화면 시연용 목업** — 실제 입력 제어 아님 |
| 6 | `03_implementation/IMPLEMENTATION_TASKS.md` | 마일스톤별 코드 작업·선행 조건·완료 판정 |
| 7 | `03_implementation/REPOSITORY_INTEGRATION.md` | 기존 Whale 코드 보존·파일 연결·manifest 규칙 |
| 8 | `03_implementation/DEVELOPMENT_REQUEST_PROMPT.md` | 코딩 AI 또는 개발자에게 복사해 전달할 단일 요청문 |
| 9 | `04_quality/ACCEPTANCE_TESTS.md` | UI·G1·G2·장애·성능·배포 시험과 NO-GO 조건 |
| 10 | `04_quality/DECISION_AND_STATUS_TEMPLATE.md` | 실제로 검증한 사항과 미검증 사항 기록 템플릿 |
| 11 | `05_references/ARCHITECTURE_V7_3_MONITOR2.md` | 사용자 제공 최신 **원문**, 수정하지 않음 |
| 12 | `05_references/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md` | 기존 실행계획 **원문**, 수정하지 않음 |

## 개발 문서 우선순위

1. **안전·비밀관리·MV3에 대한 기존 저장소 루트 `AGENTS.md` 규칙과 실제 Windows 플랫폼 제약.**
2. `05_references/ARCHITECTURE_V7_3_MONITOR2.md` — 기술 구조·근거/미확인·입력 격리.
3. `05_references/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md` — 마일스톤·실증·중단 기준.
4. 본 패키지의 `PRODUCT_SCOPE.md`와 `ON_OFF_UI_SPEC.md` — **이번 요청에서 추가된 사용자 경험/UI 구현 상세**.
5. `design.md`, `IMPLEMENTATION_TASKS.md` 및 목업 — 상세 구현 가이드.

서로 충돌하면 **G1 안전 및 원본 입력 격리를 우선**하고, 임의로 문서를 덮어쓰지 말고 결정 로그와 ADR에 남긴다. `AGENTS.md`/`HANDOFF.md`는 기존 저장소에서 직접 확인한 뒤 증분 갱신한다.

## 바로 사용법

1. ZIP 압축을 푼다.
2. `06_mockup/index.html`을 브라우저에서 열어 UI 흐름을 검토한다. 목업의 상태 변경 버튼은 **시연 전용**이다.
3. GitHub `LUCKYBRIDGE/naver`를 열고 `AGENTS.md`, `HANDOFF.md`, 현행 소스를 읽는다.
4. `03_implementation/DEVELOPMENT_REQUEST_PROMPT.md` 내용을 코딩 도구에 전달하고 본 폴더를 첨부한다.
5. 기존 파일을 삭제하지 않고 브랜치/작업트리에서 단계별로 구현한다. 시험 전에는 `ACTIVE`/`G1 PASS`/`배포 준비`라고 표시하지 않는다.

## 안전 및 현실적 제약

- 설치형 Windows 프로그램은 필요하다. 확장앱만으로 물리 터치를 격리할 수 없다.
- Tier A(`RegisterPointerInputTarget`)는 UIAccess 정책/서명/타입 전체 리디렉션/보호 공백 문제가 있다. **제품 적용은 정책·실증 게이트 통과 전 미정이다.**
- Tier B(선택적 HID 필터)는 서명·관리자 권한·HVCI·드라이버 안정성이 해결된 경우만 진행한다.
- 교사 마우스가 모니터 2에 들어가도 차단하지 않는다. 일반 Windows 앱 전체를 학생 마우스로 제어한다고 홍보하지 않는다.
- MouseMux MIT SDK 예제는 참고 대상일 뿐 본 패키지에 타사 소스나 바이너리를 넣지 않았다. 라이선스 미확인 Chromium 패치 복사 금지.
- 학교 배포 가능성과 공모전 시연 성공은 별도의 검증 단계다.
