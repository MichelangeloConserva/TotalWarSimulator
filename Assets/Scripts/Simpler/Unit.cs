using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class Unit : MonoBehaviour
{

    [System.Serializable]
    public class Soldier
    {
        public GameObject go;
        public Rigidbody rb;
        public int rowInd, colInd;
        public bool isCharging;
        public List<Vector3> trajectory;
        public List<FormationIndices> indices;

        public UnitStats stats;
        public Vector3 targetPos;

        public Soldier(GameObject g, List<FormationIndices> indices, UnitStats stats, List<Vector3> trajectory)
        {
            this.stats = stats;
            go = g;
            rb = go.GetComponent<Rigidbody>();
            Reset(trajectory, indices);
        }

        public void Reset(List<Vector3> trajectory, List<FormationIndices> indices)
        {
            this.indices = indices;
            this.trajectory = trajectory;
            targetPos = trajectory.ElementAt(0);
            rowInd = indices.ElementAt(0).rowInd;
            colInd = indices.ElementAt(0).colInd;
        }

        public void Update()
        {
            trajectory.RemoveAt(0);
            indices.RemoveAt(0);
            targetPos = trajectory.ElementAt(0);
            rowInd = indices.ElementAt(0).rowInd;
            colInd = indices.ElementAt(0).colInd;
        }

        public void Add(Vector3 target, FormationIndices ind)
        {
            trajectory.Add(target);
            indices.Add(ind);
        }

        public void SwapTrajectory(Soldier other)
        {
            List<Vector3> myTraj = new List<Vector3>(trajectory);
            List<Vector3> hisTraj = new List<Vector3>(other.trajectory);

            List<FormationIndices> myInd = new List<FormationIndices>(indices);
            List<FormationIndices> hisInd = new List<FormationIndices>(other.indices);

            Reset(hisTraj, hisInd);
            other.Reset(myTraj, myInd);
        }


        public void Move(float speed, float distance)
        {
            if (rb.velocity.magnitude < stats.topSpeed)
            {
                float dt = distance / speed;

                Vector3 p = rb.position;
                Vector3 v = rb.velocity;

                Vector3 force = rb.mass * (targetPos - p - v * dt) / dt;  // TODO : Damping is to taken into account

                rb.AddForce(isCharging ? Vector3.ClampMagnitude(force, stats.chargeForce) : Vector3.ClampMagnitude(force, stats.movementForce),
                            isCharging ? ForceMode.Impulse : ForceMode.Force);

                go.transform.LookAt(GetVector3Down(targetPos) * Random.Range(0.99f, 1.01f) + Vector3.up * 0.5f);  // TODO : Remove this hard coding here
            }
        }
        public void Move()
        {
            if (rb.velocity.magnitude < stats.topSpeed)
            {
                float dt = 0.02f;

                Vector3 p = rb.position;
                Vector3 v = rb.velocity;

                Vector3 force = rb.mass * (targetPos - p - v * dt) / dt;  // TODO : Damping is to taken into account

                rb.AddForce(isCharging ? Vector3.ClampMagnitude(force, stats.chargeForce) : Vector3.ClampMagnitude(force, stats.movementForce),
                            isCharging ? ForceMode.Impulse : ForceMode.Force);

                go.transform.LookAt(GetVector3Down(targetPos) + Vector3.up * 0.5f);  // TODO : Remove this hard coding here
            }
        }
    }

    [System.Serializable]
    public class UnitStats
    {
        public float topSpeed, chargeForce, movementForce, minDistanceForTargetPoint, distanceAfterFacingIncomingEnemy;
    }



    public Vector3 targetPos;
    public Vector3 targetDirection;

    public GameObject soldierBase, connector;
    public int cols;
    public int numOfSoldiers;
    public float soldierDistLateral;
    public float soldierDistVertical;
    public bool updateFormation;
    public UnitStats stats;

    public bool DEBUG_MODE;
    public GameObject[][] soldiersInFormation;
    public UnitStatus unitStatus;
    public bool withJoints = false;
    public Vector3 soldierLocalScale;


    private Vector3 position;            // updated at every fixed update
    private Soldier[] soldiers;      // TODO : update when soldiers die


    public Soldier[] GetSoldiers()
    {
        return soldiers;
    }
    public Vector3 GetPosition()
    {
        return position;
    }


    private void UpdateSoldiersList()
    {
        List<Soldier> soldiers = new List<Soldier>();
        for (int row = 0; row < soldiersInFormation.Length; row++)
            for (int col = 0; col < soldiersInFormation[row].Length; col++)
                soldiers.Add(new Soldier(soldiersInFormation[row][col], new List<FormationIndices> { new FormationIndices(row, col) }, stats, new List<Vector3> { soldiersInFormation[row][col].transform.position }));
        this.soldiers = soldiers.ToArray();
    }

    private void CleanUnit()
    {
        var tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
            DestroyImmediate(child.gameObject);
    }

    private void InstantiateUnit()
    {
        unitStatus = UnitStatus.IDLE;
        CleanUnit();

        var fRes = GetFormAtPos(transform.position, transform.forward, numOfSoldiers, cols, soldierDistLateral, soldierDistVertical);
        Vector3[][] formationPositions = fRes.positions;
        soldiersInFormation = new GameObject[formationPositions.Length][];
        for (int i = 0; i < formationPositions.Length; i++)
        {
            GameObject[] curRow = new GameObject[formationPositions[i].Length];
            for (int j = 0; j < curRow.Length; j++)
                curRow[j] = Instantiate(soldierBase, formationPositions[i][j], transform.rotation, transform);
            soldiersInFormation[i] = curRow;
        }
        UpdateSoldiersList();
    }



    private void Start()
    {
        if (Application.isPlaying)
        {
            InstantiateUnit();
            soldierLocalScale = soldiers[0].go.transform.localScale;
        }
        targetPos = transform.position;
        targetDirection = transform.forward;
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            if (updateFormation)
                InstantiateUnit();

            if (soldiers.Length > 0)
                position = soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / soldiers.Length;

        }
        else
        {
            if (DEBUG_MODE)
            {
                foreach (Soldier s in GetSoldiers())
                {
                    Debug.DrawLine(s.go.transform.position, s.targetPos);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        position = soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / soldiers.Length;

        var changed_soldiers_distances = new List<float>();
        float[] distances = new float[soldiers.Length];
        bool[] changed = new bool[soldiers.Length];
        int maxLenTraj = 0;
        int minLenTraj = 9999;
        bool updateSoldiers = false;

        for (int i=0; i<soldiers.Length; i++)
        {
            var s = soldiers.ElementAt(i);
            if (s.trajectory.Count > 0) updateSoldiers = true;
            distances[i] = Vector3.Distance(s.go.transform.position, s.targetPos);

            if (distances[i] < stats.minDistanceForTargetPoint && s.trajectory.Count > 1)
            {
                s.Update();
                changed[i] = true;
            }
            else
            {
                changed_soldiers_distances.Add(distances[i]);
            }
            maxLenTraj = Mathf.Max(s.trajectory.Count, maxLenTraj);
            minLenTraj = Mathf.Min(s.trajectory.Count, minLenTraj);
        }

        if (maxLenTraj - minLenTraj > 2)
            for (int i = 0; i < soldiers.Length; i++)
            {
                var s = soldiers.ElementAt(i);

                if (minLenTraj == s.trajectory.Count &&  s.trajectory.Count > 1)
                {
                    Debug.Log(maxLenTraj - s.trajectory.Count);
                    Debug.Log("Dd");
                    s.Update();
                    changed[i] = true;
                }
            }


        if (changed_soldiers_distances.Count == 0)
            changed_soldiers_distances = distances.ToList();

        float t = changed_soldiers_distances.Sum() / changed_soldiers_distances.Count() / 80;
        float min_dist = changed_soldiers_distances.Min();

        int k = 0;
        float cur_speed;
        foreach (Soldier s in soldiers)
        {

            cur_speed = min_dist / t * 0.25f;
            if (s.trajectory.Count == maxLenTraj)
                cur_speed = distances[k] / t;

            //t = distances[k] / stats.topSpeed;
            //if (s.trajectory.Count == maxLenTraj)
            //    t *= 1.5f;


            if (distances[k] > s.stats.minDistanceForTargetPoint)
                s.Move(cur_speed, distances[k]);
            else
            {
                if (s.trajectory.Count == 1 && distances[k] > 1)
                    s.Move(cur_speed, distances[k]);
            }

            //s.Move();
            k++;
        }

    }

}
