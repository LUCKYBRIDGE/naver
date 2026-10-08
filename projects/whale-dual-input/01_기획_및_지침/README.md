# [웨일 듀얼 인풋] 프로젝트 전용 기획 및 개발 지침

이 문서는 **웨일 듀얼 인풋 (Whale Dual Input)** 프로젝트만을 위한 독립 기획 공간입니다.
다른 프로젝트와 섞이지 않도록 이 폴더 내부의 설정과 명세만 참조합니다.

**2026-10-08 최신 기준:** [개발 인계 v1.1 준비 현황](docs/DEVELOPMENT_PREPARATION_V1_1.md)과 [프로젝트 AGENTS](../AGENTS.md)를 먼저 읽으세요. 확장 모드 모니터 2 전자칠판의 원본 터치 격리(G1)가 최우선이며, 입력 전달은 Whale 웹 콘텐츠로 제한합니다. 사이트 등록·영역 드래그는 정상 사용 흐름에서 제외합니다. v4와 기존 오버레이는 과거 구현 이력입니다. [HANDOFF의 ON 직후 OFF 실패 기록](docs/HANDOFF.md)을 보존하며 이번 준비에서 오류를 수정하거나 실기기 성공을 확인하지 않았습니다.

---

## 🎯 프로젝트 기본 정보
- **프로젝트명:** 웨일 듀얼 인풋 (Whale Dual Input)
- **폴더 경로:** `projects/whale-dual-input/`
- **공모 분야:** [x] **분야 1: 수업 지원 · 학습 활동**
- **연계 웨일 서비스:** [x] **웨일 클래스** / [x] **팀보드** / [x] **웨일온**
- **대상 하드웨어:** 단일 교사 Windows PC + 모니터 2 전자칠판 (화면 확장 + USB 터치, 복제 모드는 v1 미지원)

---

## 📚 폴더 내 핵심 기획 문서

1. **[공모전 제출물 01] 기획의도 1페이지:**
   - 파일: [`기획의도_A4_1페이지.md`](기획의도_A4_1페이지.md)
   - 내용: 심사용 A4 1장 규격 기획서 (문제 정의, 3대 핵심 기능, 기대 효과 및 웨일 연계성)

2. **[최신 구현 기준] 아키텍처 v7.3·실행계획 v1.0·인계 v1.1:**
   - [`docs/ARCHITECTURE_V7_3_MONITOR2.md`](docs/ARCHITECTURE_V7_3_MONITOR2.md), [`docs/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md`](docs/DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md)
   - [`docs/IMPLEMENTATION_SCOPE.md`](docs/IMPLEMENTATION_SCOPE.md), [`docs/UI_ON_OFF_SPEC_V1_1.md`](docs/UI_ON_OFF_SPEC_V1_1.md), [`docs/CODE_REUSE_AUDIT.md`](docs/CODE_REUSE_AUDIT.md)
   - [기존 구현계획 v4](whale_dual_input_implementation_plan_v4.md)는 과거 결정/구현 이력으로 보존합니다.

3. **[실행 준비와 검증 문서]:**
   - [`docs/HANDOFF.md`](docs/HANDOFF.md): 다른 기기에서 이어받는 방법, 확정 요구사항과 첫 구현 과제
   - [`docs/PREPARATION.md`](docs/PREPARATION.md): 개발환경 관찰값, 실기기 준비 목록, 첫 구현 순서
   - [`docs/TEST.md`](docs/TEST.md): 실제 터치·Whale 시험, 커서·포커스·한글 입력 보호 및 장애 해제 기준
   - [`docs/PRIVACY.md`](docs/PRIVACY.md): 현재·후보 권한, 데이터 보존·삭제, Native 연결 보안
   - [`docs/IDEA.md`](docs/IDEA.md): 학교 현장 문제와 확장앱의 역할
   - [`docs/UX.md`](docs/UX.md): 교사·학생 사용 흐름과 오류·종료 처리

4. **[기획 이력] 개발기획안 v3.0:**
   - 파일: [`whale_dual_input_development_plan_v3.md`](whale_dual_input_development_plan_v3.md)
   - 초기 아이디어와 요구 조건을 보존한다. 기술적 보장으로 적힌 내용은 v4의 검증 기준을 우선한다.

현재는 Windows Native 호스트·터치 수신 창 후보·F9·세션·학생 키보드·한글 입력을 연결한 0.2.0 구현 패키지 단계다. 설치는 [Windows 사용 안내](docs/INSTALL_WINDOWS.md)를 따른다. 사용자 지시에 따라 구현을 먼저 진행했으며 Windows Whale·전자칠판의 실제 입력 보호는 아직 검증하지 않았다. 별도 P1a 도구도 유지한다.

---

## 🔒 격리 및 개발 원칙
1. 이 프로젝트의 모든 소스코드는 `02_제작_결과물/` 내부에만 작성합니다.
2. 교사의 물리 마우스와 키보드 입력을 보호합니다. 커서 이동·숨김, 포커스 탈취, 입력 누락을 허용하지 않으며 지연은 실제 측정으로 검증합니다.
3. 전자칠판 장치의 터치 전체를 원본 입력에서 격리하고, 지원 대상 Whale 웹 콘텐츠에만 학생 입력을 전달합니다. 교사 실마우스는 두 화면에서 자유롭게 사용합니다.
