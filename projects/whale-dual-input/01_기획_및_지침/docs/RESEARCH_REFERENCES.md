# 개발 준비에서 확인한 공식 자료

열람일: 2026-10-08. 목적: v1.1의 API·설치 경계 확인. 외부 제품 소스·프로토콜·바이너리를 복사하거나 의존성에 포함하지 않았다. 아래 문서 확인은 실기기 성공의 증거가 아니다.

| 출처 | 확인 범위 | 남은 확인 |
|---|---|---|
| [Whale Manifest](https://developers.whale.naver.com/api/extensions/manifest/) | MV3·Service Worker·action/sidebar_action 배타 | 실제 설치/경고 |
| [Whale 확장 API](https://developers.whale.naver.com/api/extensions/) | whale.debugger·runtime·scripting 문서 및 Chrome 참조 연결 | 설치 Whale의 명령·Native 경로·정책 |
| [Whale MV3 전환](https://developers.whale.naver.com/tutorials/mv3-migration/) | MV3 지침 존재 확인 | Worker 재시작 복구 |
| [RegisterPointerInputTarget](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerpointerinputtarget) | 타입 리디렉션·자격·등록/해제 범위 | 우리 기기의 원본 격리·호환 이벤트·보호 공백 |
| [UIAccess 보안](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-securityoverview) | 접근성 목적·서명·보호 위치·manifest | 우리 제품 목적과 학교 조건 |
| [드라이버 서명](https://learn.microsoft.com/en-us/windows-hardware/drivers/dashboard/code-signing-reqs) | 제출·계정 인증서 요구 | 실제 계정·서명 가능성 |
| [HVCI 호환성](https://learn.microsoft.com/en-us/windows-hardware/test/hlk/testref/driver-compatibility-with-device-guard) | 보호 켜진 기능/코드 무결성 시험 필요 | 드라이버를 채택할 때 실측 |
| [Chrome Native Messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging) | 호스트·stdio·허용 origin 계약 참조 | Whale의 실제 등록 위치 |
| [Chrome debugger](https://developer.chrome.com/docs/extensions/reference/api/debugger) | CDP 연결 API 참조 | 실제 Whale M4a |
| [CDP Input](https://chromedevtools.github.io/devtools-protocol/tot/Input/), [Emulation](https://chromedevtools.github.io/devtools-protocol/tot/Emulation/) | 공식 도메인 참조 위치 | 설치 Whale 프로토콜 버전·지원 명령 |
| [dotnet-install](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script) | 로컬 SDK·NoPath 설치 | 다른 기기에서 별도 도구 준비 |

MouseMux 자료는 제공된 설계서의 참고 이력만 읽었다. 이번 세션에서 MouseMux 소스나 Chromium 패치를 열람/채택하지 않았다. 원문의 권리·구현 경계 정책을 유지한다. 실제 신규 외부 소스·라이브러리 채택은 없으므로 제3자 고지를 새로 만들지 않았다. 향후 채택할 때 자산별 라이선스·저작권·배포 조건을 기록한다.
