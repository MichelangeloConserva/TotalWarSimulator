using PathCreation;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Utils;



[UpdateInGroup(typeof(SimulationSystemGroup))]
public class Unit_UpdateFormation_System : SystemBase
{
    private EntityQuery m_query;

    protected override void OnCreate()
    {
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                typeof(Unit_PathCreatorRef_Component)
            }
        }));

        m_query = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                ComponentType.ReadWrite<Unit_Target_Component>(),
                ComponentType.ReadWrite<Unit_PathCreatorRef_Component>(),
                ComponentType.ReadWrite<Unit_States_Component>(),
                ComponentType.ReadOnly<Unit_Data_Component>(),
                ComponentType.ReadOnly<Unit_Position_Component>()
            }
        });

    }


    private struct UpdateFormationJob : IJobChunk
    {
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            throw new System.NotImplementedException();
        }
    }



    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities
        .WithName("Unit_UpdateFormation")
        .WithoutBurst()
        .ForEach((ref Unit_Target_Component utc, ref Unit_PathCreatorRef_Component upcrc, ref Unit_States_Component usc, in Unit_Data_Component udc, in Unit_Position_Component upc) =>
        {
            //Debug.DrawRay((Vector3)utc.position + Vector3.up * 5, Vector3.up, Color.red);


            if (usc.state == UnitState.MOVING || usc.state == UnitState.ESCAPING)
            {
                upcrc.distanceTravelled += udc.pathSpeed * deltaTime;

                var pathCreator = BezierPathManager.Instance[upcrc.instanceId];//.GetComponent<PathCreator>();
                var path = pathCreator.path;

                utc.position = path.GetPointAtDistance(upcrc.distanceTravelled, EndOfPathInstruction.Stop);
                utc.direction = path.GetDirectionAtDistance(upcrc.distanceTravelled, EndOfPathInstruction.Stop);

                if (path.GetClosestTimeOnPath(upc.position) > 0.97f)
                {
                    utc.position += (float3)path.GetDirectionAtDistance(upcrc.distanceTravelled, EndOfPathInstruction.Stop);
                    usc.state = UnitState.IDLE;
                }

            }
        }).Run(); // TODO : fix bezier path creator so that we can run this in parallel






    }
}
