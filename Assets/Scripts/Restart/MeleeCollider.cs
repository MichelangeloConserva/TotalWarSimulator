using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            unit.isInFight = true;
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
