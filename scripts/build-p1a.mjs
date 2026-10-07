import { readFile, readdir, mkdir, copyFile } from 'node:fs/promises';
import { resolve, dirname, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const source = resolve(root, 'projects/whale-dual-input/02_제작_결과물');
async function files(dir) {
  const output = [];
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('._') || entry.name === '.DS_Store') continue;
    if (entry.isSymbolicLink()) throw new Error(`Symlink is not allowed: ${entry.name}`);
    const name = resolve(dir, entry.name);
    if (entry.isDirectory()) output.push(...await files(name));
    else output.push(name);
  }
  return output;
}
async function validateManifest(dir) {
  const manifest = JSON.parse(await readFile(resolve(dir, 'manifest.json'), 'utf8'));
  if (manifest.manifest_version !== 3 || (manifest.action && manifest.sidebar_action)) {
    throw new Error('Invalid MV3/action manifest');
  }
  const resources = [manifest.background?.service_worker, manifest.sidebar_action?.default_page,
    ...Object.values(manifest.icons ?? {}), ...Object.values(manifest.sidebar_action?.default_icon ?? {}),
    ...(manifest.content_scripts ?? []).flatMap(item => item.js ?? [])].filter(Boolean);
  for (const resource of resources) {
    const path = resolve(dir, resource);
    if (!path.startsWith(dir + sep)) throw new Error('Manifest path escapes extension');
    await readFile(path);
  }
}
for (const file of [...await files(source), ...await files(resolve(root, 'scripts'))]) {
  if (!/\.(js|mjs)$/.test(file)) continue;
  const result = spawnSync(process.execPath, ['--check', file], { encoding: 'utf8' });
  if (result.status !== 0) throw new Error(result.stderr || `Syntax failed: ${file}`);
}
const probe = resolve(source, 'tests/p1a/probe');
await validateManifest(source);
await validateManifest(probe);
const output = resolve(root, 'dist/whale-dual-input');
const productionFiles = ['manifest.json', ...(await files(source)).filter(file =>
  ['background', 'content', 'sidebar', 'icons'].some(dir => file.startsWith(resolve(source, dir) + sep))
).map(file => file.slice(source.length + 1))];
const probeFiles = (await files(probe)).map(file => file.slice(probe.length + 1));
// Overwrite only known build outputs. Never recursively delete the destination.
for (const [directory, names, input] of [['extension', productionFiles, source], ['p1a-probe', probeFiles, probe]]) {
  for (const name of names) {
    const destination = resolve(output, directory, name);
    await mkdir(dirname(destination), { recursive: true });
    await copyFile(resolve(input, name), destination);
  }
}
console.log('Build OK: dist/whale-dual-input/extension and p1a-probe (separate MV3 packages)');
