using NetTopologySuite.Geometries;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static Utils;

public class Archer : Unit
{

    public bool freeFire;
    public float range;


    Point[] archerRangePoints;


    private Polygon prova;

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
        prova = new Polygon(new LinearRing(new Coordinate[] { leftPoint, rightPoint, rightPointFront, leftPointFront, leftPoint }));
    }


    private void OnDrawGizmos()
    {

        if (DEBUG_MODE)
        {
            if (!Application.isPlaying)
            {
                Start();
            }





            var c = new Vector3((float)prova.Coordinates[0].X * 0.5f + (float)prova.Coordinates[1].X * 0.5f, 0, (float)prova.Coordinates[0].Y * 0.5f + (float)prova.Coordinates[1].Y * 0.5f);
            var cFront = new Vector3((float)prova.Coordinates[2].X * 0.5f + (float)prova.Coordinates[3].X * 0.5f, 0, (float)prova.Coordinates[2].Y * 0.5f + (float)prova.Coordinates[3].Y * 0.5f);

            var t = new NetTopologySuite.Geometries.Utilities.AffineTransformation().SetToTranslation(position.x - c.x, position.z - c.z);
            prova = (Polygon)t.Transform(prova);



            var deg = Vector3.SignedAngle(cFront - c, GetVector3Down(targetDirection), Vector3.up);

            if (deg > 0)
                deg = 360 - deg;
            if (deg < 0)
                deg *= -1;



            Gizmos.DrawSphere(position + Vector3.up * 3, 0.5f);
            Gizmos.DrawRay(position + Vector3.up * 3, targetDirection * 5);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(position + Vector3.up * 3, cFront - c + Vector3.up * 0.5f);

            t = new NetTopologySuite.Geometries.Utilities.AffineTransformation().SetToRotation(Mathf.Deg2Rad * deg);
            prova = (Polygon)t.Transform(prova);


            Gizmos.color = Color.yellow;

            for (int i=0; i<prova.Coordinates.Length-1; i++)
            {
                Gizmos.DrawLine(new Vector3((float)prova.Coordinates[i].X, 0, (float)prova.Coordinates[i].Y),
                                new Vector3((float)prova.Coordinates[i+1].X, 0, (float)prova.Coordinates[i+1].Y));
            }


            //Gizmos.DrawLine(leftFront, rightFront);
            //Gizmos.DrawLine(rightFront, right);
            //Gizmos.DrawLine(right, left);
            //Gizmos.DrawLine(left, leftFront);
        }
        



    }









}
