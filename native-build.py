"""Build public raylib glue plus an explicitly selected external adapter directory."""
from pathlib import Path
import os,sys,subprocess,urllib.request,hashlib,tarfile,json,xml.etree.ElementTree as ET
h=Path(__file__).resolve().parent
mode=sys.argv[1];assert mode in ('desktop','web')
deps=h/'build/deps';deps.mkdir(parents=True,exist_ok=True)
pins={'raylib':('raysan5/raylib','dbc56a87da87d973a9c5baa4e7438a9d20121d28','81b06ce7c19cf3b634b0271c23c361ba6ad8bf45fb8b036abbfeb4260ec1e126')}
pins.update({'box2d':('erincatto/box2d','v3.1.1','fb6ef914b50f4312d7d921a600eabc12318bb3c55a0b8c0b90608fa4488ef2e4'),'jolt':('jrouwe/JoltPhysics','e77f175595e64cb44218cc9d9d56fc365ad0e36a','1f32328fb763135de10a244568d6ccb2ed9b1e6593fafe6dc6db5b2719d330bd')})
ext=Path(os.environ['BRUTAL_ADAPTER_ROOT']).resolve() if os.environ.get('BRUTAL_ADAPTER_ROOT') else None
adapter=json.loads((ext/'adapter.json').read_text()) if ext else {}
pins.update(adapter.get('dependencies',{}))
for name in pins:
 repo,revision,sha=pins[name];archive=deps/f'{name}.tar.gz'
 if not archive.exists():urllib.request.urlretrieve(f'https://codeload.github.com/{repo}/tar.gz/{revision}',archive)
 if hashlib.sha256(archive.read_bytes()).hexdigest()!=sha:raise SystemExit(f'{name} download hash mismatch')
 target=deps/name
 if not (target/'CMakeLists.txt').exists():
  target.mkdir(exist_ok=True)
  with tarfile.open(archive) as tf:
   for member in tf.getmembers():
    if '/' not in member.name:continue
    member.name=member.name.split('/',1)[1]
    if member.name:tf.extract(member,target,filter='data')
# The public build has no knowledge of adapter source modules: only this optional CMake seam.
threaded=mode=='web' and os.environ.get('BRUTAL_THREADS')=='true'
build=h/(('build/native-web-dotnet' if ext else 'build/native-web-public') if mode=='web' else ('build/native-desktop' if ext else 'build/native-desktop-public'))
if threaded:build=Path(str(build)+'-threaded')
env=os.environ.copy()
emroot=Path(env.get('EMSDK',str(Path.home()/'emsdk')))/'upstream/emscripten'
config=['cmake','-S',str(h/'native'),'-B',str(build),'-G','Ninja','-DCMAKE_BUILD_TYPE=Release',f'-DRAYLIB_SOURCE={deps/"raylib"}',f'-DBRUTAL_ADAPTER_ROOT={ext or ""}',f'-DBRUTAL_ADAPTER_TARGET={adapter.get("target", adapter.get("library", ""))}',f'-DBRUTAL_WEB_THREADS={"ON" if threaded else "OFF"}']
for name in set(adapter.get('dependencies',{}))|{'jolt','box2d'}:config.append(f'-D{name.upper()}_SOURCE={deps/name}')
if mode=='web':
 dotnet_root=Path(env.get('DOTNET_ROOT',str(Path.home()/'.dotnet')))
 sdk=dotnet_root/'packs/Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.linux-x64/10.0.10/tools'
 emroot=sdk/'emscripten'
 env['DOTNET_EMSCRIPTEN_LLVM_ROOT']=str(sdk/'bin')
 env['DOTNET_EMSCRIPTEN_BINARYEN_ROOT']=str(sdk)
 env['DOTNET_EMSCRIPTEN_NODE_JS']=str(dotnet_root/'packs/Microsoft.NET.Runtime.Emscripten.3.1.56.Node.linux-x64/10.0.10/tools/bin/node')
 env['EM_CONFIG']=str(emroot/'.emscripten')
 env['EM_FROZEN_CACHE']='0';env['FROZEN_CACHE']=''
 env['EM_CACHE']=str(h/'build/dotnet-emscripten-cache');env['EMCC_CORES']='2';env['BINARYEN_CORES']='2'
 config=['python3',str(emroot/'emcmake.py')]+config
subprocess.run(['nice','-n','19']+config,env=env,check=True)
subprocess.run(['nice','-n','19','cmake','--build',str(build),'--target','brutal_all','--parallel',os.environ.get('FBS_BUILD_JOBS','2')],env=env,check=True)
if mode=='desktop':(h/'build/NativeDesktopPath.txt').write_text(str(build))
if mode=='web':
 bundles=build/'bundles';bundles.mkdir(exist_ok=True)
 archives=[p for p in build.rglob('*.a') if bundles not in p.parents]
 core=[p for p in archives if p.name in ('brutal_raylib.a','libraylib.a')]
 assert len(core)==2,core
 def bundle(name,parts):
  result=bundles/name
  stamp=result.with_suffix('.inputs.json')
  inputs=json.dumps([(str(p),p.stat().st_size,p.stat().st_mtime_ns) for p in parts])
  if result.exists() and stamp.exists() and stamp.read_text()==inputs:return
  if result.exists():result.unlink()
  script='CREATE '+str(result)+'\n'+''.join('ADDLIB '+str(p)+'\n' for p in parts)+'SAVE\nEND\n'
  subprocess.run(['python3',str(emroot/'emar.py'),'-M'],input=script,text=True,check=True,env=env)
  stamp.write_text(inputs)
 bundle('brutal_raylib.a',core)
 refs=[bundles/'brutal_raylib.a']
 physics=[]
 for name,partnames in [('jolt',('brutal_jolt.a','libJolt.a')),('box2d',('brutal_box2d.a','libbox2d.a'))]:
  parts=[p for p in archives if p.name in partnames];assert len(parts)==2,parts
  bundle(name+'.a',parts);refs.append(bundles/(name+'.a'));physics+=parts
 if ext:
  others=[p for p in archives if p not in core+physics]
  name=adapter['library']+'.a';bundle(name,others);refs.append(bundles/name)
 project=ET.Element('Project');group=ET.SubElement(project,'ItemGroup')
 for p in refs:ET.SubElement(group,'NativeFileReference',{'Include':str(p)})
 for function in ('_br_unlock_audio','_br_input_edge','_br_audio_pull','_br_audio_ack'):ET.SubElement(group,'EmccExportedFunction',{'Include':function})
 prop=ET.SubElement(project,'PropertyGroup');ET.SubElement(prop,'EmccExtraLDFlags').text='$(EmccExtraLDFlags) '+'-lstdc++ '+adapter.get('linkFlags','')
 target=h/('build/NativeReferences-'+str(threaded).lower()+'.props');value=ET.tostring(project,encoding='unicode')
 if not target.exists() or target.read_text()!=value:target.write_text(value)
