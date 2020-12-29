using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Utils;








[UpdateInGroup(typeof(SimulationSystemGroup))]
public class Unit_UpdateSoldiersTargets_System : SystemBase
{
    BeginInitializationEntityCommandBufferSystem ei_ECB;

    protected override void OnCreate()
    {

        ei_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                typeof(Unit_Target_Component)
            }
        }));
    }

    protected override void OnUpdate()
    {
        //var mng = EntityManager;

        //var ecb = ei_ECB.CreateCommandBuffer().AsParallelWriter();
        Entities
        //.WithReadOnly(mng)
        .WithName("Unit_UpdateSoldiersTargets")
        .WithoutBurst()  // because LSCAssignment is not yet supported
        .ForEach((in Unit_Target_Component utc, in Unit_Data_Component udc, in Entity e, in int entityInQueryIndex, in DynamicBuffer<SoldierReference> refs) =>
        {
            NativeArray<float3> targets = new NativeArray<float3>(GetFormationAtPos(utc.position, utc.direction, udc.numOfSoldiers, udc.cols, udc.lateralDist, udc.verticalDist), Allocator.Temp);

            float minDist = 1000000;
            NativeArray<float3> currents = new NativeArray<float3>(refs.Length, Allocator.Temp);
            for (int i=0; i<refs.Length; i++)
            {
                currents[i] = (GetComponentDataFromEntity<Translation>(true))[refs[i].soldierRef].Value;
                minDist = math.min(minDist, math.distance(targets[i], currents[i]));
            }


            if (minDist > 0.1f) // TODO : encode this value
            {
                NativeArray<int> assignments = new NativeArray<int>(LSCAssignment(currents, targets), Allocator.Temp);
                
                for (int i = 0; i < refs.Length; i++)
                {
                    Entity s = refs[i].soldierRef;

                    var stc = EntityManager.GetComponentData<Soldier_Targets_Component>(s);
                    float3 pos = currents[i];
                    float3 tg = targets[assignments[i]];

                    if (math.distance(pos, tg) > 3) // TODO : encode this value
                        stc.targetLookAt = tg;
                    else
                        stc.targetLookAt = utc.direction;

                    stc.targetPosition = tg - new float3(0,tg.y,0);

                    EntityManager.SetComponentData(s, stc);

                    //Debug.DrawLine(pos, tg, Color.red);

                }
            }
            

        }).Run();
        //ei_ECB.AddJobHandleForProducer(Dependency);
    }
}
