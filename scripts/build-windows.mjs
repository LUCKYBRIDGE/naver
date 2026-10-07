import './build-p1a.mjs';
import { existsSync } from 'node:fs';
import { mkdir, readdir, copyFile, writeFile } from 'node:fs/promises';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { homedir } from 'node:os';
import { spawnSync } from 'node:child_process';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const localSdk = join(homedir(), '.local/share/naver-dotnet/dotnet');
const dotnet = process.env.DOTNET_EXE || (existsSync(localSdk) ? localSdk : 'dotnet');
const cache = process.platform === 'win32' ? join(process.env.LOCALAPPDATA || homedir(), 'WhaleDualInputBuild') : join(homedir(), 'Library/Caches/naver-native');
const output = resolve(root, 'dist/whale-dual-input/windows');
const project = resolve(root, 'projects/whale-dual-input/02_제작_결과물/native/WhaleDualInput.Host/WhaleDualInput.Host.csproj');
const native = spawnSync(dotnet, ['publish', project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
  '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true',
  `-p:BaseIntermediateOutputPath=${join(cache, 'obj')}/`, `-p:BaseOutputPath=${join(cache, 'bin')}/`, '-o', output, '--nologo'],
  { stdio: 'inherit', env: { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1' } });
if (native.error) throw native.error;
if (native.status !== 0) process.exit(native.status ?? 1);

const stamp = new Date().toISOString().replace(/[-:]/g, '').replace(/\..*/, '').replace('T', '-');
const name = `whale-dual-input-0.2.0-${stamp}`;
const release = resolve(root, 'projects/whale-dual-input/04_최종_배포_제출', name);
async function copyTree(source, destination) {
  await mkdir(destination, { recursive: true });
  for (const entry of await readdir(source, { withFileTypes: true })) {
    if (entry.name.startsWith('._') || entry.name === '.DS_Store' || entry.name.endsWith('.pdb')) continue;
    if (entry.isSymbolicLink()) throw new Error('Symlinks cannot be packaged');
    if (entry.isDirectory()) await copyTree(join(source, entry.name), join(destination, entry.name));
    else await copyFile(join(source, entry.name), join(destination, entry.name));
  }
}
await copyTree(resolve(root, 'dist/whale-dual-input/extension'), join(release, 'extension'));
await copyTree(output, join(release, 'windows'));
await copyFile(resolve(root, 'projects/whale-dual-input/01_기획_및_지침/docs/INSTALL_WINDOWS.md'), join(release, 'INSTALL_WINDOWS.md'));
await copyFile(resolve(root, 'projects/whale-dual-input/01_기획_및_지침/docs/INPUT_ENGINE_DECISION.md'), join(release, 'INPUT_ENGINE_DECISION.md'));
await writeFile(join(release, 'README.txt'), 'Whale Dual Input 0.2.0\r\nWindows 실행 파일과 웨일 확장앱입니다. INSTALL_WINDOWS.md 순서대로 설치하세요.\r\n구현 패키지이며 실제 Windows·전자칠판 동작 검증은 아직 수행하지 않았습니다.\r\n', 'utf8');
const archive = release + '.zip';
const zipped = process.platform === 'win32' ? spawnSync('powershell.exe', ['-NoProfile', '-Command',
  'Compress-Archive -Path (Join-Path $env:WDI_SOURCE "*") -DestinationPath $env:WDI_ZIP'],
  { stdio: 'inherit', env: { ...process.env, WDI_SOURCE: release, WDI_ZIP: archive } }) :
  spawnSync('zip', ['-q', '-r', archive, '.'], { cwd: release, stdio: 'inherit' });
if (zipped.error) throw zipped.error;
if (zipped.status !== 0) process.exit(zipped.status ?? 1);
console.log(`Windows package: ${archive}`);
