using PathCreation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class UnitTarget : MonoBehaviour
{

    public Unit unit;
    public float speed = 5;
    public bool isClosedLoop = false;


    public PathCreator pathCreator;
    public EndOfPathInstruction endOfPathInstruction;


    VertexPath path;
    FormationResult fr;
    float distanceTravelled;

    // Start is called before the first frame update
    void Start()
    {
        distanceTravelled = 0;
    }

    private void OnDrawGizmos()
    {

        if (!Application.isPlaying)
        {
            transform.GetChild(0).transform.position = unit.position;
            if (transform.childCount > 0)
            {
                // Create a new bezier path from the waypoints.
                List<Vector3> waypoints = new List<Vector3>(transform.childCount);
                foreach (Transform t in transform)
                    waypoints.Add(t.position);

                pathCreator.bezierPath = new BezierPath(waypoints, false, PathSpace.xyz); ;
            }
        }


        path = pathCreator.path;

        distanceTravelled += speed * Time.deltaTime;
        Vector3 center = path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        Vector3 direction = path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);

        fr = GetFormAtPos(center, direction, unit.numOfSoldiers, unit.cols, unit.stats.soldierDistLateral, unit.stats.soldierDistVertical);
        foreach (var v in fr.allPositions)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(v + Vector3.up * 3, 0.3f);
        }
        

        for (int i = 0; i < path.NumPoints; i++)
        {
            int nextI = i + 1;
            if (nextI >= path.NumPoints)
                if (isClosedLoop)
                    nextI %= path.NumPoints;
                else
                    break;
            Gizmos.DrawLine(path.GetPoint(i), path.GetPoint(nextI));
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var soldiers = unit.soldiers;
        Vector3[] targets = fr.allPositions;
        Vector3[] currents = new Vector3[soldiers.Length];
        for (int i = 0; i < soldiers.Length; i++)
            currents[i] = soldiers.ElementAt(i).go.transform.position;

        //double[,] distances = new double[soldiers.Length, soldiers.Length];
        //        Solver.Solve(distances);
        

        int[] assignment = LSCAssignment(currents, targets);

        for (int i = 0; i < soldiers.Length; i++)
        {
            soldiers.ElementAt(i).targetPos = targets[assignment[i]];
            soldiers.ElementAt(i).Move();
        }
    }
}
