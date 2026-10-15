"""Stage only the web publish payload; verify the actual itch archive."""
import os
import gzip
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

here = Path(__file__).resolve().parent
source = here / 'build/publish/wwwroot'
out = here / 'build/itch'
if out.is_symlink():
    raise SystemExit('refusing symlinked package directory')
if out.exists():
    shutil.rmtree(out)
shutil.copytree(source, out)
# No authoring symbols or duplicate compression sidecars in the itch zip.
# itch applies HTTP compression. Preserve sidecars separately for self-hosting.
for path in out.rglob('*'):
    if path.is_file() and path.suffix in ('.br', '.gz', '.pdb', '.map', '.symbols'):
        path.unlink()
from notices import stage
stage(out)
archive = here / 'build/brutal-web-itch.zip'
files = sorted(p for p in out.rglob('*') if p.is_file())
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for p in files:
        z.write(p, p.relative_to(out))
with zipfile.ZipFile(archive) as z:
    assert 'index.html' in z.namelist()
    assert z.testzip() is None
    assert not any(p.endswith(('.so', '.dll', '.pdb')) for p in z.namelist())
# SDK-produced sidecars use its Brotli encoder. Compute equivalent per-file gzip-9.
rows = []
for p in files:
    rel = p.relative_to(out)
    br = source / (str(rel) + '.br')
    rows.append({'path': str(rel), 'raw': p.stat().st_size, 'gzip9': len(gzip.compress(p.read_bytes(), compresslevel=9, mtime=0)),
                 'brotli': br.stat().st_size if br.is_file() else None, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})
report = {'files': rows, 'rawBytes': sum(p['raw'] for p in rows), 'gzip9Bytes': sum(p['gzip9'] for p in rows),
          'brotliBytes': sum(p['brotli'] if p['brotli'] is not None else p['raw'] for p in rows),
          'brotliNote': 'SDK Brotli sidecars; assets and attribution without sidecars counted raw.',
          'zipBytes': archive.stat().st_size, 'zipSha256': hashlib.sha256(archive.read_bytes()).hexdigest()}
evidence=Path(os.environ.get('FBS_EVIDENCE_DIR',str(here/'build/evidence')))
evidence.mkdir(parents=True,exist_ok=True)
(evidence / 'phase3-sizes.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps({k: v for k, v in report.items() if k != 'files'}, indent=2))
print(archive)
