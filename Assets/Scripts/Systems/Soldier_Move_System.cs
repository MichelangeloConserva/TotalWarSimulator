using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Utils;



[UpdateInGroup(typeof(SimulationSystemGroup))]
public class Soldier_Move_System : SystemBase
{
    BuildPhysicsWorld buildPhysicsWorldSystem;

    protected override void OnCreate()
    {
        buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();

        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                typeof(Soldier_Targets_Component)
            }
        }));
    }


    protected override void OnUpdate()
    {

    }
}
