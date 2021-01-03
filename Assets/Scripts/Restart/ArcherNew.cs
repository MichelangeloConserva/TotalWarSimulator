using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;


public class ArcherNew : UnitNew
{
    public GameObject arrow;

    public float arrowDamage;
    public bool freeFire;
    public float fireInterval;
    public float range;
    public float noise = 0.1f;
    public float maxVelocity = 2;

    public HashSet<UnitNew> unitsInRange = new HashSet<UnitNew>();

    private WaitForEndOfFrame wfeof = new WaitForEndOfFrame();

    private void Start()
    {
        bCol = GetComponentInChildren<BoxCollider>();
        StartCoroutine(FireArrowCoroutine());
    }



    private IEnumerator FireArrowCoroutine()
    {
        WaitForSeconds wfs = new WaitForSeconds(fireInterval);
        yield return wfs;

        while (true)
        {
            if (isInFight) yield return wfeof;

            if (commandTarget != null)
            {
                if (unitsInRange.Contains(commandTarget))
                {
                    if(ShootAt(commandTarget))
                        yield return wfs;
                }
            }
            yield return wfeof;
        }
    }

    private Vector3 target;
    private bool ShootAt(UnitNew targetUnit)
    {
        CalculateTarget(targetUnit);//enemyUnitsInRange.First());

        if (Vector3.SqrMagnitude(bCol.ClosestPoint(target) - position) < 5) return false;

        foreach (var s in soldiers)
        {
            if (s.rb.velocity.magnitude > 1) continue;

            var g = Instantiate(arrow, s.position + Vector3.up * 2, Quaternion.Euler(90, 0, 0));
            var arr = g.GetComponent<Arrow>();
            arr.damage = arrowDamage;
            arr.Launch(target + (s.position - position) * 0.8f + new Vector3(Random.Range(-noise, noise), Random.Range(-noise, noise), Random.Range(-noise, noise)));
        }
        return true;
    }

    private BoxCollider bCol;
    private void CalculateTarget(UnitNew targetUnit)
    {
        var enemyVelocity = Vector3.ClampMagnitude(targetUnit.soldiers.First().rb.velocity, maxVelocity);
        target = targetUnit.position + 2 * enemyVelocity;
    }

}
