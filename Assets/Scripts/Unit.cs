using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;

using static Utils;



public struct Unit_Initializer_Component : IComponentData
{
    public Entity boxPrefab;
    public float3 startPosition;
    public float3 startDirection;
    public int startNumOfSoldiers, startCols;
    public float lateralDist, verticalDist;
}
public struct Unit_Position_Component : IComponentData
{
    public float3 position;
}
public struct Unit_Target_Component : IComponentData
{
    public float3 position, direction;
}
public struct Unit_Data_Component : IComponentData
{
    public int numOfSoldiers, cols;
    public float lateralDist, verticalDist, pathSpeed, soldierSpeed;
    public int unitTeamID;
    public int unitID;
}
public struct Unit_PathCreatorRef_Component : IComponentData
{
    public int instanceId;
    public float distanceTravelled;
}
[InternalBufferCapacity(200)] // TODO : this should be the maximum number of soldiers of the units
public struct SoldierReference : IBufferElementData
{
    public Entity soldierRef;
}
public struct Unit_States_Component : IComponentData
{
    public UnitState state;
    public UnitCombactState combactState;
    public UnitMovementState movementState;
}
public struct Unit_Attacker_Component : IComponentData { }
public struct Unit_Defender_Component : IComponentData { }
public struct Unit_Targets_Component : IComponentData
{
    public Entity fightingTarget, commandTarget;
}


public struct Soldier_ReferenceToUnit_Component : ISharedComponentData
{
    public int unitID;
}
public struct Soldier_Targets_Component : IComponentData
{
    public float3 targetPosition, targetLookAt;
}
public struct Soldier_Speed_Component : IComponentData
{
    public float curSpeed;
}



public class Unit : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public static int UNIT_ID = 0;

    public GameObject boxPrefab;
    public int numOfSoldiers, cols, teamID;
    public float lateralDist, verticalDist, pathSpeed, soldierSpeed;

    public GameObject pathCreatorGO;
    public Transform pathsHolder;


    private void OnDrawGizmos()
    {
        if(!Application.isPlaying)
        {
            var formationPositions = GetFormationAtPos(transform.position, transform.forward, numOfSoldiers, cols, lateralDist, verticalDist);

            if (teamID == 0)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.green;

            foreach (var v in formationPositions)
                Gizmos.DrawSphere(v, 0.1f);
        }
    }

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(boxPrefab);
    }

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {

        dstManager.AddComponentData(
            entity,
            new Unit_Initializer_Component
            {
                boxPrefab = conversionSystem.GetPrimaryEntity(boxPrefab),
                startPosition = transform.position,
                startDirection = transform.forward,
                startNumOfSoldiers = numOfSoldiers,
                startCols = cols,
                lateralDist = lateralDist,
                verticalDist = verticalDist
            });

        dstManager.AddComponentData(
            entity,
            new Unit_Position_Component
            {
                position = transform.position
            });

        dstManager.AddComponentData(
            entity,
            new Unit_Target_Component
            {
                position = transform.position,
                direction = transform.forward
            });

        dstManager.AddComponentData(
            entity,
            new Unit_Data_Component
            {
                numOfSoldiers = numOfSoldiers,
                cols = cols,
                lateralDist = lateralDist,
                verticalDist = verticalDist,
                unitTeamID = teamID,
                pathSpeed = pathSpeed,
                soldierSpeed = soldierSpeed,
                unitID = GetInstanceID()
            });

        var pathGO = BezierPathManager.Instance.CreateFromPrefab(pathCreatorGO, pathsHolder, true);
        dstManager.AddComponentData(
            entity,
            new Unit_PathCreatorRef_Component
            {
                instanceId = pathGO.GetInstanceID(),
                distanceTravelled = 0
            });

        dstManager.AddComponentData(
            entity,
            new Unit_States_Component
            {
                state = UnitState.IDLE,
                combactState = UnitCombactState.DEFENDING,
                movementState = UnitMovementState.WALKING
            });

        dstManager.AddComponentData(
            entity,
            new Unit_Targets_Component
            {
                commandTarget = Entity.Null,
                fightingTarget = Entity.Null
            });



        if (teamID == 0)
            dstManager.AddComponentData(entity, new Unit_Attacker_Component { });
        else
            dstManager.AddComponentData(entity, new Unit_Defender_Component { });



    }

}
