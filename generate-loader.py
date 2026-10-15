"""FBS source-assembly overlay; never modifies BRUTAL upstream."""
from pathlib import Path
import sys
output = Path(sys.argv[2]); output.parent.mkdir(parents=True,exist_ok=True)
category = (Path(sys.argv[1]) / 'Brutal.Logging/LogCategory.cs').read_text(encoding='utf-8-sig')
(output.parent / 'LogCategory.g.cs').write_text('#nullable enable\nusing Trace = Brutal.Logging.Internal.Trace;\nusing Debug = Brutal.Logging.Internal.Debug;\n' + category)
# Native-owned borrowed strings trigger BRUTAL's current safety analyzer when
# source-compiled together. Suppress only those generated files in an overlay.
import re
brutal=Path(sys.argv[1]); overlay=output.parent/'PhysicsOverlay'
for rel in ['Generated/Jolt.gen.cs','Generated/Classes/CharacterBase.gen.cs','Generated/Classes/PhysicsMaterial.gen.cs','Generated/Classes/RTTI.gen.cs']:
 source=(brutal/'Brutal.Jolt'/rel).read_text(encoding='utf-8-sig')
 source='#pragma warning disable BRUTALCORE0010 // Upstream native-owned borrowed strings; no managed movable storage.\n'+source
 dest=overlay/rel;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(source)
# Normalize only the used struct-by-value entry points to scalar/pointer C ABIs.
for rel,methods in {'BoxShape':['BoxShape_Create_3'],'BodyCreationSettings':['BodyCreationSettings_Create_3'],'BodyInterface':['BodyInterface_AddImpulse']}.items():
 source=(brutal/'Brutal.Jolt/Generated/PInvoke'/f'{rel}.gen.cs').read_text()
 for name in methods:
  pattern=r'\[LibraryImport\("jolt", EntryPoint = "'+name+r'"\)\]\s*public static partial (\w+) '+name+r'\((.*?)\);'
  match=re.search(pattern,source,re.S);assert match,name
  result,args=match.groups();args=' '.join(args.split())
  parts=[x.strip().split() for x in args.split(',')]
  call=', '.join(('in ' if t in ('float3','floatQuat') else '')+n for t,n in parts)
  raw=', '.join(('in ' if t in ('float3','floatQuat') else '')+t+' '+n for t,n in parts)
  replacement=f'public static {result} {name}({args}) => Raw_{name}({call});\n[LibraryImport("jolt", EntryPoint="br_{name}")] private static partial {result} Raw_{name}({raw});'
  source=source[:match.start()]+replacement+source[match.end():]
 dest=overlay/'Generated/PInvoke'/f'{rel}.gen.cs';dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(source)
# Keep BRUTAL's public Box2D signatures and explicit x64 wire layouts. The native
# adapter converts pointer-bearing definitions to each target's native layout.
source=(brutal/'Brutal.Box2D/Box2D.gen.cs').read_text(encoding='utf-8-sig')
box_entries={'b2DestroyBody','b2DefaultSurfaceMaterial','b2DefaultFilter','b2GetVersion','b2DefaultWorldDef','b2DefaultBodyDef','b2DefaultShapeDef','b2CreateWorld','b2DestroyWorld','b2World_Step','b2CreateBody','b2MakeBox','b2CreatePolygonShape','b2Body_GetPosition','b2Body_GetRotation','b2Body_ApplyLinearImpulseToCenter'}
pattern=r'\[LibraryImport\(Dll, EntryPoint = "(\w+)"\)\]\s*(public|internal) static unsafe partial (\w+) (\w+)\((.*?)\);'
seen=set()
def normalize_box(m):
 entry,access,result,name,args=m.groups()
 if entry not in box_entries:return m.group()
 seen.add(entry);args=' '.join(args.split());call=[];raw=[]
 for arg in args.split(',') if args else []:
  words=arg.strip().split();typ,n=words[-2:];byref=len(words)==3 or typ.startswith('B2') or typ=='float2'
  prefix=words[0] if len(words)==3 else ('in' if byref else '')
  call.append((prefix+' ' if prefix else '')+n);raw.append((prefix+' ' if prefix else '')+typ+' '+n)
 if result!='void':raw.append('out '+result+' result');call.append('out var result')
 body=f'Raw_{entry}('+', '.join(call)+');'+(' return result;' if result!='void' else '')
 return f'{access} static unsafe {result} {name}({args}) {{ {body} }}\n[LibraryImport(Dll, EntryPoint="br_{entry}")] private static unsafe partial void Raw_{entry}('+', '.join(raw)+');'
source=re.sub(pattern,normalize_box,source,flags=re.S);assert seen==box_entries,(box_entries-seen)
(overlay/'Box2D.g.cs').write_text(source)

source=(brutal/'Brutal.Concurrency/Jobs/JobRunner.cs').read_text().replace('Thread.CurrentThread.Priority = ThreadPriority;', 'if (!OperatingSystem.IsBrowser() && ThreadPriority != ThreadPriority.Normal) Thread.CurrentThread.Priority = ThreadPriority;')
(overlay/'JobRunner.cs').write_text(source)
