using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class Human : MonoBehaviour
{
    public RectTransform selectionBox;
    public ArmyRole team;


    public List<Vector3> mouseTraj;

    public CUnit selectedUnit;

    private Vector3 mouseClick, startMouseClick;
    private Vector3 startMousePos, endMousePos;
    private Vector3 diff;



    void Start()
    {
    }

    void Update()
    {


        // SELECTION
        if (Input.GetMouseButtonDown(0))
        {
            startMousePos = Input.mousePosition;
            startMouseClick = GetMousePosInWorld();
        }

        if (Input.GetMouseButton(0))
            selectionClick(false);   // TODO : implement group selection

        if (Input.GetMouseButtonUp(0))
            selectionBox.gameObject.SetActive(false);



        if (!selectedUnit) return;

        // FORMATION 
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(1))
        {
            mouseClick = GetMousePosInWorld();
            if (mouseTraj.Count == 0 || Vector3.Distance(mouseClick, mouseTraj.Last()) > 3)
                mouseTraj.Add(mouseClick);
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (mouseTraj.Count > 4)
            {
                selectedUnit.MoveAt(mouseTraj);
                mouseTraj.Clear();
            }
        }

        if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftShift))
        {
            mouseClick = GetMousePosInWorld();
            selectedUnit.MoveAt(mouseClick);
        }

    }



    private void selectionClick(bool DEBUG_MODE = false)
    {
        endMousePos = Input.mousePosition;

        RaycastHit[] hits;
        if (Vector3.Distance(startMousePos, endMousePos) > 2)
        {    // Box selection
            selectionBox.gameObject.SetActive(true);

            diff = endMousePos - startMousePos;
            selectionBox.sizeDelta = new Vector2(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
            if (diff.x > 0)
                selectionBox.anchoredPosition = new Vector3(startMousePos.x + (diff.x / 2), startMousePos.y + (diff.y / 2), 0);
            else
                selectionBox.anchoredPosition = new Vector3(endMousePos.x - (diff.x / 2), endMousePos.y - (diff.y / 2), 0);

            diff = GetMousePosInWorld() - startMouseClick;
            hits = mouseHits(Mathf.Abs(diff.x / 2f), Mathf.Abs(diff.z / 2f), DEBUG_MODE);
        }
        else // Single click selection
            hits = mouseHits(1, 1, DEBUG_MODE);


        if (hits.Length > 0)
        {
            // TODO : implement group selection
            if (selectedUnit)
                selectedUnit.unit.isSelected = false;
            selectedUnit = null;

            selectedUnit = hits[0].collider.transform.parent.gameObject.GetComponent<CUnit>();
            selectedUnit.unit.isSelected = true;
        }
        else
        {
            if (selectedUnit)
                selectedUnit.unit.isSelected = false;
            selectedUnit = null;
        }

    }

    private RaycastHit[] mouseHits(float width, float height, bool DEBUG_MODE = false)
    {
        var click = startMouseClick * 0.5f + GetMousePosInWorld() * 0.5f;
        RaycastHit[] hits = Physics.BoxCastAll(
                                   click,
                                   new Vector3(width, height, 900),
                                   Vector3.up,
                                   Quaternion.LookRotation(Vector3.up),
                                   900,
                                   LayerMask.GetMask("unitSoldier" + ((int)team + 1))
                               );
        if (DEBUG_MODE)
        {
            Debug.Log(hits.Length);

            DrawBox(click,
                new Vector3(width, height, 900),
                Quaternion.LookRotation(Vector3.up),
                Color.yellow,
                0.1f);
        }

        return hits;
    }







}
