#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';

const [, , fileArg, startArg = '1', countArg = '120'] = process.argv;

if (!fileArg) {
  console.error('Usage: node tools/read-utf8.mjs <file> [startLine=1] [lineCount=120]');
  process.exit(1);
}

const filePath = path.resolve(process.cwd(), fileArg);
const startLine = Math.max(1, Number.parseInt(startArg, 10) || 1);
const lineCount = Math.max(1, Number.parseInt(countArg, 10) || 120);

const text = fs.readFileSync(filePath, 'utf8').replace(/^\uFEFF/, '');
const lines = text.split(/\r?\n/);
const startIndex = startLine - 1;
const selected = lines.slice(startIndex, startIndex + lineCount);

for (let index = 0; index < selected.length; index += 1) {
  const lineNumber = String(startLine + index).padStart(4, ' ');
  process.stdout.write(`${lineNumber}: ${selected[index]}\n`);
}
