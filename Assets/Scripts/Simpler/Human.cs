using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class Human : MonoBehaviour
{

    public CUnit selectedUnit;

    public List<Vector3> mouseTraj;

    private bool mouseGather;
    private Vector3 mouseClick;


    void Start()
    {
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            mouseTraj = new List<Vector3>();
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0))
        {
            mouseClick = GetMousePosInWorld();

            if (mouseTraj.Count == 0 || Vector3.Distance(mouseClick, mouseTraj.Last()) > 1)
                mouseTraj.Add(mouseClick);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (mouseTraj.Count > 4)
                selectedUnit.MoveAt(mouseTraj);
        }


        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
        {
            Vector3 mouseClick = GetMousePosInWorld();
            selectedUnit.MoveAt(mouseClick, true);
        }


    }




}
