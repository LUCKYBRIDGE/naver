# GitHub 저장소 통합 지침 — 기존 소스 보존

> 검토한 공개 저장소: `LUCKYBRIDGE/naver` (2026-10-08). 이 문서는 실제 커밋을 수행했다는 뜻이 아님.

## 1. 확인한 기존 구조

- 루트 `AGENTS.md`: MV3, 가벼운 교육용 확장, 확장 도구의 실제 기여, `action`/`sidebar_action` 동시 선언 금지, 개발/실측 구분.
- `projects/whale-dual-input/02_제작_결과물/manifest.json`: **MV3, `sidebar_action`, `sidebar/sidebar.html`, `background/service-worker.js`, `nativeMessaging`, `debugger`**.
- `projects/whale-dual-input/01_기획_및_지침/docs/HANDOFF.md`: 기존 사이트/수동 viewport 지정 흐름과 ON 직후 OFF 문제 기록. 이 결정들은 최신 v7.3 및 실행계획으로 갱신해야 하지만 **과거 실패 기록은 지우지 말 것**.
- v0.2.0 Windows Native Host, CDP/한글 입력 코드는 **재사용 후보**이지 교사 입력 격리 성공 사례가 아니다.

## 2. 안전한 적용 위치

```text
LUCKYBRIDGE/naver/
├─ AGENTS.md                                 # 기존 보존 + 필요한 지침만 증분 반영
└─ projects/whale-dual-input/
   ├─ 01_기획_및_지침/docs/
   │  ├─ ARCHITECTURE_V7_3_MONITOR2.md       # 05_references 원문 복사
   │  ├─ DEVELOPMENT_PLAN_V1_WHALE_MONITOR2.md
   │  ├─ UI_ON_OFF_SPEC_V1_1.md              # 02_ui/ON_OFF_UI_SPEC.md
   │  ├─ design.md                           # 02_ui/design.md
   │  ├─ IMPLEMENTATION_TASKS_V1_1.md        # 03_implementation/IMPLEMENTATION_TASKS.md
   │  ├─ ACCEPTANCE_TESTS_V1_1.md            # 04_quality/ACCEPTANCE_TESTS.md
   │  └─ HANDOFF.md                          # 기존 문서 끝에 최신 문서 안내만 추가
   ├─ 02_제작_결과물/
   │  ├─ manifest.json                       # sidebar_action 유지, 임의 action 병설 금지
   │  ├─ sidebar/sidebar.html                # UI 새 명세로 점진 수정
   │  ├─ background/service-worker.js        # MV3 재시작/Native 상태 재동기화
   │  ├─ background/session.js               # 기존 CDP 입력 재사용 후보
   │  └─ content/                            # 기존 한글 입력/웹 콘텐츠 도구 점검
   └─ windows/                               # 장치 프로브/격리 모듈은 실제 검증 후 단계 구현
```

이 트리는 **목표 통합 예시**다. 실제 디렉터리와 빌드 스크립트를 확인한 뒤 배치한다. 루트 `AGENTS.md` 및 다른 프로젝트의 파일/경로는 함부로 바꾸지 않는다.

## 3. 구현 순서에 관한 통합 규칙

1. 작업 시작 전 `git status`, 브랜치, 파일 목록과 빌드 기준선을 기록한다. 원본 v0.2.0의 오류 동작도 변경 전 기록.
2. 우선 문서만 추가하는 커밋과 구현 커밋을 분리한다.
3. **UI는 상태 머신으로 연결**하고, 초기에는 `OFF`로 보이더라도 실제 Native 질의 중임을 설명한다. 저장소의 마지막 상태로 `ACTIVE`를 임의 복원하지 않는다.
4. 기존 `sidebar_action`을 유지한다. 확장앱 팝업으로 바꾸려면 사용자 요구 및 Whale manifest 호환성을 검토하고 별도 ADR 승인 필요.
5. 기존 웹페이지 '선택·드래그 보정'을 새 UI에서 제거한다. 후방 코드는 재사용 시험 완료 후 단계적으로 정리한다. 보호에 필요한 기능을 무작정 삭제하지 않는다.
6. 디버거 연결 안내줄로 viewport가 바뀌어도 자동 갱신해야 한다. 기존 `resize => OFF` 동작을 정상 설계로 채택하지 않는다.
7. 디버깅 중에도 Windows 전역 `SendInput`, `SetCursorPos`, `SetForegroundWindow`, 좌표 기준 `WH_MOUSE_LL` 차단을 교사 무간섭의 우회 해결책으로 도입하지 않는다.
8. `npm test`, `npm run build`, `npm run build:windows`를 기존 프로젝트 기준으로 확인하되 **하드웨어 G1 검증과 구분**한다.
9. 성공 가능성을 증명하지 못한 보호 경로는 `EXPERIMENTAL`/`UNSUPPORTED`로 표기, `ACTIVE` 가짜 활성화 금지.

## 4. UI↔Native 상태 데이터 계약(개발 제안)

확장앱에 전달하는 **읽기 전용 상태 스냅샷 예시**. 구현자가 버전/필드를 확정하고 보안 경계를 검증한다.

```json
{
  "schemaVersion": 1,
  "requestId": "local-generated-id",
  "observedAtMonotonicMs": 12345,
  "state": "OFF",
  "reasonCode": null,
  "display": { "connected": true, "targetLabel": "모니터 2", "mappingVerified": false },
  "touch": { "discovered": false, "isolated": false, "isolationEvidence": "NONE" },
  "whale": { "tabConnected": false, "adapterReady": false },
  "capability": { "hardwareVerified": false, "tier": "NONE" }
}
```

- `requestId`: 중복 클릭/늦게 온 응답 식별. 스냅샷 문자열만으로 ACTIVE를 판단하지 않음.
- `observedAtMonotonicMs`: 특정 프로세스의 시간 기준이므로 UI 시간과 단순 비교 금지. TTL/소유 프로세스/재연결 epoch로 유효성 정의.
- `state=ACTIVE`의 발급 권한은 **Native 보호 상태 관리자**에 있고, 확장앱은 신뢰 가능한 실시간 확인 결과를 보여줄 뿐이다.
- `isolationEvidence=NONE`이면 상태를 ACTIVE로 표시하지 않는다.
- Native 명령: `GET_STATUS`, `START`, `STOP`, `PAUSE`, `RESUME`, `RECHECK` — 요청 ID 및 ACK/ERR 필수.
- 브리지 연결이 끊기면 UI는 마지막 ACTIVE를 확정 상태로 유지하지 말고 `연결 확인 중`을 표시한 뒤 Native로 재검증한다. 보호 상태 불명은 `SAFE_HOLD`로 단정 금지.
- `LOST_PROTECTION`에서도 OFF를 누를 수 있어야 하지만 보호가 없는 사실을 숨기지 않는다.

## 5. 개발 산출물 기록

`CODE_REUSE_AUDIT.md`에 각 현재 파일별 `현재 역할 / 유지 / 변경 / 제거 후보 / 이유 / 테스트` 열을 만들고 채운다. `HANDOFF.md`에는 현재 최신 계획 파일 링크와 단계별 실측 결과만 추가한다.

**이번 요청은 저장소 변경을 승인한 것이 아니다.** 이 문서를 첨부해 개발자가 별도 작업 트리에서 변경 후 리뷰/테스트/커밋하게 한다.
