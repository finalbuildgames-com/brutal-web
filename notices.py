"""Stage attribution for desktop and browser distributions."""
from pathlib import Path
import os,shutil,sys
here=Path(__file__).resolve().parent
def stage(out):
    # Upstream and third-party attribution travels with the distributed source-built runtime.
    notice = out / 'licenses'
    notice.mkdir(exist_ok=True)
    for p in (here / 'licenses').iterdir():
        if p.is_file():
            shutil.copyfile(p, notice / p.name)
    for name in ('LICENSE', 'NOTICE', 'NOTICE.txt'):
        shutil.copyfile(here / name, out / name)
    ext=os.environ.get('BRUTAL_ADAPTER_ROOT')
    if ext:
        ext=Path(ext)
        shutil.copytree(ext/'licenses', notice/'adapter', dirs_exist_ok=True)
        shutil.copyfile(ext/'NOTICE',notice/'ADAPTER-NOTICE.txt')
    brutal = Path(os.environ.get('BRUTAL_SOURCE', str(here / 'upstream/brutal')))
    for name in ('LICENSE.txt', 'NOTICE.txt', 'CONTRIBUTORS.md'):
        p = brutal / name
        if p.is_file():
            shutil.copyfile(p, notice / ('BRUTAL-' + name))
    for p in brutal.glob('*/LICENSES/*'):
        if p.parent.parent.name in {'Brutal.Core', 'Brutal.Numerics', 'Brutal.Maths', 'Brutal.Memory', 'Brutal.Collections', 'Brutal.Interop', 'Brutal.Strings', 'Brutal.Logging', 'Brutal.Box2D', 'Brutal.Jolt'}:
            shutil.copyfile(p, notice / p.name)

if __name__=="__main__":stage(Path(sys.argv[1]))
