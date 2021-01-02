using NetTopologySuite.Geometries;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static Utils;

public class Archer : Unit
{

    public RangedStats rangedStatReference;

    [HideInInspector()]
    public float arrowDamage;
    [HideInInspector()]
    public bool freeFire;
    [HideInInspector()]
    public float fireInterval;
    [HideInInspector()]
    public float range;


    private Unit rangedTarget;
    private Polygon rangedGeom = new Polygon(new LinearRing(new Coordinate[] { }));
    private GameObject arrow;



    private new void Start()
    {
        base.Start();

        arrow = rangedStatReference.arrow;
        arrowDamage = rangedStatReference.rangedHolder.arrowDamage;
        freeFire = rangedStatReference.rangedHolder.freeFire;
        fireInterval = rangedStatReference.rangedHolder.fireInterval;
        range = rangedStatReference.rangedHolder.range;


        float rowLength = GetHalfLenght(meleeStats.soldierDistLateral, cols);

        var left = transform.position - transform.right * rowLength;
        Coordinate leftPoint = new Coordinate(left.x, left.z);
        var right = transform.position + transform.right * rowLength;
        Coordinate rightPoint = new Coordinate(right.x, right.z);

        var leftFront = transform.position + transform.forward * range - transform.right * rowLength * 3f;
        Coordinate leftPointFront = new Coordinate(leftFront.x, leftFront.z);
        var rightFront = transform.position + transform.forward * range + transform.right * rowLength * 3f;
        Coordinate rightPointFront = new Coordinate(rightFront.x, rightFront.z);

        //var left = - transform.right * rowLength;
        //Coordinate leftPoint = new Coordinate(left.x, left.z);
        //var right =   transform.right * rowLength;
        //Coordinate rightPoint = new Coordinate(right.x, right.z);

        //var leftFront = transform.forward * range - transform.right * rowLength * 3f;
        //Coordinate leftPointFront = new Coordinate(leftFront.x, leftFront.z);
        //var rightFront =  transform.forward * range + transform.right * rowLength * 3f;
        //Coordinate rightPointFront = new Coordinate(rightFront.x, rightFront.z);

        //MultiPoint prova = new MultiPoint(new Coordinate[] { leftPoint, rightPoint, rightPointFront, leftPointFront });
        rangedGeom = new Polygon(new LinearRing(new Coordinate[] { leftPoint, rightPoint, rightPointFront, leftPointFront, leftPoint }));


        if (Application.isPlaying)
            StartCoroutine(FireArrowCoroutine());

    }


    private new void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (DEBUG_MODE)
        {
            if (!Application.isPlaying)
            {
                Start();
            }

            var c = new Vector3((float)coords[0].X * 0.5f + (float)coords[1].X * 0.5f, 0, (float)coords[0].Y * 0.5f + (float)coords[1].Y * 0.5f);
            var cFront = new Vector3((float)coords[2].X * 0.5f + (float)coords[3].X * 0.5f, 0, (float)coords[2].Y * 0.5f + (float)coords[3].Y * 0.5f);
            Gizmos.DrawSphere(position + Vector3.up * 3, 0.5f);
            Gizmos.DrawRay(position + Vector3.up * 3, targetDirection * 5);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(position + Vector3.up * 3, cFront - c + Vector3.up * 0.5f);

            if(Application.isPlaying)
            {
                enemyUnitsInRange.Clear();
                foreach (var e in army.enemy.units)
                {
                    if (e.numOfSoldiers > 0 && !e.meleeGeom.Disjoint(rangedGeom))
                    {
                        Gizmos.color = Color.red;
                        if (enemyUnitsInRange.Count > 0 && Vector3.Distance(position, e.position) > Vector3.Distance(position, enemyUnitsInRange.First().position))
                            enemyUnitsInRange.Insert(0, e);
                        else
                            enemyUnitsInRange.Add(e);
                    }
                }

                if (enemyUnitsInRange.Count > 0)
                {
                    var enemyVelocity = enemyUnitsInRange.First().GetComponentInChildren<Rigidbody>().velocity;
                    Vector3 target = enemyUnitsInRange.First().position + 2 * enemyVelocity;
                    Gizmos.DrawLine(position, target + Vector3.up * 5);
                }
            }

            for (int i = 0; i < coords.Length - 1; i++)
            {
                Gizmos.DrawLine(new Vector3((float)coords[i].X, 0, (float)coords[i].Y),
                                new Vector3((float)coords[i + 1].X, 0, (float)coords[i + 1].Y));
            }
        }
    }



    private List<Unit> enemyUnitsInRange = new List<Unit>();
    private void UpdateRangedGeom()
    {
        if (rangedGeom.IsEmpty) return;

        coords = rangedGeom.Coordinates;
        c = new Vector3((float)coords[0].X * 0.5f + (float)coords[1].X * 0.5f, 0, (float)coords[0].Y * 0.5f + (float)coords[1].Y * 0.5f);
        cFront = new Vector3((float)coords[2].X * 0.5f + (float)coords[3].X * 0.5f, 0, (float)coords[2].Y * 0.5f + (float)coords[3].Y * 0.5f);
        var deg = Vector3.SignedAngle(cFront - c, GetVector3Down(targetDirection), Vector3.up);

        rangedGeom = UpdateGeometry(rangedGeom, position.x - c.x, position.z - c.z, deg);
    }

    public float noise = 0.1f, distance = 5;
    public float h = 25;

    protected new void Update()
    {
        base.Update();
        UpdateRangedGeom();
    }

    private Vector3 CalculateTarget(Unit targetUnit)
    {
        var enemyVelocity = targetUnit.GetComponentInChildren<Rigidbody>().velocity;
        return targetUnit.position + 2 * enemyVelocity;
    }

    private void ShootAt(Unit targetUnit)
    {
        Vector3 target = CalculateTarget(targetUnit);//enemyUnitsInRange.First());

        foreach (var s in soldiers)
        {
            if (s.rb.velocity.magnitude > 1) continue;

            var g = Instantiate(arrow, s.position + Vector3.up * 2, Quaternion.Euler(90, 0, 0));
            var arr = g.GetComponent<Arrow>();
            arr.damage = arrowDamage;
            arr.Launch(target + (s.position - position) * 0.5f + new Vector3(Random.Range(-noise, noise), Random.Range(-noise, noise), Random.Range(-noise, noise)));
        }
    }



    private void FindCloserEnemyInRange()
    {
        enemyUnitsInRange.Clear();
        foreach (var e in army.enemy.units)
        {
            if (e.meleeGeom != null && rangedGeom != null)
            {

                if (e.numOfSoldiers > 0 && !e.meleeGeom.Disjoint(rangedGeom))
                {
                    Debug.LogFormat("{0} {1} {2}", Vector3.Distance(e.position,position), gameObject.name, e.gameObject.name);


                    if (enemyUnitsInRange.Count > 0 && Vector3.Distance(position, e.position) > Vector3.Distance(position, enemyUnitsInRange.First().position))
                        enemyUnitsInRange.Insert(0, e);
                    else
                        enemyUnitsInRange.Add(e);
                }
            }
        }
    }

    
    private WaitForEndOfFrame wfeof = new WaitForEndOfFrame();
    private IEnumerator FireArrowCoroutine()
    {
        WaitForSeconds wfs = new WaitForSeconds(fireInterval);
        yield return wfs;

        while (true)
        {
            if (isInFight) yield return wfeof;

            if (commandTarget != null)
            {
                if (commandTarget.numOfSoldiers > 0 && !commandTarget.meleeGeom.Disjoint(rangedGeom))
                {
                    ShootAt(commandTarget);
                    yield return wfs;
                }
            }


            if (freeFire)
            {
                FindCloserEnemyInRange();
                if (enemyUnitsInRange.Count > 0)
                {
                    ShootAt(enemyUnitsInRange.First());
                    yield return wfs;
                }
            }


            yield return wfeof;
        }
        
    }






}
