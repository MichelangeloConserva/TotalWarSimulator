using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static Utils;
using Debug = UnityEngine.Debug;

public class prova : MonoBehaviour
{
    [Range(5,100)]
    public int num;
    [Range(5, 50)]
    public int cols;
    public float soldierDistLateral, soldierDistVertical;



    private Vector3 formationPos, unitDir;
    private Vector3[] targets;
    private int[] assignment;
    private void CalculateAssignments(Vector3 center, Vector3 direction)
    {
        


        //targets = fr.allPositions;
        //Vector3[] currents = new Vector3[num];
        //for (int i = 0; i < soldiers.Length; i++)
        //    currents[i] = soldiers.ElementAt(i).go.transform.position;
        //assignment = LSCAssignment(currents, targets, unit.soldierLocalScale);
    }





    private void OnDrawGizmos()
    {

        var fr = GetFormAtPos(transform.GetChild(0).position, transform.GetChild(0).forward, num, cols, soldierDistLateral, soldierDistVertical);
        var fr1 = GetFormAtPos(transform.GetChild(1).position, transform.GetChild(1).forward, num, cols, soldierDistLateral, soldierDistVertical);

        //GameObject[] gs = new GameObject[num];

        Gizmos.color = Color.yellow;
        foreach (var p in fr.allPositions.Concat(fr1.allPositions))
        {
            Gizmos.DrawSphere(p, 0.1f);
        }

        var assignment = LSCAssignment(fr.allPositions, fr1.allPositions);


        Gizmos.color = Color.cyan;
        for (int i=0; i<assignment.Length; i++)
        {

            Gizmos.DrawLine(fr.allPositions[i], fr1.allPositions[assignment[i]]);
        }



        //foreach (var g in gs)
        //    DestroyImmediate(g);




    }


}
