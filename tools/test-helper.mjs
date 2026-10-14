import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const exe = process.argv[2];
assert.ok(exe, 'Supply the published helper executable');
function frame(value) {
  const body = Buffer.from(JSON.stringify(value));
  const header = Buffer.alloc(4); header.writeUInt32LE(body.length);
  return Buffer.concat([header, body]);
}
const run = spawnSync(exe, ['--host'], {input: Buffer.concat([frame({type:'stats'}), frame({type:'unknown'})]), timeout:10000});
assert.equal(run.status, 0, run.stderr?.toString());
let offset = 0; const replies = [];
while (offset < run.stdout.length) {
  const size = run.stdout.readUInt32LE(offset); offset += 4;
  assert.ok(size > 0 && size < 1024 * 1024);
  replies.push(JSON.parse(run.stdout.subarray(offset, offset+size))); offset += size;
}
assert.equal(offset, run.stdout.length); assert.equal(replies.length, 2);
assert.equal(replies[0].ok, true);
assert.ok(replies[0].memory.total_bytes > 0);
assert.ok(replies[0].memory.available_bytes <= replies[0].memory.total_bytes);
assert.ok(replies[0].firefox.rss_bytes >= 0);
assert.equal(replies[1].reason, 'unsupported-request');
for (const input of [Buffer.from([255,255,255,127]), Buffer.from([10,0,0,0,123]), frame('invalid')]) {
  const bad = spawnSync(exe, ['--host'], {input, timeout:10000});
  assert.ok(bad.status === 2 || (bad.status === 0 && bad.stdout.length > 0));
}
console.log('Helper framing, memory snapshot, multiple requests, unknown request, and malformed input tests passed.');
