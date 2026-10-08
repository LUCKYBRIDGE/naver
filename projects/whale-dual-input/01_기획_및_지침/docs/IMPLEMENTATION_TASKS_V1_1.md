> 2026-10-08 프로젝트 연결본. [인계 원본](../../../Whale_Dual_Input_Development_Handoff_v1_1/03_implementation/IMPLEMENTATION_TASKS.md)은 보존하며, 이 사본의 상대 경로만 프로젝트 문서 위치에 맞췄습니다.

# 개발 작업 목록 · 단계별 우선순위 · 완료 조건

> 최신 기술 원칙: `ARCHITECTURE_V7_3_MONITOR2.md`  
> 최신 실행계획: `DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md`  
> UI 계약: `UI_ON_OFF_SPEC_V1_1.md`  
> **중요:** UI와 브라우저 모의 이벤트 실험은 G1 증명을 대체할 수 없다.

## Phase 0 — 기준선 고정 및 현행 코드 확인

**T-001 (P0)** `AGENTS.md`, `HANDOFF.md`, manifest, sidebar, background, content, Windows Host, 빌드 스크립트 점검 → 변경 영향표 작성.  
완료: `CODE_REUSE_AUDIT.md`, 기존 실패 현상과 재사용 후보 보존.

**T-002 (P0)** `ARCHITECTURE_V7_3_MONITOR2.md`, `DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md`, `ON_OFF_UI_SPEC.md`, `design.md`를 프로젝트 문서 경로에 연결.  
완료: 루트 정책과 우선순위 충돌 점검, 신규 파일만 별도 커밋.

**T-003 (P0)** Tier A의 UIAccess 목적·서명·설치/운영 가능성, Tier B 서명·HVCI·권한 경계 조사.  
완료: `UIACCESS_POLICY_DECISION.md`, `INSTALLATION_FEASIBILITY.md`, 경로 `제품 후보/연구 전용/제외` 판정 기록.

## Phase 1 — 모니터 2 및 칠판 장치 진단(필수 선행)

**T-010 (P0)** `WhaleDualInput.Probe` 제작: Windows 디스플레이 확장/복제·모니터 2 RECT·DPI/회전, `GetPointerDevices`/`GetPointerDeviceRects`, Raw Input/HID, sourceDevice 및 PT_TOUCH/PT_PEN/PT_MOUSE 기록.  
완료: 기기별 익명화 로그 + `M0_5_MEASUREMENT.md`. 입력 원문/학생 타이핑 수집 금지.

**T-011 (P0)** 교사 커서 납치 판정 로거: Raw Input 마우스 원본, `GetCursorPos`, `EVENT_SYSTEM_FOREGROUND`, `EVENT_OBJECT_FOCUS`, `GetGUIThreadInfo`, 터치에서 유래된 WM_MOUSE 이벤트.  
완료: OFF 상태에서 **양성 대조군** 확인. 검출 실패 로거의 0건은 증거 아님.

**T-012 (P0)** 자동 장치/화면 매핑과 실패 사유 코드 구현. 수동 사각형 드래그 금지.  
완료: 정상 기기 자동 연결, 복제/미매핑/여러 후보는 `UNSUPPORTED`로 안전 종료.

## Phase 2 — 격리 PoC 및 G1 판정(최우선 기술 게이트)

**T-020 (P0)** M0.4로 제품 사용 조건이 인정된 경우에만 Tier A UIAccess + `RegisterPointerInputTarget` 연구 구현. 메시지 스레드 최소화, `WS_EX_NOACTIVATE`/message-only 조건 검증.  
완료: 물리 칠판 PT_TOUCH가 Windows의 원래 입력에 전달되지 않는지 **삼키기만 하는** 시험.

**T-021 (P0)** 터치→호환 마우스 누출 여부, 교사 마우스 모니터 2 이동/클릭, 동시 타이핑, 다른 터치 장치, 등록 충돌, 강제 종료 보호 공백 시험.  
완료: `M2A_G1_REPORT.md`에 통과/실패/미확인 구분. Tier A 실패 시 기존 Windows 커서 복원으로 대체 금지.

**T-022 (P0)** Tier A가 불가·부적합하면 Tier B의 **서명/설치/안전 복구 가능성 선행 검증**. 통과하지 못하면 NO-GO.  
완료: `DRIVER_SECURITY.md`, `INPUT_SUPPORT_MATRIX.md`, 후속 연구 여부 결정. 승인 없이 드라이버 설치 금지.

## Phase 3 — 학생 논리 입력·보호 상태 관리자

**T-030 (P1)** `SeatEventV1` 이벤트 모델 및 bounded queue, contactId/sequence/isolationEpoch/geometryEpoch 관리.  
완료: 재연결·접촉 취소·버튼 해제·중복 클릭 차단 시험.

**T-031 (P1)** `ProtectionStateManager`: OFF, DISCOVERING, PREPARING, ACTIVE, SAFE_HOLD, LOST_PROTECTION, PAUSED_BY_TEACHER, UNSUPPORTED; 상태별 ACK·TTL·회복 정책.  
완료: 단위/결함 주입 테스트로 ACTIVE 거짓 표기 없음.

**T-032 (P1)** 학생 가상 포인터/멀티터치 채널과 모니터 2 좌표 렌더(선택).  
완료: 교사 Windows 시스템 커서 이동 0건, 학생 포인터는 모니터 2 제한.

## Phase 4 — Whale 어댑터 조기 검증 및 통합

**T-040 (P0, 병행 가능)** 실제 Whale MV3 debugger/Native Messaging/`Input.dispatchMouseEvent`/`Input.dispatchTouchEvent`/`Input.insertText` 최소 호환성 확인. **가상 이벤트만 사용하는 연구 시험은 G1 PASS 아님**.  
완료: `WHALE_COMPATIBILITY_PROBE.md`.

**T-041 (P1)** Whale HWND/창-탭 자동 식별, 디버거 연결 후 viewport/DPI 계산, 페이지 이동/탭/팝업/Worker 복원, stale event 폐기.  
완료: 사이트별 지정·영역 드래그 없이 자동 입력 라우팅.

**T-042 (P1)** 사용자 G1 시험을 통과한 물리 학생 이벤트 → Native → Whale CDP 연결. 웹게임/Canvas/폼/한글 키보드 검증.  
완료: 교사 동시 타이핑 + 학생 터치 상호작용 실제 통과.

## Phase 5 — 웨일 확장 UI 구현(병행 가능한 화면 작업)

**T-050 (P1)** 기존 `sidebar_action` UI를 새 `ON_OFF_UI_SPEC.md` / `design.md`에 맞게 개발. **같은 manifest에 `action` 병설 금지**.  
완료: 320–400px, 200% 확대, 스크린리더 상태 안내, 키보드 조작 정상.

**T-051 (P1)** 8개 핵심 상태 + 설치 필요/미지원 사유별 문구, 실제 Native 스냅샷 상태 바인딩.  
완료: 새로고침/사이드바 재오픈/Native 끊김에도 거짓 ACTIVE 없음.

**T-052 (P1)** 교사 일시 해제 확인, 보호 상실 경고, 안전 OFF, 진단 상세/복사, 학생 웹 연결 안내.  
완료: `PAUSED_BY_TEACHER`에서는 '일반 Windows 터치' 위험 문구 반드시 노출.

**T-053 (P2)** 학생용 별도 탐색 UI: 뒤로/앞으로/주소/새로고침/탭/가상 키보드.  
완료: OS 포커스를 빼앗거나 원래 브라우저 크롬 UI에 독립 입력을 주입한다고 가정하지 않음.

## Phase 6 — 설치/배포/실기기 수업 시험

**T-060 (P0)** Windows 구성요소 최초 설치, Native Messaging 등록, 확장 ID 제한, 설치/제거·복구 안내.  
완료: 학교 정책/관리자 권한 조건을 솔직히 표시, 자동 다운로드 실행 금지.

**T-061 (P0)** `ACCEPTANCE_TESTS.md` G1 필수 실기기 테스트, 오류/보호 공백, G2 웹 호환, 지연 p50/p95/p99.  
완료: 관찰 0건과 완전 보증 구분, NO-GO 1건 있으면 제품 완료 판정 금지.

**T-062 (P1)** 시연 가이드·릴리스 노트·지원 매트릭스·학교 배포 조건.  
완료: '웨일 확장만 설치하면 완료' 오인 방지, 로컬 우선/개인정보 최소화.

## 병렬 처리 가능성

- M0.4 정책 조사와 M0.5 장치 진단, T-040 Whale CDP 호환성 실험은 병행 가능.
- UI 목업/T-050 상태 표현 자체는 먼저 구현 가능하나 백엔드 연결 전 `실제 독립 터치` 성공으로 표시 금지.
- Tier B 구현은 Tier A 실패뿐 아니라 **정책/서명/설치 게이트** 통과 후에만.

## 보고 형식(매 세션 종료 시)

`수정한 파일 / 구현된 기능 / 실행된 검사 / 실제 칠판 여부 / G1 보호 여부(미확인·실패·통과) / G2 호환성 / 남은 장애 / 다음 단계`. 기록되지 않은 실기기 성공을 추정하거나 요청하지 않은 GitHub 본 브랜치를 덮어쓰지 않는다.
