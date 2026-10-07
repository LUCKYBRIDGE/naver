/**
 * @file package-project.mjs
 * @description 지정된 프로젝트의 02_제작_결과물을 공모전 제출용 ZIP 파일로 자동 압축
 * 사용법: node scripts/package-project.mjs <프로젝트폴더명>
 */

import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';

const [,, folderName] = process.argv;

if (!folderName) {
  console.error('\n❌ 사용법: node scripts/package-project.mjs <프로젝트폴더명>');
  process.exit(1);
}

const sourceDir = path.resolve('projects', folderName, '02_제작_결과물');
const outputDir = path.resolve('projects', folderName, '04_최종_배포_제출');
const zipFileName = `${folderName}_제작결과물.zip`;
const zipFilePath = path.join(outputDir, zipFileName);

if (!fs.existsSync(sourceDir)) {
  console.error(`\n❌ 오류: 소스코드 폴더를 찾을 수 없습니다 -> ${sourceDir}`);
  process.exit(1);
}

if (folderName === 'whale-dual-input' && fs.existsSync(path.join(sourceDir, 'tests'))) {
  console.error('실험 도구가 포함된 소스 전체를 제출용으로 압축할 수 없습니다. npm run build의 별도 extension 출력을 확인하고, P2/P5 배포 분리 후 제출하세요.');
  process.exit(1);
}

if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

if (fs.existsSync(zipFilePath)) {
  fs.unlinkSync(zipFilePath);
}

try {
  // Windows PowerShell Compress-Archive 명령어 활용 (외부 의존성 없음)
  const psCommand = `Compress-Archive -Path "${sourceDir}\\*" -DestinationPath "${zipFilePath}" -Force`;
  execSync(`powershell -NoProfile -Command "${psCommand}"`, { stdio: 'inherit' });
  console.log(`\n🎉 성공적으로 압축되었습니다!`);
  console.log(`📦 생성된 파일: ${zipFilePath}`);
  console.log(`👉 이 ZIP 파일을 공모전 [제출물 02: 제작 결과물]로 제출하시면 됩니다.\n`);
} catch (err) {
  console.error('\n❌ 압축 중 오류 발생:', err);
}
