using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Utils;



public class Unit_Initializer_System : SystemBase
{
    BeginInitializationEntityCommandBufferSystem ei_ECB;

    protected override void OnCreate()
    {
        ei_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {

        var ecb = ei_ECB.CreateCommandBuffer().AsParallelWriter();
        Entities
            .ForEach((Entity e, int entityInQueryIndex, in Unit_Initializer_Component uic, in LocalToWorld ltw, in Unit_Data_Component udc) =>
            {
                var references = ecb.AddBuffer<SoldierReference>(entityInQueryIndex, e);
                var positions = GetFormationAtPos(uic.startPosition, uic.startDirection, uic.startNumOfSoldiers, uic.startCols, uic.lateralDist, uic.verticalDist);

                foreach (var p in positions)
                {
                    Entity soldier = ecb.Instantiate(entityInQueryIndex, uic.boxPrefab);
                    ecb.SetComponent(entityInQueryIndex, soldier, new Translation() { Value = p });
                    ecb.SetComponent(entityInQueryIndex, soldier, new Rotation() { Value = Quaternion.LookRotation(uic.startDirection) });
                    ecb.AddSharedComponent(entityInQueryIndex, soldier, new Soldier_ReferenceToUnit_Component { unitID = udc.unitID });
                    ecb.AddComponent(entityInQueryIndex, soldier, new Soldier_Targets_Component { targetPosition = p, targetLookAt = uic.startDirection });
                    ecb.AddComponent(entityInQueryIndex, soldier, new Soldier_Speed_Component { curSpeed = 0 });

                    references.Add(new SoldierReference { soldierRef = soldier });
                }

                ecb.RemoveComponent<Unit_Initializer_Component>(entityInQueryIndex, e);

            }).ScheduleParallel();
        ei_ECB.AddJobHandleForProducer(Dependency);


    }
}



