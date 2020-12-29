using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Utils;



[UpdateInGroup(typeof(SimulationSystemGroup))]
public class Unit_UpdatePosition_System : SystemBase
{
    ComponentDataFromEntity<Translation> positions;

    protected override void OnCreate()
    {
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                typeof(Unit_Position_Component)
            }
        }));
    }

    protected override void OnUpdate()
    {
        var positions = GetComponentDataFromEntity<Translation>(true);
        Entities
        .WithReadOnly(positions)
        .WithName("Unit_UpdatePosition")
        .ForEach((Entity e, int entityInQueryIndex, ref Unit_Position_Component udc, in DynamicBuffer<SoldierReference> refs) =>
        {
            // Update position of the Unit
            udc.position = new float3(0, 0, 0);
            for (int i=0; i<refs.Length; i++)
                udc.position += positions[refs[i].soldierRef].Value;
            udc.position /= refs.Length;
        }).WithDisposeOnCompletion(positions).ScheduleParallel();

    }
}
