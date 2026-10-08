# Tier A UIAccess 사전 판단 — M0.4

2026-10-08. **준비 판정: 연구 후보 / 제품 배포 미승인 / 실행 자격 미확인.** 이는 API 구현 불가능 판정이나 법적 결론이 아니다. 공식 문서를 확인했지만 인증서·실행 토큰·학교 정책·실기기는 검증하지 않았다.

## 공식 근거

- Microsoft는 UIAccess를 접근성 보조 기술의 특별 권한으로 설명하고, 관련 접근성 목적·Authenticode 서명·보호된 설치 위치·앱 manifest를 요구한다. 비보조 기술 앱에서의 사용은 피하도록 명시한다. [Security Considerations for Assistive Technologies](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-securityoverview)
- `RegisterPointerInputTarget`은 자격 있는 창으로 포인터 **타입 전체**를 리디렉션한다. PT_MOUSE/PT_POINTER를 등록할 수 없으며, 한 데스크톱·타입당 창 하나다. 창 해제/파괴 때 등록은 유지되지 않는다. message-only/WS_EX_NOACTIVATE는 활성화 위험을 줄이는 조건이다. [RegisterPointerInputTarget](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerpointerinputtarget)

공식 문서 존재와 우리 PC의 원본 입력 격리 성공은 별개의 증거다. ERROR_ACCESS_DENIED는 자격·스레드 소유·타입 등록 충돌 등 원인이 있으므로 충돌로 단정하지 않는다.

## 현재 제품에 대한 판단

현재 요구는 일반적인 교사·학생 동시 입력 분리다. 제공된 계획만으로 Microsoft가 설명하는 접근성 시나리오에 해당함을 입증할 수 없다. 따라서 UIAccess를 제품 기본 경로로 확정하지 않는다. 정책 조사·자격을 충족한 통제 시험 환경에서의 연구 가능성을 남긴다. 단순 관리자 실행이나 csproj 수정은 UIAccess 자격·학교 배포 승인·G1 성공의 대체 증거가 아니다.

| 확인해야 할 항목 | 현재 | 다음 증거 |
|---|---|---|
| 구체적 접근성 시나리오와 권한 필요성 | 미확인 | 대상 사용자·기능·필요 권한의 서면 근거 |
| 조직/학교 사용 정책 적합성 | 미확인 | 관리자의 설치·운영 조건 |
| 신뢰 가능한 코드 서명·보호 위치 설치 | 미확인 | 서명 검증·인증서 신뢰 체인·설치 결과 |
| 사용자 세션의 실제 UIAccess 토큰 | 미확인 | 실행 프로세스 자격 검사 |
| 타입 등록·장치 출처·마우스 호환 입력 누출 | 미실행 | M0.5/M2a 실제 장치 로그 |
| 다른 터치 장치·등록 충돌·잠금/강제 종료 | 미실행 | 미지원 판정·보호 공백 기록 |

## 결정 갱신

M0.4의 두 축(기술 실행 조건 / 목적·정책)이 충족되면 `제품 후보`로 변경할 수 있으나, G1 실기기 합격은 별도다. 적합성을 확보하지 못하면 연구 전용 또는 제외로 기록하고 Tier B 여건을 조사한다. 서명·안전 설치·운영이 모두 불가능하면 NO-GO를 남긴다. 보안 정책을 끄거나 사후 커서 복원으로 우회하지 않는다.
