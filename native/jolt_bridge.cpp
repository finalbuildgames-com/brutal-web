// ABI implementation for the supported BRUTAL Jolt entry points. Jolt 5.6.0.
#include <Jolt/Jolt.h>
#include <Jolt/RegisterTypes.h>
#include <Jolt/Core/Factory.h>
#include <Jolt/Core/TempAllocator.h>
#include <Jolt/Core/JobSystemSingleThreaded.h>
#include <Jolt/Physics/PhysicsSystem.h>
#include <Jolt/Physics/Body/BodyCreationSettings.h>
#include <Jolt/Physics/Collision/Shape/BoxShape.h>
#include <Jolt/Physics/Collision/BroadPhase/BroadPhaseLayerInterfaceTable.h>
#include <Jolt/Physics/Collision/BroadPhase/ObjectVsBroadPhaseLayerFilterTable.h>
#include <Jolt/Physics/Collision/ObjectLayerPairFilterTable.h>
#include <cstring>
using namespace JPH;
#define API extern "C" __attribute__((visibility("default")))
static Vec3 vec(const float* p){return Vec3(p[0],p[1],p[2]);}
API void Jolt_RegisterDefaultAllocator(){JPH::RegisterDefaultAllocator();}
API Factory* Factory_Create(){return new Factory;}
API void Factory_SetInstance(Factory* p){Factory::sInstance=p;}
API void Factory_Destroy(Factory* p){delete p;}
API void Jolt_RegisterTypes(){JPH::RegisterTypes();}
API void Jolt_UnregisterTypes(){JPH::UnregisterTypes();}
API PhysicsSystem* PhysicsSystem_Create(){return new PhysicsSystem;}
API void PhysicsSystem_Destroy(PhysicsSystem* p){delete p;}
API void PhysicsSystem_Init(PhysicsSystem* p,uint32 n,uint32 locks,uint32 pairs,uint32 contacts,BroadPhaseLayerInterface* b,ObjectVsBroadPhaseLayerFilter* f,ObjectLayerPairFilter* l){p->Init(n,locks,pairs,contacts,*b,*f,*l);}
API BodyInterface* PhysicsSystem_GetBodyInterface(PhysicsSystem* p){return &p->GetBodyInterface();}
API uint32 PhysicsSystem_Update(PhysicsSystem* p,float dt,int steps,TempAllocator* a,JobSystem* j){return uint32(p->Update(dt,steps,a,j));}
API BroadPhaseLayerInterfaceTable* BroadPhaseLayerInterfaceTable_Create(uint32 n,uint32 m){return new BroadPhaseLayerInterfaceTable(n,m);}
API void BroadPhaseLayerInterfaceTable_Destroy(BroadPhaseLayerInterfaceTable* p){delete p;}
API void BroadPhaseLayerInterfaceTable_MapObjectToBroadPhaseLayer(BroadPhaseLayerInterfaceTable* p,uint16 o,uint8 b){p->MapObjectToBroadPhaseLayer(o,BroadPhaseLayer(b));}
API ObjectLayerPairFilterTable* ObjectLayerPairFilterTable_Create(uint32 n){return new ObjectLayerPairFilterTable(n);}
API void ObjectLayerPairFilterTable_EnableCollision(ObjectLayerPairFilterTable* p,uint16 a,uint16 b){p->EnableCollision(a,b);}
API void ObjectLayerPairFilterTable_Destroy(ObjectLayerPairFilterTable* p){delete p;}
API ObjectVsBroadPhaseLayerFilterTable* ObjectVsBroadPhaseLayerFilterTable_Create(BroadPhaseLayerInterface* b,uint32 n,ObjectLayerPairFilter* f,uint32 m){return new ObjectVsBroadPhaseLayerFilterTable(*b,n,*f,m);}
API void ObjectVsBroadPhaseLayerFilterTable_Destroy(ObjectVsBroadPhaseLayerFilterTable* p){delete p;}
API TempAllocatorImpl* TempAllocatorImpl_Create(size_t bytes){return new TempAllocatorImpl(bytes);}
API void TempAllocatorImpl_Destroy(TempAllocatorImpl* p){delete p;}
API JobSystemSingleThreaded* JobSystemSingleThreaded_Create_2(uint32 jobs){return new JobSystemSingleThreaded(jobs);}
API void JobSystemSingleThreaded_Destroy(JobSystemSingleThreaded* p){delete p;}
API BoxShape* br_BoxShape_Create_3(const float* half,float radius,PhysicsMaterial* m){auto* p=new BoxShape(vec(half),radius,m);p->AddRef();return p;}
API void Shape_Release(Shape* p){p->Release();}
API BodyCreationSettings* br_BodyCreationSettings_Create_3(Shape* shape,const float* position,const float* rotation,uint8 motion,uint16 layer){return new BodyCreationSettings(shape,vec(position),Quat(rotation[0],rotation[1],rotation[2],rotation[3]),EMotionType(motion),layer);}
API void BodyCreationSettings_Destroy(BodyCreationSettings* p){delete p;}
API uint32 BodyInterface_CreateAndAddBody(BodyInterface* p,BodyCreationSettings* s,int active){return p->CreateAndAddBody(*s,EActivation(active)).GetIndexAndSequenceNumber();}
API void BodyInterface_RemoveBody(BodyInterface* p,uint32 id){p->RemoveBody(BodyID(id));}
API void BodyInterface_DestroyBody(BodyInterface* p,uint32 id){p->DestroyBody(BodyID(id));}
API void BodyInterface_GetPositionAndRotation(BodyInterface* p,uint32 id,float* pos,float* rot){RVec3 v;Quat q;p->GetPositionAndRotation(BodyID(id),v,q);pos[0]=v.GetX();pos[1]=v.GetY();pos[2]=v.GetZ();rot[0]=q.GetX();rot[1]=q.GetY();rot[2]=q.GetZ();rot[3]=q.GetW();}
API void br_BodyInterface_AddImpulse(BodyInterface* p,uint32 id,const float* impulse){p->AddImpulse(BodyID(id),vec(impulse));}
API void ObjectLayerPairFilter_Destroy(ObjectLayerPairFilter* p){delete p;}
API void ObjectVsBroadPhaseLayerFilter_Destroy(ObjectVsBroadPhaseLayerFilter* p){delete p;}
API void PhysicsMaterial_Release(PhysicsMaterial* p){p->Release();}
API void ConvexShape_SetDensity(ConvexShape* p,float density){p->SetDensity(density);}
