using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Physics.Math;
using static Utils;
using PathCreation;
using System.Linq;

[InternalBufferCapacity(100)]
public struct Trajectory_Element : IBufferElementData
{
    public float3 pos;
}
public struct Human_Component : IComponentData
{

}


public class Human : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Human_Component { });
        dstManager.AddBuffer<Trajectory_Element>(entity);
    }
}




public class Mouse_Click_System : SystemBase
{
    BeginInitializationEntityCommandBufferSystem ei_ECB;
    private PhysicsWorld physicsWorld => World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
    private EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
    private Camera Cam = Camera.main;


    private Entity selectedUnit;


    private void ChangeUnitColor(in DynamicBuffer<SoldierReference> solds, EntityCommandBuffer ecb, bool highlight)
    {

        for (int i = 0; i < solds.Length; i++)
        {

            var children = entityManager.GetBuffer<LinkedEntityGroup>(solds[i].soldierRef);
            var renderMesh = entityManager.GetSharedComponentData<RenderMesh>(children[1].Value);

            var mat = new UnityEngine.Material(renderMesh.material);
            var color = mat.color;
            if (highlight)
                color.b = 1;
            else
                color.b = 0.21f;
            mat.SetColor("_Color", color);
            renderMesh.material = mat;

            ecb.SetSharedComponent(children[1].Value, renderMesh);
        }
    }



    protected override void OnCreate()
    {
        ei_ECB = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (!Cam)
            Cam = Camera.main;


        var ecb = ei_ECB.CreateCommandBuffer();

        if (Input.GetMouseButtonDown(0))
        {

            var screenPointToRay = Cam.ScreenPointToRay(Input.mousePosition);
            var rayInput = new RaycastInput
            {
                Start = screenPointToRay.origin,
                End = screenPointToRay.GetPoint(1000),
                Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u }  // COLLISION WITH THE TERRAIN
            };
            if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
            {
                var mouseClickPos = GetVector3Down(hit.Position);

                // Deselect the unit
                if (selectedUnit != Entity.Null)
                    ChangeUnitColor(entityManager.GetBuffer<SoldierReference>(selectedUnit), ecb, false);
                selectedUnit = Entity.Null;
                
                Entities
                .WithoutBurst()
                .WithAll<Unit_Data_Component>()
                .ForEach((in Entity unit, in Unit_Position_Component upc, in DynamicBuffer<SoldierReference> solds) =>
                {
                    if (math.distance(upc.position, mouseClickPos) < 3)  // TODO :Should be done with Sphere collision / Box cast
                    {
                        selectedUnit = unit;
                        ChangeUnitColor(solds, ecb, true);
                    }
                }).Run();

            }

        }

        

        if (selectedUnit != Entity.Null)
        {

            if (Input.GetMouseButtonDown(1))
            {
            }


            // CREATING THE MOUSE TRAJECTORY
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(1))
            {
                var screenPointToRay = Cam.ScreenPointToRay(Input.mousePosition);
                var rayInput = new RaycastInput
                {
                    Start = screenPointToRay.origin,
                    End = screenPointToRay.GetPoint(1000),
                    Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u }  // COLLISION WITH THE TERRAIN
                };
                if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
                {
                    var mouseClickPos = GetVector3Down(hit.Position);
                    Entities
                    .WithoutBurst()
                    .WithAll<Human_Component>()
                    .ForEach((ref DynamicBuffer<Trajectory_Element> mt) =>
                    {
                        if (mt.Length < mt.Capacity && (mt.Length == 0 || math.distance(mouseClickPos, mt[mt.Length - 1].pos) > 4))
                            mt.Add(new Trajectory_Element { pos = mouseClickPos });
                    }).Run();
                }
            }


            // SENDING THE MOUSE TRAJECTORY
            if (Input.GetMouseButtonUp(1))
            {
                Entities
                .WithoutBurst()
                .WithAll<Human_Component>()
                .ForEach((ref DynamicBuffer<Trajectory_Element> mt) =>
                {
                    if (mt.Length > 2)
                    {

                        var usc = entityManager.GetComponentData<Unit_States_Component>(selectedUnit);
                        usc.state = UnitState.MOVING;
                        entityManager.SetComponentData(selectedUnit, usc);


                        var upcrc = entityManager.GetComponentData<Unit_PathCreatorRef_Component>(selectedUnit);
                        upcrc.distanceTravelled = 0;  // Resetting the distance travelled variable
                        entityManager.SetComponentData(selectedUnit, upcrc);
                        int goID = upcrc.instanceId;

                        var pathCreator = BezierPathManager.Instance[goID].GetComponent<PathCreator>();

                        var traj = mt.Reinterpret<Vector3>().ToNativeArray(Allocator.Temp).ToList();
                        traj.Insert(0, entityManager.GetComponentData<Unit_Position_Component>(selectedUnit).position);

                        pathCreator.bezierPath = new BezierPath(traj, false, PathSpace.xyz);

                    }
                    mt.Clear();
                }).Run();
            }

        }


        ei_ECB.AddJobHandleForProducer(Dependency);

    }




    //var ecb = ei_ECB.CreateCommandBuffer().AsParallelWriter();
    //float3 mouseClickPos = GetMousePositionOnTerrain();

    //Entities
    //.WithoutBurst()
    //.ForEach((ref DynamicBuffer<MouseTrajectory> mt) =>
    //{
    //    if (mt.Length == 0)



    //}).Run();
    //ei_ECB.AddJobHandleForProducer(Dependency);


}
//using System.Collections;
 //using System.Collections.Generic;
 //using System.Linq;
 //using Unity.Collections;
 //using Unity.Entities;
 //using Unity.Mathematics;
 //using Unity.Physics;
 //using Unity.Physics.Systems;
 //using Unity.Rendering;
 //using UnityEngine;
 //using static Utils;




//public class Human : MonoBehaviour
//{
//    public static readonly float RAYCAST_DISTANCE = 1000;
//    public RectTransform selectionBox;
//    public string teamID;

//    public CollisionFilter collFilter;

//    public List<Vector3> mouseTraj;

//    //public CUnit selectedUnit;

//    private PhysicsWorld physicsWorld => World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
//    private EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

//    private Camera Cam;
//    private Vector3 mouseClick, startMouseClick;
//    private Vector3 startMousePos, endMousePos;
//    private Vector3 diff;



//    void Start()
//    {
//        Cam = Camera.main;
//    }

//    void LateUpdate()
//    {

//        // SELECTION
//        if (Input.GetMouseButtonDown(0))
//        {
//            startMousePos = Input.mousePosition;
//            startMouseClick = GetMousePositionOnTerrain();
//        }

//        if (Input.GetMouseButton(0))
//            selectionClick(false);   // TODO : implement group selection

//        if (Input.GetMouseButtonUp(0))
//            selectionBox.gameObject.SetActive(false);


//        //if (!selectedUnit) return;

//        // FORMATION 
//        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(1))
//        {
//            mouseClick = GetMousePosInWorld();
//            if (mouseTraj.Count == 0 || Vector3.Distance(mouseClick, mouseTraj.Last()) > 3)
//                mouseTraj.Add(mouseClick);
//        }

//        //if (Input.GetMouseButtonUp(1))
//        //{
//        //    if (mouseTraj.Count > 4)
//        //    {
//        //        selectedUnit.MoveAt(mouseTraj);
//        //        mouseTraj.Clear();
//        //    }
//        //}

//        //if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftShift))
//        //{
//        //    Vector3 mouseClick = GetMousePosInWorld();
//        //    selectedUnit.MoveAt(mouseClick);
//        //}

//    }
//    //void LateUpdate()
//    //{
//    //    if (!Input.GetMouseButtonDown(0) || Cam == null) return;

//    //    var screenPointToRay = Cam.ScreenPointToRay(Input.mousePosition);
//    //    var rayInput = new RaycastInput
//    //    {
//    //        Start = screenPointToRay.origin,
//    //        End = screenPointToRay.GetPoint(RAYCAST_DISTANCE),
//    //        Filter = new CollisionFilter { BelongsTo = ~0u , CollidesWith = 1u }  // COLLISION WITH THE TERRAIN
//    //    };

//    //    if (!physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
//    //    {
//    //        Debug.Log("FAIL");
//    //        return;
//    //    }

//    //    var selectedEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
//    //    var renderMesh = entityManager.GetSharedComponentData<RenderMesh>(selectedEntity);
//    //    var mat = new UnityEngine.Material(renderMesh.material);
//    //    mat.SetColor("_Color", UnityEngine.Random.ColorHSV());
//    //    renderMesh.material = mat;

//    //    entityManager.SetSharedComponentData(selectedEntity, renderMesh);
//    //}


//    private Vector3 GetMousePositionOnTerrain()
//    {
//        var screenPointToRay = Cam.ScreenPointToRay(Input.mousePosition);
//        var rayInput = new RaycastInput
//        {
//            Start = screenPointToRay.origin,
//            End = screenPointToRay.GetPoint(RAYCAST_DISTANCE),
//            Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u }  // COLLISION WITH THE TERRAIN
//        };


//        if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
//        {
//            Debug.DrawRay(hit.Position, Vector3.up * 3, Color.red, 10);
//        }

//        return GetVector3Down(hit.Position);
//    }





//    private void selectionClick(bool DEBUG_MODE = false)
//    {
//        endMousePos = Input.mousePosition;

//        Unity.Physics.RaycastHit[] hits;
//        if (Vector3.Distance(startMousePos, endMousePos) > 2)
//        {    // Box selection
//            selectionBox.gameObject.SetActive(true);

//            diff = endMousePos - startMousePos;
//            selectionBox.sizeDelta = new Vector2(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
//            if (diff.x > 0)
//                selectionBox.anchoredPosition = new Vector3(startMousePos.x + (diff.x / 2), startMousePos.y + (diff.y / 2), 0);
//            else
//                selectionBox.anchoredPosition = new Vector3(endMousePos.x - (diff.x / 2), endMousePos.y - (diff.y / 2), 0);

//            diff = GetMousePositionOnTerrain() - startMouseClick;
//            hits = mouseHits(Mathf.Abs(diff.x / 2f), Mathf.Abs(diff.z / 2f), DEBUG_MODE);



//        }
//        else // Single click selection
//            hits = mouseHits(1, 1, DEBUG_MODE);


//        //if (hits.Length > 0)
//        //{
//        //    // TODO : implement group selection
//        //    if (selectedUnit)
//        //        selectedUnit.unit.isSelected = false;
//        //    selectedUnit = null;

//        //    selectedUnit = hits[0].collider.transform.parent.gameObject.GetComponent<CUnit>();
//        //    selectedUnit.unit.isSelected = true;
//        //}
//        //else
//        //{
//        //    if (selectedUnit)
//        //        selectedUnit.unit.isSelected = false;
//        //    selectedUnit = null;
//        //}

//    }



//    private Unity.Physics.RaycastHit[] mouseHits(float width, float height, bool DEBUG_MODE = false)
//    {
//        var click = startMouseClick * 0.5f + GetMousePosInWorld() * 0.5f;
//        //UnityEngine.RaycastHit[] hits = Physics.BoxCastAll(
//        //                           click,
//        //                           new Vector3(width, height, 900),
//        //                           Vector3.up,
//        //                           Quaternion.LookRotation(Vector3.up),
//        //                           900,
//        //                           LayerMask.GetMask("unitSoldier" + teamID)
//        //                       );
//        //if (DEBUG_MODE)
//        //{
//        //    Debug.Log(hits.Length);

//        //    DrawBox(click,
//        //        new Vector3(width, height, 900),
//        //        Quaternion.LookRotation(Vector3.up),
//        //        Color.yellow,
//        //        0.1f);
//        //}

//        //return hits;
//        //var boxCollider = Unity.Physics.BoxCollider.Create(new BoxGeometry
//        //{
//        //    Center = click,
//        //    Orientation = quaternion.identity,
//        //    Size = new float3(width, height, 900),
//        //    BevelRadius = 0.0f
//        //});

//        //ColliderCastInput input = new ColliderCastInput()
//        //{
//        //    Collider = (boxCollider*)(boxCollider.GetUnsafePtr())
//        //};


//        //Position = startMouseClick * 0.5f + pos * 0.5f,
//        //    Orientation = quaternion.identity,
//        //    Direction = new float3(0, 1, 0),
//        //    Collider = (Collider*)boxCollider.GetUnsafePtr()
//    }



//}
