# 웨일 브라우저 및 웨일스페이스 확장 프로그램 핵심 개발 수칙

이 문서는 NAVER Whale / WhaleSpace 환경에서 성공적인 교육용 확장 프로그램을 개발하기 위한 핵심 기술 수칙 및 심사 기준 요약본입니다. 전체 상세 기준은 루트의 [AGENTS.md](../AGENTS.md)를 참조하세요.

---

## 1. 플랫폼 및 Manifest 규칙

1. **Manifest V3 준수**:
   - `manifest_version: 3`만 사용합니다.
   - background page 대신 **Service Worker** 기반으로 동작해야 합니다.
   - 원격 JavaScript (`<script src="https://...">` 및 `eval()`)는 엄격히 금지됩니다.

2. **`action`과 `sidebar_action` 동시 선언 금지**:
   - 웨일에서는 툴바용 `action`과 사이드바용 `sidebar_action`을 동시에 선언할 수 없습니다.
   - 수업 중 웹페이지와 동시에 활용하는 교육 도구는 **`sidebar_action` + Content Script** 조합을 기본으로 채택합니다.

3. **최소 권한 원칙**:
   - `<all_urls>`와 같은 무제한 권한을 남용하지 않습니다.
   - 활성 탭 일시 권한인 `"activeTab"`과 브라우저 저장소 `"storage"`를 우선 활용합니다.
   - API Key나 비밀 정보(Secret)를 클라이언트 코드에 절대 포함하지 않습니다.

---

## 2. 웨일 사이드바 UI 개발 규칙

1. **반응형 폭 대응 (390px ~ 590px)**:
   - 웨일 사이드바 기본 폭은 약 390px이며, 사용자가 최대 590px까지 늘릴 수 있습니다.
   - 가로 스크롤을 유발하는 고정 너비 레이아웃을 사용하지 않습니다.
   - 1열(Single-column) 카드 레이아웃을 권장합니다.

2. **성능 및 백그라운드 관리 (`visibilitychange`)**:
   - 사이드바가 닫히거나 숨겨졌을 때 불필요한 타이머, 폴링, 애니메이션을 계속 실행하지 않습니다.
   ```javascript
   document.addEventListener('visibilitychange', () => {
     if (document.visibilityState === 'visible') {
       startSync();
     } else {
       stopHeavyTasks();
     }
   });
   ```

---

## 3. 웹페이지 Content Script 수칙

1. **호스트 웹페이지 스타일 격리**:
   - `div`, `button`, `body` 같은 일반 태그에 스타일을 전역 적용하지 않습니다.
   - 반드시 고유 prefix (예: `whale-edu-*`) 또는 Shadow DOM을 적용합니다.
2. **비침습적 상호작용**:
   - 웹페이지의 기존 기능을 방해하거나 기본 동작(텍스트 복사, 스크롤 등)을 막지 않습니다.
   - DOM 탐색 실패 시 에러로 앱 전체가 중단되지 않도록 방어 코드를 작성합니다.

---

## 4. 교육용 UX 및 개인정보 보호

1. **3초의 법칙**:
   - 복잡한 로그인이나 긴 설명서 없이, 첫 화면을 보고 3초 안에 기능을 이해할 수 있어야 합니다.
2. **사고력 지원 (정답 생성기 금지)**:
   - AI나 자동화가 정답을 대신 풀어주는 방식이 아니라, 질문 유도, 힌트 제공, 생각 정리, 읽기 보조 등 학생의 주도적 학습을 돕는 도구여야 합니다.
3. **Local-first 원칙**:
   - 학생의 이름, 이메일, 탐색 기록 등 개인정보를 외부 서버로 무단 전송하지 않습니다.
   - `whale.storage.local` / `chrome.storage.local`을 기본 저장소로 삼습니다.
