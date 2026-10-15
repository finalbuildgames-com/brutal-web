from pathlib import Path
import shutil
h=Path(__file__).resolve().parent
out=h/'build/publish/wwwroot'
if out.is_symlink():raise SystemExit('Refusing symlinked publish directory')
if out.exists():shutil.rmtree(out)
shutil.copytree(h/'FinalBuild.Brutal.Web/wwwroot',out)
for name,mode in [('single','false'),('threaded','true')]:
 shutil.copytree(h/f'build/publish-{mode}/wwwroot/_framework',out/name/'_framework')
shutil.copytree(h/'build/assets',out/'assets')
shutil.copy2(h/'build/assets.json',out/'assets.json')
shutil.copy2(h/'game.json',out/'game.json')
