using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class MeleeCollider : MonoBehaviour
{
    public UnitNew unit;


    private void OnTriggerEnter(Collider other)
    {
        if (other.GetType() != typeof(BoxCollider)) return;


        if (unit.fightingAgainst.Count == 0)
            unit.fightingTarget = other.GetComponentInParent<UnitNew>();

        unit.fightingAgainst.Add(other.GetComponentInParent<UnitNew>());

        if (!unit.isInFight)
        {
            if (!other.gameObject) return;

            unit.isInFight = true;
            var dir = GetVector3Down(other.transform.position - transform.position);
            float dist = dir.magnitude;
            if(dist>0)
            {
                unit.transform.rotation = Quaternion.LookRotation(dir);
                unit.transform.position = transform.position + dir * (dist - unit.meleeCollider.size.z + 2) / dist;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetType() != typeof(BoxCollider)) return;

        Debug.DrawLine(unit.position + Vector3.up * 5, other.gameObject.transform.position + Vector3.up, Color.magenta);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetType() != typeof(BoxCollider)) return;


        unit.fightingAgainst.Remove(other.GetComponentInParent<UnitNew>());

        if (unit.fightingAgainst.Count == 0)
        {
            unit.fightingTarget = null;
            unit.isInFight = false;
            return;
        }

        if (unit.fightingTarget == other.GetComponentInParent<UnitNew>())
            unit.fightingTarget = unit.fightingAgainst.OrderBy(en => Vector3.SqrMagnitude(en.position - unit.position)).First();
    }


}
