using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;



public class C_Unit : MonoBehaviour
{

    [System.Serializable]
    public class Soldier
    {
        public GameObject go;
        public Rigidbody rb;
        public Vector3 targetPos;
        public int rowInd, colInd;
        public bool isCharging;

        public Soldier(GameObject g, Vector3 tg, int r, int c)
        {
            go = g;
            targetPos = tg;
            rowInd = r; colInd = c;
            rb = go.GetComponent<Rigidbody>();
        }
        
        public void Update(Vector3 tg, int r, int c) { targetPos = tg; colInd = c; rowInd = r; }
    }

    public struct SoldierTrajectory
    {
        public C_Unit.Soldier s;
        public Vector3[] trajectory;
        public SoldierTrajectory(C_Unit.Soldier s, Vector3[] traj) { this.s = s; trajectory = traj; }
    }

    [System.Serializable]
    public class UnitStats
    {
        public float topSpeed, chargeForce, movementForce, minDistanceForTargetPoint, distanceAfterFacingIncomingEnemy;
    }


    public UnitState unitStatus;
    public float targetsUpdateEverySec;
    public UnitStats stats;


    public List<GameObject> enemyUnits;
    public GameObject currentFightingEnemyUnit;
    public Vector3 unitPosition;

    public bool IsDefending;
    public List<Soldier> soldiers;
    public FormationResult currentFormationResult;
    public FormationResult[] trajectory;


    private UnitCreator unit;
    private bool activeUpdateAtTimeIntervals;


    public List<Soldier> GetRowOfSoldiers(int r) { return soldiers.Where(s => s.rowInd == r).ToList(); }
    public int GetNumRowsOfUnit() { return soldiers.Aggregate(0, (acc, s) => Mathf.Max(acc, s.rowInd + 1)); }
    public float GetSoldiersMeanDistanceToTarget() { return soldiers.Aggregate(0f, (acc, s) => acc + Vector3.Distance(s.go.transform.position, s.targetPos)) / soldiers.Count; }


    void Start()
    {
        unitStatus = UnitState.IDLE;

        unitPosition = soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / soldiers.Count;

        unit = GetComponent<UnitCreator>();

        currentFormationResult = unit.fRes;
        GameObject[][] formation = unit.soldiersInFormation;

        soldiers = new List<Soldier>();
        for (int row = 0; row < formation.Length; row++)
            for (int col = 0; col < formation[row].Length; col++)
                soldiers.Add(new Soldier(formation[row][col], formation[row][col].transform.position, row, col));

        activeUpdateAtTimeIntervals = true;
        StartCoroutine(UpdateTargetPositionOfSoldiersOnInterval());
    }

    private void FixedUpdate()
    {
        unitPosition = soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / soldiers.Count;
    }

    private bool charge = false;
    void Update()
    {
        EnemyUnitsChecking();
        SoldiersUpdate();
    }


    public void AttackUnit(GameObject enemy)
    {
        currentFightingEnemyUnit = enemy;
        FaceEnemyUnit(enemy, true);
        UpdateTargetPositionOfSoldiers();
        StartCoroutine(PerformCharge());
    }

    private void FaceEnemyUnit(GameObject enemy, bool isAttacking)
    {
        C_Unit enemyUnit = enemy.GetComponent<C_Unit>();
        var closestEnemy = enemyUnit.soldiers.OrderBy(s => Vector3.Distance(unitPosition, s.go.transform.position)).First();
        var diameter = unit.soldierBase.transform.localScale.z;
        var dir = closestEnemy.go.transform.position - unitPosition;
        var targetPos = unitPosition;
        if (isAttacking) // when false is BUGGED CHEKC PLS
            targetPos += dir.normalized * (dir.magnitude);// + diameter);
        var hl = GetHalfLenght(unit.soldierDistVertical, GetNumRows(unit.numOfSoldiers, unit.cols));
        targetPos = targetPos + (transform.position - targetPos).normalized * hl;
        Debug.DrawLine(unitPosition, closestEnemy.go.transform.position + Vector3.up * 5, Color.red, 100);
        currentFormationResult = GetFormAtPos(targetPos, dir, unit.numOfSoldiers, unit.cols, unit.soldierDistLateral, unit.soldierDistVertical);
        UpdateTargetPositionOfSoldiers();
    }
    private void FaceEnemyUnit(bool isAttacking) { FaceEnemyUnit(currentFightingEnemyUnit, isAttacking); }

    private IEnumerator PerformCharge()
    {
        yield return new WaitUntil(() => soldiers.Aggregate(0f, (acc, s) => acc + Vector3.Distance(s.go.transform.position, s.targetPos)) / soldiers.Count < 0.15f);


        //C_Unit enemyUnit = currentFightingEnemyUnit.GetComponent<C_Unit>();
        //if (!enemyUnit.currentFightingEnemyUnit)
        //    enemyUnit.currentFightingEnemyUnit = gameObject;
        //if (enemyUnit.currentFightingEnemyUnit == gameObject)
        //    enemyUnit.FaceEnemyUnit(false);


        float chargeDepth = 2; // TODO change this value

        for (int j = 0; j < GetNumRowsOfUnit(); j++)
        {
            var rowSoldiers = GetRowOfSoldiers(j).ToArray();

            // Add hindges to maintain rows connected during charges 
            for (int i = 0; i < rowSoldiers.Length; i++)
            {
                rowSoldiers[i].isCharging = true;
                StartCoroutine(FollowTrajectory(new SoldierTrajectory(rowSoldiers[i], new Vector3[] { rowSoldiers[i].go.transform.position + rowSoldiers[i].go.transform.forward * chargeDepth, rowSoldiers[i].targetPos })));
                if (i > 0)
                    AddHindge(rowSoldiers[i - 1].go, rowSoldiers[i].go);
            }
        }
    }

    private void EnemyUnitsChecking()
    {
        foreach (GameObject eUnit in enemyUnits)
        {
            Vector3 otherUnitPosition = eUnit.GetComponent<C_Unit>().unitPosition;

            if (!currentFightingEnemyUnit && unitStatus == UnitState.IDLE && Vector3.Distance(unitPosition, otherUnitPosition) < stats.distanceAfterFacingIncomingEnemy)
            {
                currentFightingEnemyUnit = eUnit;
                FaceEnemyUnit(false);
            }
        }
    }



    #region Function for soldiers
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
    private IEnumerator UpdateTargetPositionOfSoldiersOnInterval()
    {
        while (true)
        {
            if (activeUpdateAtTimeIntervals)
                UpdateTargetPositionOfSoldiers();
            yield return new WaitForSeconds(targetsUpdateEverySec);
        }
    }
    public void UpdateTargetPositionOfSoldiers()
    {
        Vector3[] finalPositions = currentFormationResult.allPositions;
        Vector3[] currentSoldierPositions = soldiers.Select(p => p.go.transform.position).ToArray();

        int[] assignment = LSCAssignment(soldiers.ToArray(), finalPositions);
        for (int j = 0; j < unit.numOfSoldiers; j++)
            if(!soldiers.ElementAt(j).isCharging)
                soldiers.ElementAt(j).Update(finalPositions[assignment[j]], currentFormationResult.indices[assignment[j]].rowInd, currentFormationResult.indices[assignment[j]].colInd);
    }
    private void MoveSoldier(Soldier s, Vector3 targetPos)
    {
        Rigidbody rb = s.rb;

        if (Vector3.Distance(s.go.transform.position, targetPos) > stats.minDistanceForTargetPoint && rb.velocity.magnitude < stats.topSpeed)
        {
            float dt = Time.fixedDeltaTime;

            Vector3 p = rb.position;
            Vector3 v = rb.velocity;

            Vector3 force = rb.mass * (targetPos - p - v * dt) / dt;
            rb.AddForce(s.isCharging ? Vector3.ClampMagnitude(force, stats.chargeForce) : Vector3.ClampMagnitude(force, stats.movementForce), 
                        s.isCharging ? ForceMode.Impulse                   : ForceMode.Force);
        }
    }
    private void SoldiersUpdate()
    {
        foreach (var s in soldiers)
        {
            MoveSoldier(s, s.targetPos);
            // Debbugging soldiers
            //Debug.DrawLine(s.go.transform.position + Vector3.up*5, s.targetPos, Color.yellow);
            //Debug.DrawRay(s.go.transform.position + Vector3.up * s.rowInd, s.go.transform.forward, Color.blue);
        }
    }
    #endregion


    #region Functions for Joints
    public GameObject AddHindge(GameObject a, GameObject b)
    {
        var curConnector = Instantiate(unit.connector, a.transform.position * 0.5f + b.transform.position * 0.5f, Quaternion.identity, transform);
        var hindges = curConnector.GetComponents<HingeJoint>();
        hindges[0].connectedBody = a.GetComponent<Rigidbody>();
        hindges[1].connectedBody = b.GetComponent<Rigidbody>();
        return curConnector;
    }
    private void AddSpringBetween(GameObject g1, GameObject g2, int springForce = 5000, int damperForce = 100)
    {
        g1.AddComponent<SpringJoint>();
        var spring = g1.GetComponent<SpringJoint>();
        spring.connectedBody = g2.GetComponent<Rigidbody>();
        spring.anchor = g1.transform.InverseTransformPoint((g1.transform.position + g2.transform.position) * 0.5f);
        spring.spring = springForce;
        spring.damper = damperForce;
    }
    private void AddJoints(GameObject[][] soldiersFormation)
    {
        for (int i = 0; i < soldiersFormation.Length; i++)
            for (int j = 0; j < soldiersFormation[i].Length; j++)
            {
                if (j > 0)
                    AddHindge(soldiersFormation[i][j], soldiersFormation[i][j - 1]);
                if (i > 0)
                    AddSpringBetween(soldiersFormation[i - 1][j], soldiersFormation[i][j]);
            }
    }
    #endregion

}
