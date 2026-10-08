# 개발 도구와 설치 가능성 — M0.4 준비

2026-10-08. 개발 PC의 관찰값과 학교 배포 조건을 구분한다.

## 이 개발 PC

| 항목 | 확인 결과 | 의미 |
|---|---|---|
| Git | main, a5f0d23dae251d2ccb57494001c3b40b73241b56 | 기존 소스 기준선. 준비 전 untracked 인계 폴더와 ARCHITECTURE_V5.md 보존 |
| Windows | .NET 정보의 10.0.26200 / win-x64 | 학교 시험 PC 값으로 일반화하지 않음 |
| Node / npm | v24.15.0 / 11.12.1 | 일반 검사에 추가 npm 설치 불필요 |
| 시스템 .NET | SDK 8.0.424 | 기존 net10.0-windows 대상에 부족 |
| 사용자 로컬 .NET | SDK 10.0.401 설치·실행 확인 | `%USERPROFILE%\.local\share\naver-dotnet\dotnet.exe`, Microsoft 서명 Valid 확인 |
| Whale | Program Files의 whale.exe 5.39.412.57 | 실행 파일 버전 확인. 브라우저 실행·최신 버전 여부·확장 호환 실험은 미수행 |
| Windows.Forms 화면 관측 | DISPLAY2 primary (0,0) 1920×1080 / DISPLAY3 (1920,0) 1920×1080 | 두 데스크톱 영역 관측. 표시 이름은 Windows 설정의 '2번'과 같다고 가정하지 않음 |
| 실제 전자칠판·USB 포인터 | 미확인 | 두 화면 존재가 터치 장치·매핑·G1 증거는 아님 |

Microsoft의 [dotnet-install 도구](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script)로 관리자 권한 없이 로컬 빌드 SDK를 준비했다. 시스템 SDK·영구 PATH·레지스트리를 변경하지 않았다. 이번 설치는 개발용이고 제품은 기존 자체 포함 EXE 방식이다. SDK는 저장소에 포함하지 않는다.

`build-windows.mjs`는 `DOTNET_EXE` 지정값을 먼저 사용하고, 로컬 SDK가 있으면 OS별 dotnet/dotnet.exe를 사용한 뒤 시스템 dotnet으로 대체한다. 다른 PC에서 사용자 로컬 SDK를 쓰려면 같은 상대 사용자 경로에 준비하거나 다음처럼 해당 PowerShell 세션에서 경로를 지정한다.

```powershell
$env:DOTNET_EXE = 'C:\개발도구\dotnet\dotnet.exe'
npm run build:windows
```

지정 경로는 예시다. .NET 10의 설치된 실제 실행 파일을 사용한다. 로컬 SDK는 개발 도구 위치를 점유하므로 기존 내용을 확인한 뒤 관리하며, 프로젝트/사용자 홈 전체를 삭제하지 않는다.

## 실제 설치/배포 여건

| 경로 | 지금 판정 | 미해결 조건 |
|---|---|---|
| 기존 v0.2.0 Native Messaging Host | 빌드 후보, 입력 보호 미완성 | Whale 등록 키 실측·allowed_origins·설치/제거·중복 실행·학교 정책 |
| Tier A | 연구 후보, 제품 배포 미승인 | UIAccess 목적·서명·보호 위치·세션·다른 터치 장치·G1 |
| Tier B | 조건부 조사, 본 개발/설치 미착수 | 서명·관리자 설치·HVCI·장치별 부착·롤백·교사 복구 |
| 일반 학교 배포 | 준비 미완료 | 지원 기기에서 G1/G2·운영·성능·조직 허용 조건 |

Microsoft의 현행 문서는 attestation/WHCP 제출 계정에 EV 인증서 연결이 필요함을 명시한다. 계정·인증서 확보 여부는 미확인이다. [Driver code signing requirements](https://learn.microsoft.com/en-us/windows-hardware/drivers/dashboard/code-signing-reqs)

HVCI 호환성은 서명만으로 증명되지 않는다. 코드 무결성 검사와 보호가 켜진 시험 PC의 기능 검증이 필요하다. [Driver compatibility with memory integrity](https://learn.microsoft.com/en-us/windows-hardware/test/hlk/testref/driver-compatibility-with-device-guard)

이번 준비는 Native 호스트 등록·드라이버 설치·UIAccess 실행·디스플레이 설정 변경을 수행하지 않는다. 제품 설치 안내는 아직 [기존 v0.2.0 안내](INSTALL_WINDOWS.md)이며 새 자동 매핑 제품 안내로 해석하지 않는다.

## 게이트를 끝내기 위해 필요한 현장 정보

- 전자칠판 모델·외부 화면/USB 연결·Windows의 실제 포인터 타입·장치 매핑.
- 교사 PC의 다른 터치/펜 장치, 실제 Windows/Whale 버전·학교 실행 정책.
- 코드 서명·설치 권한·복구 수단과 UIAccess 목적 판단.
- 최초 Native 등록 및 실제 확장 ID 제한·재연결 결과.

현재 확보하지 못한 정보가 관찰 도구 구현의 시작을 막지는 않는다. 보호 활성화·드라이버 설치·제품 배포 결정은 해당 게이트의 증거를 확보한 뒤 수행한다.
