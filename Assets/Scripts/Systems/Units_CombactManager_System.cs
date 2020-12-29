using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Utils;


public class Units_CombactManager_System : SystemBase
{
    EntityQuery attacker_query, defender_query;

    protected override void OnCreate()
    {

        attacker_query = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                typeof(Unit_Attacker_Component)
            }
        });
        defender_query = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[]
            {
                typeof(Unit_Defender_Component)
            }
        });

    }


    public struct FillPosistion : IJobChunk
    {
        NativeArray<float3> positions;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            throw new System.NotImplementedException();
        }
    }




    protected override void OnUpdate()
    {

        NativeArray<float3> attackerPositions = new NativeArray<float3>(attacker_query.CalculateEntityCount(), Allocator.Temp);
        NativeArray<float3> defenderPositions = new NativeArray<float3>(defender_query.CalculateEntityCount(), Allocator.Temp);




        Entities
            .ForEach((Entity e, int entityInQueryIndex, in Unit_Position_Component upc, in Unit_Attacker_Component uac) =>
            {
                attackerPositions[entityInQueryIndex] = upc.position;
            }).Run();

        Entities
            .ForEach((Entity e, int entityInQueryIndex, in Unit_Position_Component upc, in Unit_Defender_Component udc) =>
            {
                defenderPositions[entityInQueryIndex] = upc.position;
            }).Run();


        float[,] prova = new float[attackerPositions.Length, defenderPositions.Length];
        for (int i = 0; i < attackerPositions.Length; i++)
            for (int j = 0; j < defenderPositions.Length; j++)
            {
                prova[i, j] = math.distance(attackerPositions[i], defenderPositions[j]);
                Debug.Log(prova[i, j]);
            }














    }
}
