# 현행 코드 재사용 점검 — v1.1 준비

2026-10-08. 기준: `main` / `a5f0d23dae251d2ccb57494001c3b40b73241b56`, 제품 v0.2.0. 코드 정적 확인이며 실기기 재사용 합격이 아니다. 아래 파일은 실제 존재한다. 경로는 별도 표기 없으면 `02_제작_결과물/` 기준.

| 현재 파일 | 현재 역할 | 유지 | 변경 | 제거 후보 | 이유 | 후속 검사 |
|---|---|---|---|---|---|---|
| `manifest.json` | MV3·sidebar·Native/CDP 권한 | MV3/sidebar | 자동 탐색 권한 판단만 별도 | 불필요해지는 권한 | action 병설 없음, 현재 권한 유지 | SEC-01·권한 리뷰 |
| `background/service-worker.js` | session 로드·설치 이벤트 | 진입점 | 재시작 상태 조회 연결 | 없음 | Worker 수명 독립 필요 | G2-10·UX-09 |
| `background/session.js` | CDP·Native·입력 큐·메모리 상태 | 제한된 CDP 호출·sender 검증·늦은 attach 정리 후보 | Native 권위 상태, 명령 ACK, 자동 대상/좌표, 멀티접촉 | 사이트 수동 select·READY/STARTING 설계·이동/resize OFF | 단순 active 응답으로 ACTIVE, OFF 확정에 Native 해제 ACK 없음 | 늦은 ACK·교착·stale epoch·G2-06/07/10 |
| `sidebar/sidebar.html` | 사이트/탭 선택·연결·ON/OFF | 패키지 UI 진입점 | 상태·3행 연결·주 버튼·진단 | URL 입력·영역 지정 안내 | 새 ON/OFF UI 계약과 충돌 | UX-01~12 |
| `sidebar/sidebar.js` | 개별 origin 승인·버튼·Worker 상태 표시 | API 래퍼 후보 | 8상태·reasonCode·실제 snapshot | 사이트 등록·로컬 상태 확정 | Native의 실제 보호 상태를 표시해야 함 | 재오픈·단절·중복 클릭 |
| `sidebar/sidebar.css` | 한 열·44px 버튼 | 최소 크기·포커스 링 | design 토큰·300px/200% | 없음 | 상태 경고 및 접근성 보완 | UX-10/11 |
| `content/content-script.js` | Shadow DOM 키보드·포인터·describe | 스타일 격리·입력란 식별 후보 | 자동 재주입·navigation/geometry·지원 경계 | resize/pagehide 즉시 pause→OFF | 디버거 배너 높이 변경이 OFF 원인 후보. contenteditable 현재 미지원 | G2-04/06/07·원래 페이지 영향 |
| `content/hangul.js` | 순수 두벌식 조합기 | 독립 모듈 재사용 후보 | 필요 시 경계 보완 | 없음 | Windows IME를 호출하지 않음. 현재 npm test는 이 모듈을 직접 검증하지 않음 | 겹받침·백스페이스·실제 Whale 입력 |
| `native/WhaleDualInput.Host/Program.cs` | stdio 길이 프레임·queue·Installer·감시 | 크기 제한·확장 ID 제한·stdout 분리 후보 | Bridge/설치/보호 수명 분리·ACK/ERR·IPC 인증 | Host 죽으면 보호가 유지된다는 가정 | 현재 메시지는 type/session, requestId/상태 증거 없음. 종료 감시는 보호 보증 아님 | SEC-02·브리지 재시작·G1-08 |
| `native/WhaleDualInput.Host/ControlWindow.cs` | 창 선택·수동 viewport·F9·TouchOverlay | F9/해제·창 조회의 일부 후보 | 장치 자동 매핑·보호 상태 관리 분리 | ScreenSelection·TouchOverlay를 격리 엔진으로 쓰는 방식 | HTTRANSPARENT는 외부 Whale 마우스 통과 증거 없음. 단일 접촉만 관리 | 교사 모니터 2 클릭·G1 양성 대조군 |
| `native/WhaleDualInput.Host/Native.cs` | Win32 선언·터치발 마우스 서명 | 검증된 필요한 선언 후보 | Display/Raw Input/Pointer API 별도 모듈 | 서명만으로 장치 확정하는 가정 | 현재 GetPointerDevices/GetPointerDeviceRects/QueryDisplayConfig/RegisterPointerInputTarget 없음 | 구조체 ABI·장치 매핑·OFF 관측 |
| `native/WhaleDualInput.Host/WhaleDualInput.Host.csproj` | net10.0-windows WinForms | .NET 10·자체 포함 빌드 | 신규 모듈/서명은 별도 | 없음 | 현재 UIAccess 앱 manifest·서명 설정 없음 | publish·서명 자격 확인 |
| `icons/icon-16.png` | 확장 아이콘 | 유지 | 없음 | 없음 | 계획 개편과 무관 | MV3 리소스 검사 |
| `icons/icon-48.png` | 확장 아이콘 | 유지 | 없음 | 없음 | 계획 개편과 무관 | MV3 리소스 검사 |
| `icons/icon-128.png` | 확장 아이콘 | 유지 | 없음 | 없음 | 계획 개편과 무관 | MV3 리소스 검사 |
| `tests/p1a/probe/manifest.json` | 루프백/debugger 전용 실험 action | 제품과 분리 | M4a 필요 시 별도 검토 | 없음 | 별도 manifest의 action은 제품 sidebar와 충돌하지 않음 | 실험/제품 산출물 분리 |
| `tests/p1a/probe/engine.js` | 고정 CDP 스위트·대상/문서 검사 | M4a 기초 | touch·포커스 후보 실험 추가는 별도 | 제품 정책으로 복사 금지 | 기존 viewport 변화 차단은 고정 시험 보호 정책 | probe.test·실제 Whale |
| `tests/p1a/probe/worker.js` | 실험 sender·대상·CDP 연결 | 재사용 후보 | M4a 범위만 | 없음 | 제품 보호 상태 관리자와 다름 | worker.test |
| `tests/p1a/probe/panel.js` | 실험 실행·취소·보고 | 유지 | 필요 시 M4a 보고 | 없음 | 명시적 실행 화면 | P1a 실험 |
| `tests/p1a/probe/panel.html` | 실험 제어 UI | 유지 | 없음 | 없음 | 제품 ON/OFF UI가 아님 | P1a 로드 |
| `tests/p1a/probe/panel.css` | 실험 화면 스타일 | 유지 | 없음 | 없음 | 제품과 분리 | P1a 화면 |
| `tests/p1a/p1a.html` | 고정 입력 fixture | 유지 | 필요 시 touch fixture | 없음 | 민감한 수업 페이지 없이 입력 결과 검사 | server·실제 Whale |
| `tests/p1a/fixture.js` | 클릭/드래그/문자 관찰 | 유지 | M4a 관측 확장 후보 | 없음 | 실제 OS 포커스 측정은 아님 | 실제 페이지 값 |
| `tests/p1a/fixture.css` | 시험 요소 스타일 | 유지 | 없음 | 없음 | 제품 주입 CSS와 분리 | fixture 화면 |
| `tests/p1a/server.mjs` | 루프백 고정 자산 서버 | 유지 | 없음 | 없음 | 제품 운영 서버로 쓰지 않음 | server.test |
| `tests/p1a/probe.test.js` | 프로브 입력·중단 경계 | 유지 | M4a 변경 때 필요한 검사 | 없음 | C2/제품 session 테스트 아님 | npm test |
| `tests/p1a/worker.test.js` | 실험 Worker sender/중복 실행 | 유지 | 실험 변경에 맞춤 | 없음 | 제품 Worker 검증과 구분 | npm test |
| `tests/p1a/server.test.js` | 고정 라우팅·루프백 | 유지 | 없음 | 없음 | 저장소 노출 차단 확인 | npm test |
| `tests/p1a/browser-smoke.mjs` | headless Chrome/Playwright CDP | 참고 유지 | 실제 Whale 시험을 별도로 기록 | G1 증거로 인용 금지 | 확장 debugger·Windows 물리 키보드·터치를 사용하지 않음 | G1 NOT TESTED 유지 |
| 루트 `scripts/build-p1a.mjs` | JS 문법·MV3·제품/프로브 별도 복사 | allowlist 유지 | 새 리소스 구현 시만 | 없음 | Native 폴더·실험은 제품 extension에서 제외 | npm run build |
| 루트 `scripts/build-windows.mjs` | Host publish·v0.2.0 ZIP | 로컬 캐시·분리 패키징 | 이번 준비: Windows 로컬 dotnet.exe 탐색. 향후 버전/새 모듈 패키징 | v0.2.0을 최신 기능으로 오인하는 표시 | 기존 로컬 SDK 경로는 Windows .exe를 찾지 못했음 | npm run build:windows |
| 루트 `scripts/package-project.mjs` | 공통 ZIP·Dual Input 전체소스 압축 차단 | 차단 유지 | 새 패키징 시 별도 | 없음 | 실험 코드 제출 방지 | 차단 코드 정적 확인 |
| 루트 `package.json` | Node 24·검사 명령 | 유지 | 새 Native 도구 구현 후 명령 추가 | 없음 | 의존성 설치 불필요 | 기존 명령 |

## 보존하는 실패 현상

2026-10-07 사용자 Whale 시험에서 사이트·영역 선택 후 ON → 디버거 안내줄 → '페이지 크기·배율 변경' OFF가 관측됐다. 코드의 연결 전 viewport 저장과 ACTIVE resize 해제는 원인 후보다. 이번 준비에서는 오류를 재현하거나 수정하지 않았다. [HANDOFF](HANDOFF.md)의 기존 기기·버전·패키지 기록을 그대로 보존한다.

현재 `active` Native 응답은 overlay.Show 성공 뒤 발급된다. 격리 등록·소유권·장치 매핑·하드웨어 검증을 의미하지 않는다. 기존 UI의 ACTIVE는 새 v1.1 합격 상태와 동일시할 수 없다. 호스트 종료·브리지 끊김을 무조건 OFF로 표시하는 기존 구현도 새 보호 계약에 맞춰 변경해야 한다.

## 다음 변경의 최소 단위

1. 관찰 전용 Probe와 Device/Display 식별부터 신규 모듈로 구현한다.
2. 상태·프로토콜 계약을 확정한 뒤 Bridge/Worker를 변경한다.
3. 보호 경로가 실제 확인된 후 물리 이벤트를 C4에 연결한다.
4. 제거 후보는 대체 경로와 해당 검사 결과를 확보한 뒤 정리한다. 이번 준비에서 제품 코드 삭제는 0건이다.
