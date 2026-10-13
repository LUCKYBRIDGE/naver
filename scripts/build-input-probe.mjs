import { existsSync } from 'node:fs';
import { mkdir, copyFile } from 'node:fs/promises';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { homedir } from 'node:os';
import { spawnSync } from 'node:child_process';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const localSdk = join(homedir(), '.local/share/naver-dotnet', process.platform === 'win32' ? 'dotnet.exe' : 'dotnet');
const dotnet = process.env.DOTNET_EXE || (existsSync(localSdk) ? localSdk : 'dotnet');
const cache = join(process.env.LOCALAPPDATA || homedir(), 'WhaleDualInputBuild', 'probe');
const output = resolve(root, 'dist/whale-dual-input/input-probe');
const project = resolve(root, 'projects/whale-dual-input/02_제작_결과물/native/WhaleDualInput.Probe/WhaleDualInput.Probe.csproj');
const result = spawnSync(dotnet, ['publish', project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
  '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
  `-p:BaseIntermediateOutputPath=${join(cache, 'obj')}/`, `-p:BaseOutputPath=${join(cache, 'bin')}/`, '-o', output, '--nologo'],
{ stdio: 'inherit', env: { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1' } });
if (result.error) throw result.error;
if (result.status !== 0) process.exit(result.status ?? 1);
await mkdir(output, { recursive: true });
await copyFile(resolve(root, 'projects/whale-dual-input/01_기획_및_지침/docs/INPUT_PROBE_RUNBOOK.md'), join(output, 'INPUT_PROBE_RUNBOOK.md'));
console.log(`Research tool only: ${output} (not included in production extension or v0.2.0 ZIP)`);
