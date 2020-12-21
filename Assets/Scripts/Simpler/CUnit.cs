using Interpolation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;



public class CUnit : MonoBehaviour
{

    Unit unit;
    UnitTarget unitTarget;

    private void Start()
    {
        unit = GetComponent<Unit>();

        StartCoroutine(UpdateAssignmentCO());
    }

    public void MoveAt(Vector3 targetPos, Vector3 dir, bool reset)
    {
        Vector3 start = unit.GetPosition();
        if (reset)
        {
            double[] ctrlp = new double[10];
            getInterPoints(start, targetPos).CopyTo(ctrlp, 0);
            MoveAt(ctrlp, true);
        }
        else
        {
            var ctrlp = getInterPoints(start, unit.targetPos).Concat(getInterPoints(unit.targetPos, targetPos)).ToList();
            MoveAt(ctrlp, true);
        }
        
        unit.targetPos = targetPos;
    }
    private void MoveAt(IList<double> ctrlp, bool reset)
    {
        if (reset)
            foreach (var s in unit.GetSoldiers())
            {
                s.Reset(new List<Vector3>() { s.go.transform.position }, new List<FormationIndices>() { s.indices.First() });
            }

        if (Vector3.Distance(unit.GetPosition(), new Vector3((float)ctrlp[0], unit.GetPosition().y, (float)ctrlp[1])) > 10) // TODO : ENCODE
        {
            double[] start = new double[] { unit.GetPosition().x*0.7f + ctrlp[0]*0.3f, unit.GetPosition().z * 0.7f + ctrlp[1] * 0.3f };
            ctrlp = start.Concat(ctrlp).ToList();
        }

        FormationResult fr;
        var res = getSpline(ctrlp, unit.DEBUG_MODE);
        var trajectory = res[0];
        var directions = res[1];

        Vector3 lastDir = directions.ElementAt(0);
        for (int i=0; i<trajectory.Count; i++)
        {
            Vector3 curPos = trajectory.ElementAt(i);
            Vector3 curDir;
            if (i < directions.Count)  curDir = directions.ElementAt(i);
            else                       curDir = curPos - trajectory.ElementAt(i-1);

            // bool applyLSA = i==0 || Vector3.Angle(lastDir, curDir) > 35;  // TODO : ENCODE
            bool applyLSA = true;  // TODO : ENCODE
            fr = GetFormAtPos(curPos, curDir, unit.numOfSoldiers, unit.cols, unit.soldierDistLateral, unit.soldierDistVertical);
            UpdateTargetPositionOfSoldiers(fr, applyLSA);
        }
    }

    public void MoveAt(Vector3 targetPos, bool reset)
    {
        MoveAt(targetPos, (targetPos - unit.GetPosition()).normalized, reset);
    }

    public void MoveAt(List<Vector3> traj)
    {
        double[] ctrlp = new double[traj.Count * 2];

        int k = 0;
        foreach(var v in traj)
        {
            ctrlp[k++] = v.x;
            ctrlp[k++] = v.z;
        }

        MoveAt(ctrlp, true);
    }



    public void UpdateTargetPositionOfSoldiers(FormationResult to, bool applyLSA)
    {
        var soldiers = unit.GetSoldiers();

        Vector3[] finalPositions = to.allPositions;

        int[] assignment;
        if (applyLSA)
        {
            Vector3[] fromPositions = soldiers.Select(s => s.trajectory.Last()).ToArray(); // using the last position in the trajectory
            assignment = LSCAssignment(fromPositions, finalPositions, unit.soldierLocalScale);
            for (int j = 0; j < unit.numOfSoldiers; j++)
                soldiers.ElementAt(j).Add(finalPositions[assignment[j]], to.indices[assignment[j]]);
        }
        else
        {
            for (int j = 0; j < unit.numOfSoldiers; j++)
                soldiers.ElementAt(j).Add(finalPositions[j], to.indices[j]);
        }
    }



    public IEnumerator UpdateAssignmentCO()
    {
        while (true)
        {
            yield return new WaitForSeconds(3); // TODO : encode
            //UpdateAssignment();
        }
    }

    public void UpdateAssignment()
    {
        var soldiers = unit.GetSoldiers();
        Vector3[] targets = soldiers.Select(s => s.trajectory.First()).ToArray();
        Vector3[] currents = soldiers.Select(s => s.go.transform.position).ToArray();
        int[] assignment = LSCAssignment(currents, targets, unit.soldierLocalScale);

        for (int j = 0; j < unit.numOfSoldiers; j++)
            soldiers.ElementAt(j).SwapTrajectory(soldiers.ElementAt(assignment[j]));
    }





}
