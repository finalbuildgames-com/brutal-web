#!/usr/bin/env bash
# Focused Linux acceptance; Xvfb/software Mesa and a private silent audio sink.
set -euo pipefail
here="${FBS_KIT_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)}"
evidence="${FBS_EVIDENCE_DIR:-$here/build/evidence}"
prefix="${FBS_EVIDENCE_PREFIX:-phase3-}"
[[ "$prefix" =~ ^[a-z0-9-]*$ ]] || exit 1
mkdir -p "$here/build" "$evidence"
app="$here/FinalBuild.Brutal.Desktop/bin/Release/net10.0/linux-x64/publish/FinalBuild.Brutal.Desktop"
saves="$(mktemp -d "$here/build/verify-saves.XXXXXX")"
sink="brutal_jam_test_$$"
module="$(pactl load-module module-null-sink sink_name="$sink" rate=48000 channels=2)"
record_pid=''
cleanup(){ if [[ -n "$record_pid" ]]; then kill "$record_pid" 2>/dev/null || true; wait "$record_pid" 2>/dev/null || true; fi; pactl unload-module "$module"; rm -rf "$saves"; }
trap cleanup EXIT
nice -n 19 "$app" --physics-check > "$evidence/${prefix}physics.log" 2>&1
parec --device="$sink.monitor" --format=s16le --rate=48000 --channels=2 > "$here/build/desktop-audio.pcm" &
record_pid=$!
export PULSE_SINK="$sink" LIBGL_ALWAYS_SOFTWARE=1 FBS_SAVE_DIR="$saves"
FBS_SAMPLE_FRAMES=420 FBS_SAMPLE_SCREENSHOT_FRAME=359 FBS_SAMPLE_SCREENSHOT="$evidence/${prefix}desktop.png" nice -n 19 xvfb-run -a -s '-screen 0 1024x768x24' "$app" > "$evidence/${prefix}desktop.log" 2>&1
kill "$record_pid"; wait "$record_pid" || true; record_pid=''
FBS_SAMPLE_FRAMES=0 nice -n 19 xvfb-run -a -s '-screen 0 1024x768x24' "$app" > "$evidence/${prefix}desktop-reload.log" 2>&1
python3 - "$here/build/desktop-audio.pcm" "$evidence" "$prefix" <<'PY'
import sys,array,math,json,pathlib,re
pcm=array.array('h',pathlib.Path(sys.argv[1]).read_bytes());peak=max(abs(x)/32768 for x in pcm);rms=math.sqrt(sum((x/32768)**2 for x in pcm)/len(pcm))
assert peak>.001 and rms>.0001
out=pathlib.Path(sys.argv[2]);prefix=sys.argv[3]
logs=[(out/(prefix+n)).read_text() for n in ['desktop.log','desktop-reload.log']]
rows=[json.loads(re.search(r'^WITNESS (.+)$',s,re.M)[1]) for s in logs]
a,b=rows
assert a['failed']==0 and a['score']==1 and a['modelReady']
assert a['services']['threaded'] and all(i>1 for i in a['services']['workers'])
assert a['services']['checksum']==399960000 and a['services']['streamBytes']==131072 and a['services']['streamChunks']>1
assert a['physics']['top2y']<5 and a['physics']['top3y']<5
assert a['physics']['top2y']>=0 and a['physics']['top3y']>=0
if 'started' in a['audio']:assert a['audio']['started']>=1 and a['audio']['consumed']>0 and a['audio']['underrun']==0
assert b['services']['previousSave'] and b['score']==a['score'] and abs(b['playerX']-a['playerX'])<.01
for s in logs:
 assert re.search(r'^resources=0$',s,re.M)
 assert not re.search(r'\b(?:WARNING|ERROR):',s)
result={'mode':'Linux Xvfb/software Mesa; silent PulseAudio monitor; real stereo s16 PCM 48000 Hz; isolated fixture save directory', 'samples':len(pcm),'audioPeak':peak,'audioRms':rms,'firstFrameMs':float(re.search(r'first-frame ms=([0-9.]+)',logs[0])[1]),'fps':[float(x) for x in re.findall(r'fps=([0-9.]+)',logs[0])], 'metrics':a,'restored':b,'resources':0}
(out/(prefix+'desktop.json')).write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result))
PY
