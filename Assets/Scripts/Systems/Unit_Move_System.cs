using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Utils;




[UpdateBefore(typeof(SimulationSystemGroup))]
public class Unit_Move_System : SystemBase
{
    BuildPhysicsWorld buildPhysicsWorldSystem;

    EntityQuery unitSoldiers_query;

    protected override void OnCreate()
    {
        buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();

        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                typeof(Unit_Data_Component)
            }
        }));

        unitSoldiers_query = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                typeof(Soldier_ReferenceToUnit_Component)
            }
        });


    }


    //private struct UpdateSoldiers : IJobChunk
    //{
    //    [ReadOnly] public float deltaTime;
    //    [ReadOnly] public float curT;
    //    [ReadOnly] public ComponentTypeHandle<Translation> positions;
    //    [ReadOnly] public ComponentTypeHandle<Soldier_Targets_Component> targets;
    //    [ReadOnly] public ComponentTypeHandle<PhysicsMass> mass;
    //    public ComponentTypeHandle<PhysicsVelocity> vel;
    //    public ComponentTypeHandle<Rotation> rot;

    //    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    //    {
    //        var chunkPositions = chunk.GetNativeArray(positions);
    //        var chunkTargets = chunk.GetNativeArray(targets);
    //        var chunkRotations = chunk.GetNativeArray(rot);
    //        var chunkVels = chunk.GetNativeArray(vel);

    //        for (var i = 0; i < chunk.Count; i++)
    //        {
    //            Debug.DrawLine(chunkPositions[i].Value, chunkTargets[i].targetPosition, Color.cyan);
    //        }



    //    }
    //}



    protected override void OnUpdate()
    {

        var positionsFE = GetComponentDataFromEntity<Translation>(true);
        var tgcsFE = GetComponentDataFromEntity<Soldier_Targets_Component>(true);
        float deltaTime = Time.DeltaTime;


        //unitSoldiers_query.ResetFilter();
        //Debug.Log(unitSoldiers_query.CalculateEntityCount());
        //unitSoldiers_query.SetSharedComponentFilter<Soldier_ReferenceToUnit_Component>(new Soldier_ReferenceToUnit_Component { unitID = 59186 });
        //Debug.Log(unitSoldiers_query.CalculateEntityCount());


        Entities
        .WithName("Unit_Move")
        .WithReadOnly(positionsFE)
        .WithReadOnly(tgcsFE)
        .WithoutBurst()
        .ForEach((Entity e, in Unit_Data_Component udc, in DynamicBuffer<SoldierReference> refs) =>
        {

            NativeArray<float> distances = new NativeArray<float>(refs.Length, Allocator.Temp);
            NativeArray<float> t = new NativeArray<float>(1, Allocator.Temp);
            NativeArray<float3> positions = new NativeArray<float3>(refs.Length, Allocator.Temp);
            NativeArray<float3> targets = new NativeArray<float3>(refs.Length, Allocator.Temp);
            NativeArray<float3> targetsLookAt = new NativeArray<float3>(refs.Length, Allocator.Temp);

            //float minDist = 100000;
            for (int i = 0; i < refs.Length; i++)
            {
                positions[i] = positionsFE[refs[i].soldierRef].Value;
                targets[i] = tgcsFE[refs[i].soldierRef].targetPosition;
                targetsLookAt[i] = tgcsFE[refs[i].soldierRef].targetLookAt;

                distances[i] = math.distance(positions[i], targets[i]);
                t[0] += distances[i];
            }
            t[0] /= (refs.Length);


            //var job = new UpdateSoldiers()
            //{
            //    deltaTime = deltaTime,
            //    curT = t[0],
            //    mass = GetComponentTypeHandle<PhysicsMass>(),
            //    positions = GetComponentTypeHandle<Translation>(),
            //    rot = GetComponentTypeHandle<Rotation>(),
            //    targets = GetComponentTypeHandle<Soldier_Targets_Component>(),
            //    vel = GetComponentTypeHandle<PhysicsVelocity>()
            //};

            //Dependency = job.Schedule(unitSoldiers_query, Dependency);

            const float maxAcceleration = 250.0f;
            float curSpeed;
            float3 v, p, tg, impulse;
            PhysicsVelocity vc;
            PhysicsMass massComponent;
            Soldier_Targets_Component tgs;
            for (int i = 0; i < refs.Length; i++)
            {
                vc = EntityManager.GetComponentData<PhysicsVelocity>(refs[i].soldierRef);


                p = positions[i];
                tg = targets[i];
                if (math.distance(tg, p) < 0.1f) continue;

                curSpeed = distances[i] * udc.soldierSpeed / t[0];

                v = vc.Linear;
                massComponent = EntityManager.GetComponentData<PhysicsMass>(refs[i].soldierRef);

                impulse = curSpeed * math.normalize(tg - p) - v;
                impulse -= new float3(0, impulse.y, 0);

                // Clip impulse
                float maxImpulse = math.rcp(massComponent.InverseMass) * Time.DeltaTime * maxAcceleration;
                impulse *= math.min(1.0f, math.sqrt((maxImpulse * maxImpulse) / math.lengthsq(impulse)));

                // Apply impulse
                vc.Linear += impulse * massComponent.InverseMass;
                vc.Angular = new float3(0, 0, 0);
                EntityManager.SetComponentData(refs[i].soldierRef, vc);

                // TODO : Add check if unit is in fight
                EntityManager.SetComponentData(refs[i].soldierRef, new Rotation { Value = Quaternion.LookRotation(targetsLookAt[i]) });

            }

        }).Run();


    }
}
