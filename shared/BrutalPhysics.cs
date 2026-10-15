using Brutal.Box2DApi;
using Brutal.JoltApi;
using Brutal.Numerics;
namespace FinalBuild.Brutal;
// All native physics calls stay here; games use IPhysicsLayer on every target.
internal sealed class BrutalPhysics:IPhysicsLayer {
 B2World world;
 List<B2Body> bodies2=[];
 List<uint> bodies3=[];
 Factory factory;
 PhysicsSystem system;
 BodyInterface bodyInterface;
 BroadPhaseLayerInterfaceTable broad;
 ObjectLayerPairFilterTable pairs;
 ObjectVsBroadPhaseLayerFilterTable filters;
 TempAllocatorImpl allocator;
 JobSystemSingleThreaded jobs;
 float accumulator;
 int steps;bool disposed;
 public BrutalPhysics()=>Initialize();
 public void Reset(){Dispose();disposed=false;bodies2.Clear();bodies3.Clear();accumulator=0;steps=0;Initialize();}
 void Initialize(){
  world=Box2D.CreateWorld(new B2WorldDef{Gravity=new(0,-9.81f)});
  Jolt.RegisterDefaultAllocator();factory=Factory.Create();Factory.SetInstance(factory);Jolt.RegisterTypes();
  broad=BroadPhaseLayerInterfaceTable.Create(2,2);broad.MapObjectToBroadPhaseLayer(0,0);broad.MapObjectToBroadPhaseLayer(1,1);
  pairs=ObjectLayerPairFilterTable.Create(2);pairs.EnableCollision(0,1);pairs.EnableCollision(1,1);
  filters=ObjectVsBroadPhaseLayerFilterTable.Create(broad,2,pairs,2);
  system=PhysicsSystem.Create();system.Init(128,0,256,256,broad,filters,pairs);bodyInterface=system.GetBodyInterface();
  allocator=TempAllocatorImpl.Create(8*1024*1024);jobs=JobSystemSingleThreaded.Create(1024);
 }
 static void Validate(Vec3 p,Vec3 h,int count,bool threeD){if(count>=128)throw new InvalidOperationException("Physics body capacity is 128 per world");if(!float.IsFinite(p.X)||!float.IsFinite(p.Y)||!float.IsFinite(p.Z)||!float.IsFinite(h.X)||!float.IsFinite(h.Y)||!float.IsFinite(h.Z)||h.X<.03f||h.Y<.03f||(threeD&&h.Z<.03f))throw new ArgumentException("Finite positions and half extents >= 0.03 are required");}
 public int Box2(Vec3 p,Vec3 h,bool dynamic){
  ObjectDisposedException.ThrowIf(disposed,this);Validate(p,h,bodies2.Count,false);
  var body=world.CreateBody(new B2BodyDef{Type=dynamic?B2BodyType.Dynamic:B2BodyType.Static,Position=new(p.X,p.Y)});
  var shape=new B2ShapeDef{Density=1};var polygon=Box2D.MakeBox(h.X,h.Y);body.CreatePolygonShape(shape,polygon);
  bodies2.Add(body);return bodies2.Count-1;
 }
 public int Box3(Vec3 p,Vec3 h,bool dynamic){
  ObjectDisposedException.ThrowIf(disposed,this);Validate(p,h,bodies3.Count,true);
  using var shape=BoxShape.Create(new(h.X,h.Y,h.Z),.02f,default);shape.SetDensity(1);
  using var settings=BodyCreationSettings.Create(shape,new(p.X,p.Y,p.Z),floatQuat.Identity,dynamic?MotionType.Dynamic:MotionType.Static,(ushort)(dynamic?1:0));
  uint id=bodyInterface.CreateAndAddBody(settings,dynamic?Activation.Activate:Activation.DontActivate);
  if(id==uint.MaxValue)throw new InvalidOperationException("Jolt body capacity exhausted");
  bodies3.Add(id);return bodies3.Count-1;
 }
 public Pose Read2(int body){ObjectDisposedException.ThrowIf(disposed,this);var b=bodies2[body];var p=b.GetPosition();var q=b.GetRotation();float angle=MathF.Atan2(q.S,q.C);return new(new(p.X,p.Y,0),0,0,MathF.Sin(angle/2),MathF.Cos(angle/2));}
 public Pose Read3(int body){ObjectDisposedException.ThrowIf(disposed,this);bodyInterface.GetPositionAndRotation(bodies3[body],out var p,out var q);return new(new(p.X,p.Y,p.Z),q.X,q.Y,q.Z,q.W);}
 public void Impulse2(int body,Vec3 v)=>bodies2[body].ApplyLinearImpulseToCenter(new(v.X,v.Y),true);
 public void Impulse3(int body,Vec3 v)=>bodyInterface.AddImpulse(bodies3[body],new(v.X,v.Y,v.Z));
 public void Step(float delta){ObjectDisposedException.ThrowIf(disposed,this);if(!float.IsFinite(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));accumulator+=Math.Clamp(delta,0,.1f);while(accumulator>=1f/60){world.Step(1f/60,4);var result=system.Update(1f/60,1,allocator,jobs);if(result!=0)throw new Exception($"Jolt update failed: {result}");accumulator-=1f/60;steps++;}}
 public string Metrics=>$"{{\"box2d\":{bodies2.Count},\"jolt\":{bodies3.Count},\"steps\":{steps}}}";
 public void Dispose(){if(disposed)return;disposed=true;world.Dispose();foreach(uint id in bodies3){bodyInterface.RemoveBody(id);bodyInterface.DestroyBody(id);}system.Dispose();jobs.Dispose();allocator.Dispose();filters.Dispose();pairs.Dispose();broad.Dispose();Jolt.UnregisterTypes();Factory.SetInstance(default);factory.Dispose();}
}
