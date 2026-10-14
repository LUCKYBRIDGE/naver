# Native 보호 상태 관리자 구현 계약

2026-10-14. 코드: `02_제작_결과물/native/WhaleDualInput.Core/ProtectionStateManager.cs`.
상태: 순수 Native 상태 코어 구현·결함 주입 검사 완료. **물리 입력 격리 엔진·Bridge·제품 Worker에는 아직 연결하지 않았다.** 연구 도구의 관찰/차단 후보 상태와 제품 보호 상태는 별개다.

후속 구현: [PointerIsolationExperiment](ISOLATION_A_IMPLEMENTATION.md)가 등록 결과를 EXPERIMENTAL로, 해제 응답을 NONE으로 상태 코어에 전달한다. 따라서 제품 ACTIVE를 발급하지 않는다. Bridge·제품 Worker 통합은 계속 후속이다. 아래 계약은 유지한다.

## 상태 발급 조건

- START/RESUME/RECHECK는 DISCOVERING을 거쳐 실제 확인 데이터를 받는다. 매핑 완료 체크포인트는 PREPARING이다.
- ACTIVE에는 원본 격리 생존, VERIFIED 증거, 하드웨어 검증, 장치 매핑, 어댑터 준비, 학생 채널 준비가 모두 필요하다. OBSERVATION_ONLY/EXPERIMENTAL/빌드/CDP 성공은 대체 증거가 아니다.
- 격리 생존·하드웨어 증거는 유효하지만 전달·매핑·채널이 준비되지 않으면 SAFE_HOLD로 전달을 중단한다. 격리 소멸이 확인되면 LOST_PROTECTION이다.
- 만료/연결 불명은 StatusKnown=false, 표시 상태 PREPARING 및 별도 사유로 반환한다. 과거 ACTIVE/SAFE_HOLD를 현재 상태처럼 표시하지 않는다. 연결 불명 자체를 보호 소멸로 단정하지 않는다.
- STOP/PAUSE는 ReleasePending=true로 실제 해제를 기다린다. 생존 중이라는 응답은 해제 ACK로 인정하지 않는다. 확인된 해제 뒤 OFF/PAUSED를 발급한다.
- STOP은 진행 중 START를 무효화한다. 늦은 응답·다른 epoch의 관측은 거부한다. 해제 timeout 후 RECHECK로 생존을 확인해도 ACTIVE로 돌아가지 않고 실제 해제 확인을 기다린다.

## 통신 및 호출 경계

Operation은 RequestId + Epoch + Command를 가진다. 변경 명령 START/STOP/PAUSE/RESUME/RECHECK와 읽기 Snapshot을 제공한다. GET_STATUS 메시지를 받는 Bridge는 Snapshot 호출로 응답할 예정이다. 실제 JSON 명령 파서/IPC 인증·ACK 전송은 후속이다.

Snapshot에는 schemaVersion, instanceId, revision, epoch, state, statusKnown, reasonCode, releasePending, requestId, isolationEvidence, isolationEpoch가 있다. instanceId는 새 상태 관리자마다 달라 오래된 프로세스 결과와 구분한다. enum JSON 직렬화는 Bridge 구현 때 문자열 변환 규칙을 확정한다.

모든 메서드는 lock으로 상태 읽기/변경을 직렬화한다. 비동기 API/차단 엔진 호출을 lock 안에서 실행하지 않는다. 호출자는 backend 작업을 직렬화하고 Operation을 응답과 함께 돌려준다. 웹페이지/사이드바의 전달값을 VERIFIED 증거로 그대로 넣지 않는다. 허용된 Native 격리 backend만 증거를 발급하도록 Bridge 경계를 구현해야 한다.

requestId 중복은 최근 256개를 차단한다. 저장소·원격 서버를 추가하지 않는다. 세대(epoch) 검사는 캐시에서 오래된 ID가 빠져도 과거 응답 재생을 막는다. 현재 구현은 중복 요청에 예외를 반환하며, 브리지에서 기존 ACK를 재전송하는 idempotent 응답 캐시는 후속이다.

TTL과 명령 timeout은 생성자 매개변수이며 생산값을 확정하지 않았다. 검사에서는 100ms/200ms를 합성 시계로 사용한다. 관측 시각은 관리자 자신의 단조 시계에서 기록한다. Native 다른 프로세스/브라우저의 시계 숫자를 직접 비교하지 않는다. 생산 poll/heartbeat·취소·보호 backend와의 시간 계약은 실제 격리 경로를 결정한 뒤 확정한다.

## 현재 회귀 검사

`npm run build:input-probe` 후 연구 EXE의 `--self-test`에서 기존 검사 40개와 상태 결함 검사 30개를 실행한다. 실제 차단은 실행하지 않는다.

검사 대상: 실험/하드웨어 미확인 거부, 매핑/학생 채널/어댑터 조건, stale 응답, 중복 요청, 진행 중 STOP, TTL 만료, disconnect, 격리 epoch 불일치, LOST→ACTIVE 직접 회복 거부, PAUSE/RESUME 재검사, STOP timeout·해제 재확인, 시계 역행, 관리자 인스턴스 분리.

## 다음 연결 작업

1. 실제 장치 구성과 원본 격리 backend를 선정한다. 관찰 훅과 포인터 그림은 격리 인증자가 아니다.
2. backend의 실제 등록/해제/소유권/생존 증거를 상태 코어에 연결한다. 프로브에서 차단 실행을 강행하지 않는다.
3. Bridge 명령/ACK·읽기 상태를 구현하고 Worker가 이 snapshot을 재조회하도록 변경한다. 기존 overlay active 응답으로 ACTIVE를 발급하는 v0.2.0 흐름을 새 계약과 섞지 않는다.
4. 상태 전환에서 StudentSeat 접촉·큐 취소와 navigation/geometry epoch를 연동한다.
5. 실제 Whale·전자칠판에서 G1/G2·OFF·비상 해제·보호 상실 경고를 검증한다.

현재 G1/G2 NOT TESTED. 이 모듈은 입력을 차단하거나 커서를 제한하지 않으며 실행만으로 교사 보호가 생기지 않는다.
