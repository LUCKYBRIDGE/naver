/**
 * @file service-worker.js
 * @description 웨일 듀얼 인풋 백그라운드 서비스 워커 (MV3)
 */
import './session.js';
const api = globalThis.whale ?? globalThis.chrome;
api.runtime.onInstalled.addListener(() => {
  console.log('[웨일 듀얼 인풋] 서비스 워커 설치 완료');
});
