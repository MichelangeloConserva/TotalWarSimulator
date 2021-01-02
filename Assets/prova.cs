using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static Utils;
using Debug = UnityEngine.Debug;

[ExecuteInEditMode]
public class prova : MonoBehaviour
{

    public float range, rowLength;


    public float width, height;

    private void Update()
    {

        //var left = transform.position - transform.right * rowLength;
        //var right = transform.position + transform.right * rowLength;
        //var leftFront = transform.position + transform.forward * range - transform.right * rowLength;// * 3f;
        //var rightFront = transform.position + transform.forward * range + transform.right * rowLength;// * 3f;


        //Mesh mesh = GetComponent<MeshFilter>().mesh;

        //Vector3[] vertices = new Vector3[]
        //{
        //    left, right, rightFront, leftFront
        //};

        //mesh.vertices = vertices;

        //mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        //GetComponent<MeshFilter>().mesh = mesh;


        var left = transform.position - transform.right * rowLength;
        Debug.Log(left);


        Mesh mesh = GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = new Vector3[4]
        {
            left + new Vector3(0, 1, 0),
            left + new Vector3(width, 1, 0),
            left + new Vector3(0, 1, height),
            left + new Vector3(width, 1, height + 5)
        };
        mesh.vertices = vertices;

        int[] tris = new int[6]
        {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
        };
        mesh.triangles = tris;

        Vector3[] normals = new Vector3[4]
        {
            -Vector3.up,
            -Vector3.up,
            -Vector3.up,
            -Vector3.up
        };
        mesh.normals = normals;



        GetComponent<MeshFilter>().mesh = mesh;




    }




}
