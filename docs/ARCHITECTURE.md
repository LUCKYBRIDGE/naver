# 웨일스페이스 멀티 확장 프로그램 아키텍처 및 통합(Merge) 가이드

이 문서는 하나의 리포지토리에서 **여러 개의 독립적인 웨일 확장 프로그램을 개발**하고, 필요할 때 **두 개 이상의 프로그램을 단일 확장 프로그램으로 결합(Merge)하는 개발 구조와 원리**를 설명합니다.

---

## 1. 아키텍처 개요

브라우저 확장 프로그램은 각각 독립적인 `manifest.json`, Service Worker, Sidebar UI, Content Script를 가집니다. 따라서 여러 확장 프로그램을 개발하면서 향후 통합까지 유연하게 대처하려면 아래와 같은 폴더 구조를 사용합니다.

```text
c:\ai_dev\apps\naver\
├── packages/
│   └── shared/                 # 공통 모듈 (기본 타입, 스토리지 래퍼, 메시지 통신 유틸, 공통 스타일)
├── extensions/                 # 독립 확장 프로그램들이 위치하는 디렉토리
│   ├── app-a/                  # [독립 확장앱 A] 단독 빌드 및 웨일 브라우저 로드 가능
│   ├── app-b/                  # [독립 확장앱 B] 단독 빌드 및 웨일 브라우저 로드 가능
│   └── unified-app/            # [통합 확장앱] A와 B의 기능을 합친 단일 프로그램 (필요 시 구성)
└── scripts/                    # 새 확장 프로그램 자동 생성 및 아이콘 유틸리티
```

---

## 2. 왜 이런 구분이 필요한가?

웨일 브라우저 Manifest V3 환경의 주요 제약:
1. **단 하나의 `manifest.json`**: 브라우저에 등록되는 확장앱 1개당 매니페스트 1개, 서비스 워커 1개만 실행됩니다.
2. **사이드바 UI는 하나의 HTML 엔트리**: 브라우저는 사이드바 페이지로 하나의 HTML 파일만 엽니다.

따라서:
- **독립 개발 시**: `extensions/app-a`, `extensions/app-b` 각 폴더가 자체 `manifest.json`을 갖고 있어, 웨일 확장앱 관리자(`whale://extensions`)에 따로따로 로드하여 독립 테스트할 수 있습니다.
- **공통 자원 재사용**: `packages/shared/`에 스토리지 처리, 메시징 유틸리티 등을 모아두어 코드 중복을 방지합니다.

---

## 3. 독립 확장 프로그램 생성 방법

명령어를 실행하여 원하는 이름의 확장 프로그램 뼈대를 즉시 생성합니다:

```bash
node scripts/new-extension.mjs <폴더명> "<확장앱이름>"
```

생성되는 구조:
```text
extensions/<폴더명>/
├── manifest.json              # 웨일 Manifest V3 규격 (sidebar_action 설정 포함)
├── background/service-worker.js
├── content/content-script.js
├── sidebar/sidebar.html, sidebar.js, sidebar.css
└── icons/ (16, 48, 128 PNG 자동 생성)
```

생성 직후 웨일 브라우저(`whale://extensions`)에서 **[압축해제된 확장앱 로드]**로 해당 폴더를 선택하면 바로 실행됩니다.

---

## 4. 추후 두 프로그램을 하나로 합칠 때의 4단계 공식

독립적으로 개발한 App A와 App B를 단일 확장앱(`unified-app`)으로 결합할 때의 원칙입니다.

### 1) Manifest 병합
- 두 프로그램이 사용하는 `permissions`의 합집합 구성 (예: `storage`, `activeTab`)
- 주 UI가 사이드바인 경우 `sidebar_action`의 `default_page`를 통합 사이드바 HTML로 지정

### 2) Service Worker 병합
- `chrome.runtime.onInstalled` 이벤트에서 App A와 App B가 사용하는 스토리지 초기 데이터를 함께 설정
- 백그라운드 메시지 리스너 통합

### 3) Content Script 병합
- 웹페이지에 주입되는 CSS 클래스명에 각각 고유 prefix를 붙여 충돌 방지
- 메시지 액션(`action`) 이름을 구분하여 switch-case로 라우팅

### 4) 사이드바 UI 통합 (Tab Navigation 패턴 권장)
- 웨일 사이드바(~390px 폭)에서는 화면 스크롤이 길어지는 것보다 상단 탭(Tab)으로 App A와 App B 화면을 전환할 수 있게 구성하는 것이 사용자 경험에 가장 좋습니다.
