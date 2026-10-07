import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';

function createPng(width, height, colorRgb) {
  const [r, g, b] = colorRgb;
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

  function chunk(type, data) {
    const len = Buffer.alloc(4);
    len.writeUInt32BE(data.length, 0);
    const typeBuf = Buffer.from(type);
    const crcBuf = Buffer.alloc(4);
    const crc = crc32(Buffer.concat([typeBuf, data]));
    crcBuf.writeInt32BE(crc, 0);
    return Buffer.concat([len, typeBuf, data, crcBuf]);
  }

  const crcTable = [];
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) {
      c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
    }
    crcTable[n] = c;
  }

  function crc32(buf) {
    let crc = -1;
    for (let i = 0; i < buf.length; i++) {
      crc = (crc >>> 8) ^ crcTable[(crc ^ buf[i]) & 0xff];
    }
    return crc ^ -1;
  }

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr.writeUInt8(8, 8); // 8-bit
  ihdr.writeUInt8(6, 9); // RGBA
  ihdr.writeUInt8(0, 10);
  ihdr.writeUInt8(0, 11);
  ihdr.writeUInt8(0, 12);

  const rawData = [];
  const cx = width / 2;
  const cy = height / 2;
  const radius = width * 0.44;

  for (let y = 0; y < height; y++) {
    rawData.push(0);
    for (let x = 0; x < width; x++) {
      const dx = x - cx;
      const dy = y - cy;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist > radius) {
        rawData.push(0, 0, 0, 0);
      } else {
        // inner emblem (white dot/square)
        const inCenter = Math.abs(dx) < width * 0.22 && Math.abs(dy) < height * 0.22;
        if (inCenter) {
          rawData.push(255, 255, 255, 255);
        } else {
          rawData.push(r, g, b, 255);
        }
      }
    }
  }

  const idatData = zlib.deflateSync(Buffer.from(rawData));
  return Buffer.concat([
    signature,
    chunk('IHDR', ihdr),
    chunk('IDAT', idatData),
    chunk('IEND', Buffer.alloc(0))
  ]);
}

export function generateIconsForDir(targetDir, rgb = [0, 199, 60]) {
  if (!fs.existsSync(targetDir)) {
    fs.mkdirSync(targetDir, { recursive: true });
  }
  for (const size of [16, 48, 128]) {
    const png = createPng(size, size, rgb);
    const dest = path.join(targetDir, `icon-${size}.png`);
    fs.writeFileSync(dest, png);
  }
  console.log(`Icons generated in: ${targetDir}`);
}

// 직접 실행될 경우
if (process.argv[1]?.endsWith('generate-icons.mjs')) {
  generateIconsForDir('extensions/01-reading-guide/icons', [4, 117, 244]); // Whale Blue
  generateIconsForDir('extensions/02-study-notes/icons', [0, 199, 60]);   // Whale Green
  generateIconsForDir('extensions/03-unified-edu-suite/icons', [112, 51, 255]); // Whale Purple
}
