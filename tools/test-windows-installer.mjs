import assert from "node:assert/strict";
import {spawnSync} from "node:child_process";
import {statSync} from "node:fs";
import path from "node:path";

if(process.platform!=="win32") throw Error("The portable GUI setup test requires Windows.");
const installer=path.resolve(process.argv[2]||"");
assert.ok(statSync(installer).size>100_000,"Standalone GUI setup is unexpectedly small.");
const verification=spawnSync(installer,["--verify-payload"],{encoding:"utf8",timeout:30000,windowsHide:true});
assert.equal(verification.status,0,"Bundled native host failed integrity verification: "+verification.stderr);
const badCommand=spawnSync(installer,["--not-a-real-command"],{encoding:"utf8",timeout:30000,windowsHide:true});
assert.equal(badCommand.status,2,"Unsupported arguments must not trigger installation.");
console.log("PASS: single-EXE Windows setup has intact native-host payload; unsupported options cannot install.");
