using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static C_Unit;
using static Utils;


[ExecuteInEditMode]
public class HumanController : MonoBehaviour
{

    public float acceleration;

    public GameObject unitToAttack;


    private bool first = false;
    private C_Unit unitController;

    // Start is called before the first frame update
    void Start()
    {
        unitController = GetComponent<C_Unit>();

    }





    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            unitController.unitStatus = UnitStatus.CHARGING;
            unitController.AttackUnit(unitController.enemyUnits.First());
        }



        //if (!Application.isPlaying)
        //{

        //    var enemy = unitToAttack.GetComponent<UnitCreator>();
        //    var closestEnemy = enemy.GetSoldiers().OrderBy(es => Vector3.Distance(transform.position, es.transform.position)).First();


        //    var diameter = GetComponent<UnitCreator>().soldierBase.transform.localScale.x;
        //    var dir = closestEnemy.transform.position - transform.position;
        //    var targetPos = transform.position + dir.normalized * (dir.magnitude);// + diameter);


        //    var rowDir = Vector3.Cross(Vector3.up, dir.normalized);
        //    //Debug.DrawLine(transform.position + Vector3.up, closestEnemy.transform.position + Vector3.up, Color.green);
        //    //Debug.DrawLine(transform.position + Vector3.up * 2, targetPos + Vector3.up * 2, Color.blue);
        //    //Debug.DrawRay(targetPos + Vector3.up * 2, rowDir, Color.blue);


        //    var unit = GetComponent<UnitCreator>();
        //    var hl = GetHalfLenght(unit.soldierDistVertical, GetNumRows(unit.numOfSoldiers, unit.cols));

        //    targetPos = targetPos + (transform.position - targetPos).normalized * hl;
        //    var lineAttack = GetFormAtPos(targetPos, dir, unit.numOfSoldiers, unit.cols, unit.soldierDistLateral, unit.soldierDistVertical);
        //    foreach (var v in lineAttack.allPositions)
        //            Debug.DrawRay(v + Vector3.up, Vector3.up, Color.red);


        //    targetPos = targetPos + (transform.position - targetPos).normalized * hl;

        //    var res = GetFormAtPos(targetPos, dir, unit.numOfSoldiers, unit.cols, unit.soldierDistLateral, unit.soldierDistVertical);
        //}
        //else
        //{
        //    //var angle = Input.GetAxis("Horizontal");
        //    //var speed = Input.GetAxis("Vertical");

        //    //for (int i = 0; i < transform.childCount; i++)
        //    //{
        //    //    var rb = transform.GetChild(i).GetComponent<Rigidbody>();
        //    //    if (!rb) continue;
        //    //    if (rb.velocity.magnitude < 15)
        //    //        rb.AddForce(transform.forward * speed * acceleration, ForceMode.Force);
        //    //}

        //    //transform.Rotate(Vector3.up, angle);

        //    //var soldiers = GetComponent<UnitCreator>().soldiers;



        //    // Vector3 mouseClick = GetMousePosInWorld();
        //    //if (unitToAttack)
        //    //    unitToAttack.GetComponent<UnitController>().MoveAtPos(mouseClick);
        //    var unitController = GetComponent<C_Unit>();

        //    if (!first)
        //    {
        //        first = true;

        //        var enemy = unitToAttack.GetComponent<UnitCreator>();
        //        var closestEnemy = enemy.GetSoldiers().OrderBy(es => Vector3.Distance(transform.position, es.transform.position)).First();
        //        Debug.DrawLine(transform.position + Vector3.up, closestEnemy.transform.position + Vector3.up, Color.green);


        //        var diameter = GetComponent<UnitCreator>().soldierBase.transform.localScale.x;
        //        var dir = closestEnemy.transform.position - transform.position;
        //        var targetPos = transform.position + dir.normalized * (dir.magnitude);// + diameter);
        //        Debug.DrawLine(transform.position + Vector3.up * 2, targetPos + Vector3.up * 2, Color.blue);


        //        var rowDir = Vector3.Cross(Vector3.up, dir.normalized);
        //        Debug.DrawRay(targetPos + Vector3.up * 2, rowDir, Color.blue);


        //        var unit = GetComponent<UnitCreator>();
        //        var hl = GetHalfLenght(unit.soldierDistVertical, GetNumRows(unit.numOfSoldiers, unit.cols));


        //        targetPos = targetPos + (transform.position - targetPos).normalized * hl;
        //        var res = GetFormAtPos(targetPos, targetPos - transform.position, unit.numOfSoldiers, unit.cols, unit.soldierDistLateral, unit.soldierDistVertical);
                
        //        unitController.UpdateTargetPositionOfSoldiers(res);
        //        unitController.currentFormationResult = res;
        //    }





        //    // Check if the current FormationResult has been fulfilled
        //    var meandistance = unitController.soldiers.Aggregate(0f, (acc, s) => acc + Vector3.Distance(s.go.transform.position, s.targetPos)) / unitController.soldiers.Count;

        //    if (!charge && meandistance < 0.15f)
        //    {
        //        charge = true;

        //        for (int j =0; j < unitController.GetNumRowsOfUnit(); j++)
        //        {
        //            var rowSoldiers = unitController.GetRowOfSoldiers(j).ToArray();
        //            for (int i = 1; i < rowSoldiers.Length; i++)
        //                unitController.AddHindge(rowSoldiers[i - 1].go, rowSoldiers[i].go);
        //            foreach (var g in rowSoldiers)
        //            {
        //                g.isCharging = true;
        //                StartCoroutine(FollowTrajectory(new SoldierTrajectory(g, new Vector3[] { g.go.transform.position + g.go.transform.forward * 4, g.targetPos })));
        //            }
        //        }

        //    }

        //}

    }


    
    private IEnumerator FollowTrajectory(SoldierTrajectory sTraj)
    {
        sTraj.s.targetPos = sTraj.trajectory.First();
        yield return new WaitUntil(() => Vector3.Distance(sTraj.s.go.transform.position, sTraj.s.targetPos) < 0.15f);

        if (sTraj.trajectory.Length > 1)
            StartCoroutine(FollowTrajectory(new SoldierTrajectory(sTraj.s, sTraj.trajectory.Skip(1).ToArray())));
        
        sTraj.s.isCharging = false;
        yield return null;


        // IN THIS PIECE OF CODE THE isCharging VARIABLE IS REMOVE JUST AFTER THE FIRST CHARGE
        //if (sTraj.trajectory.Length > 1)
        //{
        //    sTraj.s.isCharging = false;
        //    StartCoroutine(FollowTrajectory(new SoldierTrajectory(sTraj.s, sTraj.trajectory.Skip(1).ToArray())));
        //}
        //yield return null;

    }

    





}
