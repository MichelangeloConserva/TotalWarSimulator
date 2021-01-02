using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AI : MonoBehaviour
{
    public Army enemy;
    public List<Unit> archers;
    public CombactManager mng;

    public float fallBackDistance = 1, distanceToRetreat = 15;

    private void OnDrawGizmos()
    {
        //foreach (var a in archers)
        //    Gizmos.DrawLine(a.position, a.position - a.transform.forward * fallBackDistance + Vector3.up);
    }


    // Update is called once per frame
    void Update()
    {
        if (Time.time > 2)
        {
            foreach (var a in archers.ToArray())
                foreach ( var e in enemy.units)
                    if (Vector3.Distance(a.position, e.position) < fallBackDistance)
                    {
                        a.cunit.MoveAt(a.position - a.transform.forward * fallBackDistance, a.transform.forward); // TODO : this forward has no sense in general
                        archers.Remove(a);
                    }
        }
        

    }
}
