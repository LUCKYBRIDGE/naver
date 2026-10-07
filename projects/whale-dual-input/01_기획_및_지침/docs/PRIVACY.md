# 권한 및 데이터 처리 계획

작성일: 2026-10-07. 상태: 0.2.0 구현. 제품 Manifest는 최소 동작 권한과 사이트별 선택 권한으로 변경했다.

후속 P1a 실험은 제품과 별도 Manifest를 사용한다. `debugger`와 루프백 host 권한만 요청하고 정확한 시험 URL·고정 관찰식·Input 명령으로 제한한다. 권한 capability, 메모리 관찰값 및 종료 방법은 [P1A_RUNBOOK](P1A_RUNBOOK.md)의 권한·중단 절차를 따른다. 교사 입력 내용·실제 수업 페이지는 실험 도구의 수집 대상이 아니다.

## 1. 현재와 후보 권한

| 항목 | 현재 상태 | 구현 시 판단 |
|---|---|---|
| storage | 제거 | 세션·교정값은 실행 중 메모리에만 둠 |
| activeTab | 선언됨 | 사용자 선택한 학생 탭을 대상으로 사용 |
| 모든 HTTP/HTTPS content_scripts matches | 제거 | 전체 사이트 자동 주입 없음 |
| nativeMessaging | 선언됨 | 등록한 Windows 호스트와만 통신 |
| debugger | 선언됨 | 선택된 학생 탭의 Input 명령만 전달 |
| scripting | 선언됨 | 사용자 허용한 사이트의 선택 탭에 키보드·학생 포인터 주입 |
| optional_host_permissions | http/https 후보 범위 선언 | 사이드바에 입력한 구체적 사이트 origin만 사용자 동작으로 요청 |
| tabs / webNavigation | 없음 | 민감한 탭 메타데이터·탐색 감시가 실제로 필요할 때만 검토. 대상 ID·이벤트만으로 가능한지 우선 확인 |

위 목록은 권한을 모두 넣으라는 지시가 아니다. 구현 단계별로 필요한 항목만 추가한다. `debugger`의 실제 capability는 앱 내부 명령 제한보다 크므로 선택한 탭·허용 명령으로 동작을 제한하고 사용자와 학교 관리자가 판단할 수 있게 한다. [Chrome debugger API](https://developer.chrome.com/docs/extensions/reference/api/debugger)

`action`과 `sidebar_action`을 함께 선언하지 않는다. MV3 및 패키지 내부 코드만 사용한다. [Whale Manifest](https://developers.whale.naver.com/api/extensions/manifest/)

## 2. 처리 데이터

| 데이터 | 처리 위치 | 보존 정책 |
|---|---|---|
| 선택 장치·디스플레이 설정, 교정값 | 실행 중 메모리 | 대상 재선택·호스트 종료 때 폐기 |
| HWND·tabId·windowId·세션·좌표 세대 | 실행 중 메모리 / 필요한 session storage | OFF·대상 변경·재시작 때 폐기, ON 상태 영구 보존 금지 |
| 학생 포인터 좌표·버튼 상태 | Native → Worker → 대상 페이지 | 동작 처리 후 폐기, 기본 로그 없음 |
| 학생 입력 문자열·조합 상태 | 페이지·확장앱 메모리 | 편집 처리에 필요한 동안만. 로그·Native 호스트·서버로 보내지 않음 |
| 오류 코드·지연·카운터 | 로컬 진단 | 기본은 메모리. 사용자가 요청한 진단만 파일 저장 |
| 교사 입력 내용 | 수집하지 않음 | 글로벌 키로거 없음 |
| 시험용 커서·foreground 이벤트 | 시험 도구 | 명시적 시험 중에만 관찰. 창 제목·문서 내용·개인 URL 제외 |

실명·학번·학교 계정·방문 기록·전체 DOM·카메라·마이크는 수집하지 않는다. 사용자가 원래 웹사이트의 제출 버튼을 누르면 해당 사이트의 기존 데이터 처리는 발생할 수 있으나 이 확장앱의 별도 서버 전송은 만들지 않는다.

## 3. Native 연결과 입력 검증

- 호스트 `allowed_origins`에는 실제 배포 확장앱 ID만 등록. 공개 와일드카드 허용 금지.
- Worker는 메시지 sender의 확장앱/탭/프레임/문서와 세션을 확인한다. 웹페이지 `postMessage`를 시스템 제어 명령으로 직접 연결하지 않는다.
- 명령 종류·메시지 크기·좌표·seq를 제한. 임의 CDP·파일 경로·셸 명령·전역 키 입력은 허용하지 않음.
- Native 표준 출력은 프로토콜 전용. 입력 문자를 포함하는 디버그 출력 금지.
- 지속 통신은 활성 독립 입력 세션에서만 사용. 연결 종료 때 입력 격리 해제.
- 연결 등록 manifest는 사용자 로컬 `WhaleDualInput/native-host.json`에 저장한다. 설치된 EXE 경로·허용 확장앱 ID만 포함하며 학생 데이터는 없다. 등록 해제는 해당 호스트 키만 삭제한다.
- 별도 Named Pipe 도입 시 해당 Windows 사용자에게 접근 제한. localhost 포트를 기본 배포에 열지 않음.

Native Messaging의 호스트 등록·프로토콜·권한은 Chrome 문서를 참고하되 Whale 등록 위치·정책은 실기기에서 확정한다. [Native Messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging)

## 4. 삭제와 진단

설정 초기화는 교정·장치 선호 설정을 삭제하고 OFF로 전환한다. 제거 도구는 프로젝트가 생성한 호스트 등록과 설정만 삭제한다. 다른 브라우저나 학교 정책을 수정하지 않는다.

진단 파일을 만들면 저장 경로와 삭제 버튼을 제공한다. 공개하지 않고 기본 자동 업로드도 하지 않는다. 시험 문자열과 익명화된 버전·오류·카운터만 기록한다. 실제 사용자 입력 문자열·페이지 본문·계정 URL은 진단 대상에서 제외한다.
