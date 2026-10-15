from pathlib import Path
import os,shutil,hashlib,json
h=Path(__file__).resolve().parent
out=h/'build/assets'
if out.is_symlink():raise SystemExit('Refusing symlinked asset directory')
if out.exists():shutil.rmtree(out)
shutil.copytree(h/'assets',out)
if os.environ.get('BRUTAL_ADAPTER_ROOT'):
 ext=Path(os.environ['BRUTAL_ADAPTER_ROOT'])
 shutil.copytree(ext/'assets',out,dirs_exist_ok=True)
 adapter=json.loads((ext/'adapter.json').read_text())
 for row in adapter.get('assetCopies',[]):
  shutil.copy2(h/'build/deps'/row['dependency']/row['source'],out/row['destination'])
rows=[{'path':p.relative_to(out).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size} for p in sorted(out.rglob('*')) if p.is_file()]
(h/'build/assets.json').write_text(json.dumps(rows,indent=2)+'\n')
