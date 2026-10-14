import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
// Intentional actual installation only on disposable CI runners, never on a user's workstation.
assert.equal(process.env.CI, 'true', 'Installer integration test requires a disposable CI account');
assert.equal(process.env.RAMRATIO_INSTALL_TEST, '1', 'Explicit CI test opt-in required');
const exe=path.resolve(process.argv[2]);
const win=process.platform==='win32', mac=process.platform==='darwin';
const home=os.homedir();
const folder=win?path.join(process.env.LOCALAPPDATA,'RamRatio','DesktopHelper'):path.join(home,mac?'Library/Application Support/RamRatio/DesktopHelper':'.local/share/ramratio/desktop-helper');
const host=path.join(folder,win?'RamRatio.Host.exe':'RamRatio.Host');
const manifest=win?path.join(folder,'com.ratioworks.ramratio.json'):path.join(home,mac?'Library/Application Support/Mozilla/NativeMessagingHosts':'.mozilla/native-messaging-hosts','com.ratioworks.ramratio.json');
assert.ok(!fs.existsSync(host)&&!fs.existsSync(manifest),'Refuse to replace a pre-existing installation');
function setup(choice){const r=spawnSync(exe,[],{input:choice+'\n',encoding:'utf8',timeout:15000});assert.equal(r.status,0,r.stderr+'\n'+r.stdout);}
setup('I');
const config=JSON.parse(fs.readFileSync(manifest,'utf8'));
assert.equal(config.path,host);assert.equal(config.name,'com.ratioworks.ramratio');
assert.deepEqual(config.allowed_extensions,['memory-optimizer@local']);
assert.deepEqual(fs.readFileSync(host),fs.readFileSync(exe));
if(win){const registry=spawnSync('reg.exe',['query','HKCU\\Software\\Mozilla\\NativeMessagingHosts\\com.ratioworks.ramratio','/ve'],{encoding:'utf8'});assert.equal(registry.status,0);assert.ok(registry.stdout.toLowerCase().includes(manifest.toLowerCase()));}
else assert.ok((fs.statSync(host).mode&0o100)!==0);
setup('I'); // update/repair is repeatable
const check=spawnSync(process.execPath,['tools/test-helper.mjs',host],{encoding:'utf8',timeout:15000});
assert.equal(check.status,0,check.stderr);
const marker=path.join(folder,'leave-other-files-alone.txt');fs.writeFileSync(marker,'test');
setup('U'); assert.ok(!fs.existsSync(host)&&!fs.existsSync(manifest));assert.equal(fs.readFileSync(marker,'utf8'),'test');
if(win)assert.notEqual(spawnSync('reg.exe',['query','HKCU\\Software\\Mozilla\\NativeMessagingHosts\\com.ratioworks.ramratio']).status,0);
fs.unlinkSync(marker);
setup('U'); // repeat removal is harmless
console.log('Install, native registration, installed-host protocol, update, removal, repeat removal, and preservation of unrelated files passed.');