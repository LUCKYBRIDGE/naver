> 2026-10-08 최신 기술 방향은 [ARCHITECTURE_V7_3_MONITOR2](ARCHITECTURE_V7_3_MONITOR2.md), 현재 개발 PC는 [INSTALLATION_FEASIBILITY](INSTALLATION_FEASIBILITY.md)를 따른다. 아래 macOS 접근 한계는 당시 이력이다. 기존 오버레이는 새 원본 격리 엔진으로 채택하지 않는다.

# 독립 입력 엔진의 현재 판단

2026-10-07. 상태: 핵심 입력 분리 미완성. 0.2.0 ZIP은 통합 코드 빌드 산출물이며 완성본이 아니다.

## 구현된 연결

사이트별 권한 요청, 대상 탭·문서 고정, Native Messaging, Windows 제어창, viewport 수동 교정, CDP 클릭·드래그·스크롤, 학생 한/영 키보드·한글 조합, OFF/F9·연결/세션 종료를 연결했다. `.NET 10` win-x64 자체 포함 EXE 생성이 통과했다. 이 사실은 OS 입력 격리가 완성됐다는 증거가 아니다.

## 완성을 막는 핵심 사항

1. 현재 별도 수신 창의 `HTTRANSPARENT` 반환은 교사 물리 마우스의 Whale 프로세스 통과를 보장하지 않는다. Microsoft 문서는 같은 스레드의 아래 창으로 전달되는 동작을 설명한다. 별도 프로세스의 웨일 위에 수신 창을 놓는 구조에서 이 값을 완전한 통과 방식으로 취급할 수 없다. [WM_NCHITTEST](https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-nchittest)
2. 터치 합성 마우스 서명을 hit-test 시점에 읽는 것만으로 원래 pointer/touch 경로를 모두 구별한다고 가정할 수 없다. 특정 USB 전자칠판 장치 식별과 동일하지도 않다.
3. `CURSOR_SUPPRESSED`를 감지해 OFF로 전환하는 것은 실패 시 해제 처리다. 억제가 처음부터 발생하지 않았다는 R2의 성공 조건이 아니다. [CURSORINFO](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-cursorinfo)
4. Whale NativeMessagingHosts 레지스트리 위치는 현재 설치창의 초기 후보일 뿐, 실제 웨일에서 확정하지 않았다.

## 이어서 해야 할 일

목표는 사용자 요구대로 실제 사용 가능한 본 프로그램을 완성한 뒤 검사하는 것이다. 위 문제를 실험용 UI나 성공 표시로 감추지 않는다. Windows 실행 환경에서 실제 입력 전달 경로와 등록 위치를 확인해 기본 구현을 확정하고 고친다. 사용자 모드에서 요구를 충족하지 못한다면 필요한 장치 수준 제어의 범위·설치 제약을 명시한다. 드라이버나 시스템 설정 변경을 몰래 추가하지 않는다.

현재 이 맥에서 Windows에 접속할 경로는 확보되지 않았다. 연결된 원격 도구에는 Windows PC가 등록되어 있지 않았다. 사용자는 Windows PC와 전자칠판을 보유한다고 확인했다. 접근 방법을 요청한 상태다.
