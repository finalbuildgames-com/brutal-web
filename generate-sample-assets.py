"""Original deterministic sample assets; no external art, audio, or model dependency."""
from pathlib import Path
import zlib,struct,binascii,math,json,base64,wave
root=Path(__file__).parent/'assets';root.mkdir(exist_ok=True)
def png(path,w,h,pixels):
 def chunk(k,b):return struct.pack('>I',len(b))+k+b+struct.pack('>I',binascii.crc32(k+b)&0xffffffff)
 data=b''.join(b'\0'+bytes(pixels[y*w*4:(y+1)*w*4]) for y in range(h))
 path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(data,9))+chunk(b'IEND',b''))
px=[]
for y in range(64):
 for x in range(128):
  tx,ty=x%32,y%32;cell=x//32
  c=(0,0,0,0)
  if y<32:
   if cell==1 and abs(tx-16)+abs(ty-16)<13:c=(85,222,208,255) if abs(tx-16)+abs(ty-16)>7 else (217,255,245,255)
  else:
   if abs(tx-16)<(27-ty)*.55 and 4<ty<27:c=(226,236,237,255)
   if abs(tx-16)<3 and 13<ty<23:c=(231,164,66,255)
   if abs(tx-16)<4 and 25<ty<30-(cell%2)*3:c=(85,222,208,255)
  px.extend(c)
png(root/'atlas.png',128,64,px)
# Minimal monochrome HUD icon set: diamond, bars, triangle, speaker, rewind, question.
for name in ('score','pause','play','sound','reset','help'):
 pixels=[]
 for y in range(24):
  for x in range(24):
   on=False
   if name=='score':on=abs(x-12)+abs(y-12)<9 and abs(x-12)+abs(y-12)>4
   if name=='pause':on=(6<=x<=9 or 14<=x<=17) and 4<=y<=19
   if name=='play':on=6<=x<=18 and abs(y-12)<(20-x)*.7
   if name=='sound':on=(3<=x<9 and 9<=y<=15) or (8<=x<=14 and abs(y-12)<x-5) or (18<=x<=19 and 7<=y<=17)
   if name=='reset':on=(5<=x<=18 and (y==6 or y==18)) or (x==18 and 6<=y<=18) or (3<=x<=8 and abs(y-6)<(8-x))
   if name=='help':on=(7<=x<=16 and y in (4,5)) or (x in (16,17) and 5<=y<=10) or (11<=x<=16 and y in (10,11)) or (x in (11,12) and (11<=y<=14 or 18<=y<=20))
   pixels.extend((232,193,112,255) if on else (0,0,0,0))
 png(root/f'icon-{name}.png',24,24,pixels)
# Octahedron glTF: duplicated vertices for hard normals, embedded buffer.
v=[(0,1.8,0),(-.8,.6,0),(0,.6,.8),(.8,.6,0),(0,.6,-.8),(0,-.2,0)]
faces=[(0,2,1),(0,3,2),(0,4,3),(0,1,4),(5,1,2),(5,2,3),(5,3,4),(5,4,1)]
positions=[];normals=[]
for f in faces:
 a,b,c=[v[i] for i in f];u=[b[i]-a[i] for i in range(3)];w=[c[i]-a[i] for i in range(3)]
 n=[u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0]];length=math.sqrt(sum(t*t for t in n));n=[t/length for t in n]
 for point in (a,b,c):positions.extend(point);normals.extend(n)
buf=struct.pack('<72f',*positions)+struct.pack('<72f',*normals)
gltf={'asset':{'version':'2.0','generator':'Final Build Games original sample'},'scene':0,'scenes':[{'nodes':[0]}],'nodes':[{'mesh':0}], 'meshes':[{'primitives':[{'attributes':{'POSITION':0,'NORMAL':1},'material':0}]}], 'materials':[{'pbrMetallicRoughness':{'baseColorFactor':[1,1,1,1],'metallicFactor':0,'roughnessFactor':.8}}], 'buffers':[{'byteLength':len(buf),'uri':'data:application/octet-stream;base64,'+base64.b64encode(buf).decode()}], 'bufferViews':[{'buffer':0,'byteOffset':0,'byteLength':288,'target':34962},{'buffer':0,'byteOffset':288,'byteLength':288,'target':34962}], 'accessors':[{'bufferView':0,'componentType':5126,'count':24,'type':'VEC3','min':[-.8,-.2,-.8],'max':[.8,1.8,.8]},{'bufferView':1,'componentType':5126,'count':24,'type':'VEC3'}]}
(root/'beacon.gltf').write_text(json.dumps(gltf,separators=(',',':'))+'\n')
with wave.open(str(root/'collect.wav'),'wb') as f:
 f.setnchannels(1);f.setsampwidth(2);f.setframerate(48000)
 f.writeframes(b''.join(struct.pack('<h',int(.55*32767*math.sin(2*math.pi*(660+330*i/14400)*i/48000)*math.exp(-i/3000))) for i in range(14400)))
for profile,version,precision in [('gl330','#version 330',''),('es300','#version 300 es','precision mediump float;')]:
 (root/f'beacon.{profile}.vs').write_text(version+'\n'+precision+'''\nin vec3 vertexPosition; in vec3 vertexNormal; in vec2 vertexTexCoord;
uniform mat4 mvp; uniform mat4 matNormal;
out vec3 normal; out vec2 texcoord;
void main(){normal=normalize(mat3(matNormal)*vertexNormal);texcoord=vertexTexCoord;gl_Position=mvp*vec4(vertexPosition,1.0);}
''')
 (root/f'beacon.{profile}.fs').write_text(version+'\n'+precision+'''\nin vec3 normal; in vec2 texcoord; uniform sampler2D texture0; uniform vec4 colDiffuse; uniform float uPulse;
out vec4 finalColor;
void main(){float light=0.28+0.72*max(dot(normalize(normal),normalize(vec3(-0.4,0.8,0.5))),0.0);finalColor=vec4(colDiffuse.rgb*light*uPulse,1.0);}
''')

(root/'stream.bin').write_bytes(bytes(i%251 for i in range(131072)))
