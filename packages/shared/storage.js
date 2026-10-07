/**
 * @file storage.js
 * @description Local-first 스토리지 유틸리티
 * 학생 개인정보 보호를 위해 외부 서버를 거치지 않고 브라우저 로컬 저장소(whale/chrome.storage.local)만 사용합니다.
 */

function getStorage() {
  if (typeof chrome !== 'undefined' && chrome.storage && chrome.storage.local) {
    return chrome.storage.local;
  }
  throw new Error('브라우저 storage API를 사용할 수 없습니다.');
}

/**
 * 단일 키 또는 다중 키로 스토리지 데이터를 조회합니다.
 * @param {string|string[]} keys
 * @returns {Promise<any>}
 */
export async function getStorageData(keys) {
  const storage = getStorage();
  return new Promise((resolve, reject) => {
    storage.get(keys, (result) => {
      if (chrome.runtime.lastError) {
        reject(chrome.runtime.lastError);
      } else {
        resolve(result);
      }
    });
  });
}

/**
 * 스토리지에 데이터를 저장합니다.
 * @param {Record<string, any>} data
 * @returns {Promise<void>}
 */
export async function setStorageData(data) {
  const storage = getStorage();
  return new Promise((resolve, reject) => {
    storage.set(data, () => {
      if (chrome.runtime.lastError) {
        reject(chrome.runtime.lastError);
      } else {
        resolve();
      }
    });
  });
}
