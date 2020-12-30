using NetTopologySuite.Geometries;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static Utils;

public class Archer : Unit
{

    public GameObject arrow;
    public float arrowDamage;

    public bool freeFire;
    public float range, fireInterval;


    Point[] archerRangePoints;

    private Polygon rangedGeom;
    private Unit rangedTarget;
    


    private void Start()
    {
        base.Start();

        float rowLength = GetHalfLenght(stats.soldierDistLateral, cols);

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

            var c = new Vector3((float)rangedGeom.Coordinates[0].X * 0.5f + (float)rangedGeom.Coordinates[1].X * 0.5f, 0, (float)rangedGeom.Coordinates[0].Y * 0.5f + (float)rangedGeom.Coordinates[1].Y * 0.5f);
            var cFront = new Vector3((float)rangedGeom.Coordinates[2].X * 0.5f + (float)rangedGeom.Coordinates[3].X * 0.5f, 0, (float)rangedGeom.Coordinates[2].Y * 0.5f + (float)rangedGeom.Coordinates[3].Y * 0.5f);
            Gizmos.DrawSphere(position + Vector3.up * 3, 0.5f);
            Gizmos.DrawRay(position + Vector3.up * 3, targetDirection * 5);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(position + Vector3.up * 3, cFront - c + Vector3.up * 0.5f);

            if(Application.isPlaying)
            {
                enemyUnitsInRange.Clear();
                foreach (var e in army.enemy.units)
                {
                    e.CreateMeleeGeometry();
                    if (!e.meleeGeom.Disjoint(rangedGeom))
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


                for (int i = 0; i < rangedGeom.Coordinates.Length - 1; i++)
                {
                    Gizmos.DrawLine(new Vector3((float)rangedGeom.Coordinates[i].X, 0, (float)rangedGeom.Coordinates[i].Y),
                                    new Vector3((float)rangedGeom.Coordinates[i + 1].X, 0, (float)rangedGeom.Coordinates[i + 1].Y));
                }
            }
            



            







        }
        
    }



    private List<Unit> enemyUnitsInRange = new List<Unit>();


    private new void FixedUpdate()
    {
        base.FixedUpdate();
        UpdateRangedGeom();
    }

    private void UpdateRangedGeom()
    {
        var c = new Vector3((float)rangedGeom.Coordinates[0].X * 0.5f + (float)rangedGeom.Coordinates[1].X * 0.5f, 0, (float)rangedGeom.Coordinates[0].Y * 0.5f + (float)rangedGeom.Coordinates[1].Y * 0.5f);
        var cFront = new Vector3((float)rangedGeom.Coordinates[2].X * 0.5f + (float)rangedGeom.Coordinates[3].X * 0.5f, 0, (float)rangedGeom.Coordinates[2].Y * 0.5f + (float)rangedGeom.Coordinates[3].Y * 0.5f);
        var deg = Vector3.SignedAngle(cFront - c, GetVector3Down(targetDirection), Vector3.up);

        rangedGeom = UpdateGeometry(rangedGeom, position.x - c.x, position.z - c.z, deg);
    }



    public float noise = 0.1f, distance = 5;
    public float h = 25;

    protected new void Update()
    {
        base.Update();
    }

    private void Shoot()
    {
        var enemyVelocity = enemyUnitsInRange.First().GetComponentInChildren<Rigidbody>().velocity;
        Vector3 target = enemyUnitsInRange.First().position + 2 * enemyVelocity;

        foreach (var s in soldiers)
        {
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
                if (!e.meleeGeom.Disjoint(rangedGeom))
                {
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
        yield return wfeof;

        while (true)
        {
            if (isInFight) yield return wfeof;

            if (freeFire)
            {
                FindCloserEnemyInRange();
                if (enemyUnitsInRange.Count > 0)
                {
                    Shoot();
                    yield return wfs;
                }
            }
            yield return wfeof;
        }
        
    }






}
