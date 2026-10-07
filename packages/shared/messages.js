/**
 * @file messages.js
 * @description 사이드바, 백그라운드, 컨텐츠 스크립트 간 메시지 통신 유틸리티
 */

/**
 * 현재 활성화된 탭으로 메시지를 전송합니다.
 * @param {string} action
 * @param {any} [payload]
 * @returns {Promise<{success: boolean, data?: any, error?: string}>}
 */
export async function sendToActiveTab(action, payload = null) {
  try {
    const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tabs || tabs.length === 0 || !tabs[0].id) {
      return { success: false, error: '활성 탭을 찾을 수 없습니다.' };
    }

    const tabId = tabs[0].id;
    return new Promise((resolve) => {
      chrome.tabs.sendMessage(tabId, { action, payload }, (response) => {
        if (chrome.runtime.lastError) {
          resolve({ success: false, error: chrome.runtime.lastError.message });
        } else {
          resolve(response || { success: true });
        }
      });
    });
  } catch (err) {
    return { success: false, error: err.message || String(err) };
  }
}

/**
 * 백그라운드 Service Worker로 메시지를 전송합니다.
 * @param {string} action
 * @param {any} [payload]
 * @returns {Promise<{success: boolean, data?: any, error?: string}>}
 */
export async function sendToBackground(action, payload = null) {
  try {
    return new Promise((resolve) => {
      chrome.runtime.sendMessage({ action, payload }, (response) => {
        if (chrome.runtime.lastError) {
          resolve({ success: false, error: chrome.runtime.lastError.message });
        } else {
          resolve(response || { success: true });
        }
      });
    });
  } catch (err) {
    return { success: false, error: err.message || String(err) };
  }
}
