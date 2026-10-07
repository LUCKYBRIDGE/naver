/**
 * @file sidebar.js
 * @description 웨일 듀얼 인풋 사이드바 제어 로직
 */
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') {
    console.log('[웨일 듀얼 인풋] 사이드바 활성화');
  }
});
