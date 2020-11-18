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



    public GameObject otherUnit;
    public bool IsDefending;
    public List<Soldier> soldiers;
    public FormationResult currentFormationResult;
    public FormationResult[] trajectory;


    private UnitCreator unit;
    private bool hasJoint = false;
    private float time;



    public List<Soldier> GetRowOfSoldiers(int r) { return soldiers.Where(s => s.rowInd == r).ToList(); }
    public int GetNumRows() { return soldiers.Aggregate(0, (acc, s) => Mathf.Max(acc, s.rowInd + 1)); }

    void Start()
    {
        time = Time.time;
        unit = GetComponent<UnitCreator>();


        currentFormationResult = unit.fRes;
        GameObject[][] formation = unit.soldiersInFormation;

        soldiers = new List<Soldier>();
        for (int row = 0; row < formation.Length; row++)
            for (int col = 0; col < formation[row].Length; col++)
                soldiers.Add(new Soldier(formation[row][col], formation[row][col].transform.position, row, col));

    }

    void Update()
    {
        // updating soldiers
        foreach (var s in soldiers)
        {
            float meanDistance = soldiers.Aggregate(0f, (acc, g) => acc + Vector3.Distance(s.go.transform.position, g.targetPos)) / soldiers.Count;
            MoveSoldiers(s, s.targetPos, meanDistance);

            Rigidbody rb = s.rb;
            Vector3 targetPos = s.targetPos;
            
            // Debbugging soldiers
            //Debug.DrawLine(s.go.transform.position + Vector3.up*5, s.targetPos, Color.yellow);
            //Debug.DrawRay(s.go.transform.position + Vector3.up * s.rowInd, s.go.transform.forward, Color.blue);
        }


        if (IsDefending)
        {
            Vector3 unitPosition = transform.Cast<Transform>().ToList().Aggregate(Vector3.zero, (acc, v) => acc + v.position) / transform.childCount;
            Vector3 otherUnitPosition = otherUnit.transform.Cast<Transform>().ToList().Aggregate(Vector3.zero, (acc, v) => acc + v.position) / transform.childCount;
            Vector3 closestEnemy = otherUnit.transform.Cast<Transform>().ToList().OrderBy(es => Vector3.Distance(transform.position, es.transform.position)).First().position;
            Vector3 closestAlly = transform.Cast<Transform>().ToList().OrderBy(es => Vector3.Distance(otherUnitPosition, es.transform.position)).First().position;

            if (Vector3.Distance(closestEnemy, closestAlly) < 10)
            {
                currentFormationResult = GetFormAtPos(unitPosition, otherUnitPosition - unitPosition, unit.numOfSoldiers, unit.cols, unit.soldierDistLateral, unit.soldierDistVertical);
                UpdateTargetPositionOfSoldiers(currentFormationResult, true);
                IsDefending = false;
            }
        }


        if(Time.time - time > 1.5f)
        {
            time = Time.time;
            UpdateTargetPositionOfSoldiers(currentFormationResult, true);
        }
    }





    public void UpdateTargetPositionOfSoldiers(FormationResult res, bool debug = false)
    {
        Vector3[] finalPositions = res.allPositions;
        Vector3[] currentSoldierPositions = soldiers.Select(p => p.go.transform.position).ToArray();

        int[] assignment = LSCAssignment(soldiers.ToArray(), finalPositions);
        for (int j = 0; j < unit.numOfSoldiers; j++)
        {
            // Debug.Log(assignment.Aggregate("", (acc, a) => acc + " " + a.ToString()));
            //  Debug.Log(soldiers.ElementAt(j).rowInd.ToString() + " : " + res.indices[assignment[j]].rowInd.ToString());

            if(!soldiers.ElementAt(j).isCharging)
                soldiers.ElementAt(j).Update(finalPositions[assignment[j]], res.indices[assignment[j]].rowInd, res.indices[assignment[j]].colInd);
        }
    }


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
                if (j>0)
                    AddHindge(soldiersFormation[i][j], soldiersFormation[i][j - 1]);
                if (i > 0)
                    AddSpringBetween(soldiersFormation[i - 1][j], soldiersFormation[i][j]);
            }
    }

    private void MoveSoldier(Soldier s, Vector3 targetPos, float refAcc)
    {
        //s.transform.LookAt(targetPos);
        //s.transform.position = s.transform.position + (targetPos - s.transform.position).normalized * Time.deltaTime * 3;


        Rigidbody rb = s.rb;

        if (rb.velocity.magnitude < 15)
        {
            float dt = Time.fixedDeltaTime;

            Vector3 p = rb.position;
            Vector3 v = rb.velocity;

            Vector3 force = rb.mass * (targetPos - p - v * dt) / dt;
            rb.AddForce(s.isCharging ? Vector3.ClampMagnitude(force, 1500) : Vector3.ClampMagnitude(force, 1500), 
                        s.isCharging ? ForceMode.Impulse                   : ForceMode.Force);
        }



        // We should use the other one
        //s.transform.LookAt(targetPos);
        //rb.velocity = s.transform.forward * refAcc;



        //s.transform.LookAt(targetPos);
        //// Force to move
        //var forwardSpeed = Vector3.Project(rb.velocity, s.transform.forward).magnitude;
        //if (Vector3.Angle(rb.velocity, s.transform.forward) > 90)
        //    forwardSpeed *= -1;

        //var accelleration = (refAcc - forwardSpeed) / Time.fixedDeltaTime * Random.Range(0.9f, 1.1f);
        //rb.AddForce(accelleration * s.transform.forward, ForceMode.Acceleration);

        //// Kill lateral velocity
        //var lateralVelocity = Vector3.Project(rb.velocity, s.transform.right);
        //lateralVelocity = Vector3.ClampMagnitude(lateralVelocity, 5f);
        //rb.AddForce(lateralVelocity, ForceMode.Acceleration);



    }

    private void MoveSoldiers(Soldier s, Vector3 targetPos, float meanDistance)
    {
        Rigidbody rb = s.rb;
        float rbSpeed = rb.velocity.magnitude;
        if (Vector3.Distance(s.go.transform.position, targetPos) > 0.01f)
        {
            // anim.SetBool("IsMoving", true);


            // TODO : add as a public variable
            float dist = Vector3.Distance(transform.position, targetPos);
            var t = dist/8;
            var v_f = (2 * dist) / t - rbSpeed - rbSpeed;
            var acc = (v_f - rbSpeed) / t;
            MoveSoldier(s, targetPos, acc);

        }
        //else if (rbSpeed > 2f)
        //{
        //    var accelleration = -rbSpeed / Time.fixedDeltaTime / 2;
        //    rb.AddForce(accelleration * s.transform.forward, ForceMode.Acceleration);

        //    // anim.SetBool("IsMoving", false);
        //}
        //else if (Vector3.Distance(s.transform.position, targetPos) > 0.1f)
        //{
        //    //var topSpeed = 0.5f;
        //    //MoveSoldier(s, rb, targetPos, topSpeed);
        //}
        

        if (meanDistance < 0.11f)
        {
            Debug.Log("Good point to have springs");

            if (!hasJoint)
            {
                hasJoint = true;
                // AddJoints(soldiersInFormation);
            }
        }


    }



}
