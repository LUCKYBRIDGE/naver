# P1a 로컬 입력 전달 실험

상태: 시험 도구 구현. Windows Whale·실제 USB 터치의 입력 보호는 미검증.

## 준비

저장소 루트에서 Node.js 24 이상을 사용한다. 일반 빌드·단위 테스트에는 npm 의존성 설치가 필요 없다.

```powershell
npm test
npm run build
npm run p1a:serve
```

서버는 `127.0.0.1:8765`에서 정해진 시험 파일 3개만 제공한다. 학생 데이터나 원격 서버를 사용하지 않는다. 종료는 Ctrl+C다.

## Whale에서 실행

1. `whale://extensions`에서 개발자 모드를 켜고 **별도 실험 확장앱**인 `dist/whale-dual-input/p1a-probe`를 로드한다. 기존 제품 확장앱 폴더와 혼동하지 않는다.
2. 별도 학생 웨일 창에서 `http://127.0.0.1:8765/p1a.html`을 연다. 시험 탭은 그 창에서 선택된 탭이어야 하며 화면에 보여야 한다. 1280×800 정도의 넓이·높이로 시작한다. 시험 요소가 viewport 밖이면 도구가 거부한다.
3. 실험 확장앱 툴바 버튼으로 제어 화면을 연다. 목록에서 학생 탭 ID와 창 ID를 확인한다. 각 시행 전 시험 페이지를 새로고침하고 URL에 query/hash가 없는지 확인한다.
4. `5초 뒤 시험 실행`을 누르고 **직접** 메모장 등 교사 작업 앱으로 이동해 알려진 문장을 계속 타이핑한다. 시험 도구는 탭·창 활성화나 커서 복원 명령을 호출하지 않는다.
5. 클릭 → 드래그 → 중첩 스크롤 → input/textarea 문자 입력·Backspace·Enter가 순서대로 수행된다. 10초 정도 뒤 제어 화면의 `결과 확인`을 누른다. 다른 웨일 창에서 교사가 입력하는 경우와 한글 IME 조합 중에도 반복한다.
6. 교사 커서·foreground·IME·문자 오류를 TEST.md 양식으로 별도 기록한다. 아래 페이지 관찰값만으로 P1a 성공을 선언하지 않는다.

초기화된 시험 페이지의 기대값:

| 관찰값 | 기대 결과 |
|---|---|
| clicks | 1 |
| drags | 1 |
| scrollTop | 0보다 큼 |
| input | `한글 ABC 12` |
| textarea | `한글 ABC 12` + 줄바꿈 |
| events | 실제 발생한 이벤트 type/target/isTrusted 기록 |
| teacherFocus | 항상 UNVERIFIED. 이 도구는 OS 입력 보호를 측정하지 않음 |

`OBSERVED`는 API 실행 및 관찰값 반환을 뜻한다. 페이지 값이 기대와 다르면 실패로 기록한다. iframe·리치 편집기·실제 사이트·학생 터치 격리·한글 조합기는 이 프로브 범위에 없다. 링크는 수동으로 기본 동작을 시험한다.

## 중단·장애

- 제어 화면의 `중단`은 AbortSignal로 남은 명령을 취소한다. 같은 문서가 남아 있으면 눌린 학생 버튼/키를 해제하고 debugger를 분리한다. F9 전역 해제는 아직 구현하지 않았다.
- 탭 탐색·종료·debugger 해제·창 크기/좌표 변경 시 중단한다. 탭 URL과 문서 토큰을 매 명령 전에 검사하나 비동기 검사와 입력 사이의 모든 경합을 원자적으로 방지한다는 보장은 없다. 이 때문에 로컬 시험 페이지만 대상으로 한다.
- DevTools와 debugger 연결 충돌, API 시간 초과, 정책 거부를 실패로 기록한다. 시간 초과 후에는 실험 확장앱을 다시 로드하고 브라우저의 debugger 연결 안내가 해제됐는지 확인한다. 분리가 실패하면 실험 확장앱을 끄거나 제거한다.
- Worker 재시작 후 시험을 자동 복원하지 않는다. 실행 상태가 사라졌다면 디버거 연결을 확인하고 실험 확장앱을 다시 로드한다. ON 영구 저장·백그라운드 자동 재시도·폴링은 없다.
- 결과는 Worker 메모리에서만 보관하며 다음 시험이나 재시작 때 폐기한다. 교사 문서·키보드 입력을 수집하지 않는다.

## 권한과 패키지 분리

기존 제품 Manifest는 그대로다. 실험 Manifest만 `debugger`, `http://127.0.0.1/*` host 권한을 사용한다. Chrome 계열 match pattern은 포트별 제한을 제공하지 않아 코드에서 URL을 `http://127.0.0.1:8765/p1a.html`과 정확히 비교한다. tabs/scripting/nativeMessaging 권한은 추가하지 않았다.

`debugger` 자체는 강한 권한이다. 프로브의 URL 제한은 앱 코드의 제한이며 권한 capability를 축소하지 않는다. Input 메서드와 로컬 시험 페이지의 고정 `window.p1aSnapshot()` 관찰식만 사용한다. 사용자 메시지에서 임의 CDP 명령·스크립트·문자를 받지 않는다.

`npm run build`는 소스 문법·Manifest 참조를 검사하고 `dist/whale-dual-input/extension`과 `p1a-probe`를 각각 만든다. 알려진 출력 파일만 갱신하며 다른 파일을 삭제하지 않는다. 기존 폴더 전체 ZIP 명령은 이 프로젝트에 실험 파일이 섞이는 것을 막기 위해 차단된다. 공모전 제출·exe·Native 설치 패키지는 P2/P5 이후 준비한다.

## 선택적 Chrome CDP 검사

macOS에 Chrome과 외부 경로의 Playwright가 준비되어 있다면 서버를 실행한 상태에서 다음과 같이 확인한다. WAN2에 node_modules를 설치하지 않는다.

```text
node projects/whale-dual-input/02_제작_결과물/tests/p1a/browser-smoke.mjs <외부-playwright/index.mjs-절대경로>
```

이는 실제 CDP 메서드로 시험 페이지 값과 별도 브라우저 탭의 자동 입력 문자열을 비교한다. 확장앱의 chrome/whale.debugger 연결, 물리 키보드, Windows foreground, 실제 Whale 실행을 검증하지 않는다.

공식 참고: [Whale API](https://developers.whale.naver.com/api/extensions/), [Chrome debugger](https://developer.chrome.com/docs/extensions/reference/api/debugger), [CDP Input](https://chromedevtools.github.io/devtools-protocol/tot/Input/).
