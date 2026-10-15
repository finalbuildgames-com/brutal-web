/* BRUTAL's explicit-layout definitions are x64 wire records on every target.
 * Convert their fields to native Box2D layouts; never memcpy a pointer-bearing
 * BRUTAL record into a wasm32 native record. Supported entry points only. */
#include <box2d/box2d.h>
#include <string.h>
#include <stdint.h>
#define API __attribute__((visibility("default")))
#define PUT(o,f) memcpy((char*)wire+(o),&d.f,sizeof d.f)
#define GET(o,f) memcpy(&d.f,(const char*)wire+(o),sizeof d.f)
#define WORLD_FIELDS(OP) OP(0,gravity);OP(8,restitutionThreshold);OP(12,hitEventThreshold);OP(16,contactHertz);OP(20,contactDampingRatio);OP(24,maxContactPushSpeed);OP(28,maximumLinearSpeed);OP(32,frictionCallback);OP(40,restitutionCallback);OP(48,enableSleep);OP(49,enableContinuous);OP(52,workerCount);OP(56,enqueueTask);OP(64,finishTask);OP(72,userTaskContext);OP(80,userData);OP(88,internalValue)
#define BODY_FIELDS(OP) OP(0,type);OP(4,position);OP(12,rotation);OP(20,linearVelocity);OP(28,angularVelocity);OP(32,linearDamping);OP(36,angularDamping);OP(40,gravityScale);OP(44,sleepThreshold);OP(48,name);OP(56,userData);OP(64,enableSleep);OP(65,isAwake);OP(66,fixedRotation);OP(67,isBullet);OP(68,isEnabled);OP(69,allowFastRotation);OP(72,internalValue)
#define SHAPE_FIELDS(OP) OP(0,userData);OP(8,material);OP(32,density);OP(40,filter);OP(64,isSensor);OP(65,enableSensorEvents);OP(66,enableContactEvents);OP(67,enableHitEvents);OP(68,enablePreSolveEvents);OP(69,invokeContactCreation);OP(70,updateBodyMass);OP(72,internalValue)
_Static_assert(sizeof(b2WorldId)==4 && sizeof(b2BodyId)==8 && sizeof(b2Polygon)==144 && sizeof(b2SurfaceMaterial)==24 && sizeof(b2Filter)==24,"BRUTAL pointer-free ABI drift");
API void br_b2DefaultWorldDef(void* wire){b2WorldDef d=b2DefaultWorldDef();memset(wire,0,96);WORLD_FIELDS(PUT);}
API void br_b2DefaultBodyDef(void* wire){b2BodyDef d=b2DefaultBodyDef();memset(wire,0,80);BODY_FIELDS(PUT);}
API void br_b2DefaultShapeDef(void* wire){b2ShapeDef d=b2DefaultShapeDef();memset(wire,0,80);SHAPE_FIELDS(PUT);}
API void br_b2CreateWorld(const void* wire,b2WorldId* out){b2WorldDef d=b2DefaultWorldDef();WORLD_FIELDS(GET);*out=b2CreateWorld(&d);}
API void br_b2DestroyWorld(const b2WorldId* p){b2DestroyWorld(*p);}
API void br_b2World_Step(const b2WorldId* p,float dt,int substeps){b2World_Step(*p,dt,substeps);}
API void br_b2CreateBody(const b2WorldId* w,const void* wire,b2BodyId* out){b2BodyDef d=b2DefaultBodyDef();BODY_FIELDS(GET);*out=b2CreateBody(*w,&d);}
API void br_b2MakeBox(float x,float y,b2Polygon* out){*out=b2MakeBox(x,y);}
API void br_b2CreatePolygonShape(const b2BodyId* b,const void* wire,const b2Polygon* p,b2ShapeId* out){b2ShapeDef d=b2DefaultShapeDef();SHAPE_FIELDS(GET);*out=b2CreatePolygonShape(*b,&d,p);}
API void br_b2Body_GetPosition(const b2BodyId* b,b2Vec2* out){*out=b2Body_GetPosition(*b);}
API void br_b2Body_GetRotation(const b2BodyId* b,b2Rot* out){*out=b2Body_GetRotation(*b);}
API void br_b2Body_ApplyLinearImpulseToCenter(const b2BodyId* b,const b2Vec2* impulse,unsigned char wake){b2Body_ApplyLinearImpulseToCenter(*b,*impulse,wake!=0);}

API void br_b2DefaultSurfaceMaterial(b2SurfaceMaterial* p){*p=b2DefaultSurfaceMaterial();}
API void br_b2DefaultFilter(b2Filter* p){*p=b2DefaultFilter();}
API void br_b2GetVersion(b2Version* p){*p=b2GetVersion();}
API void br_b2DestroyBody(const b2BodyId* b){b2DestroyBody(*b);}
