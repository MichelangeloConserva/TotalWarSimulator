using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Utils;

public class HumanNew : MonoBehaviour
{
    public RectTransform selectionBox;
    public ArmyRole team;
    public ArmyNew[] armies;


    public ArmyNew army { get { return armies[(int)team]; } }

    public List<Vector3> mouseTraj;

    public CUnitNew selectedUnit;

    private Vector3 mouseClick, startMouseClick;
    private Vector3 startMousePos, endMousePos;
    private Vector3 diff;

    public LineRenderer mylr;
    private LineRenderer lr;

    void Start()
    {
    }

    void Update()
    {
        if (selectedUnit && selectedUnit.unit.numOfSoldiers == 0)
            selectedUnit = null;


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







        if (Input.GetKey(KeyCode.Space))
            RenderPath(army.units.Select(u => u.cunit).ToArray());
        
        if(Input.GetKeyUp(KeyCode.Space))
            foreach (var u in army.units)
                u.lr.enabled = false;



        if (!selectedUnit) return;


        if (!Input.GetKey(KeyCode.Space))
        {
            RenderPath(selectedUnit);
        }


        // FORMATION 
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(1))
        {
            mylr.enabled = true;
            mouseClick = GetMousePosInWorld();
            if (mouseTraj.Count == 0 || Vector3.Distance(mouseClick, mouseTraj.Last()) > 3)
            {
                mouseTraj.Add(mouseClick);
                mylr.positionCount = mouseTraj.Count;
                mylr.SetPositions(mouseTraj.ToArray());
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            mylr.enabled = false;
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


        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("reload");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("quit");
            Application.Quit();
        }

    }


    private void RenderPath(params CUnitNew[] cunits)
    {
        foreach(var cu in cunits)
        {
            var path = cu.pathCreator.path;
            lr = cu.unit.lr;
            lr.enabled = true;


            //if (lr.material == null)
            //{
            //    lr.material = new Material(mylr.material);
            //    lr.material.SetColor("_Color", Color.yellow);
            //}


            if (path.NumPoints != 30)
            {
                var points = Enumerable.Range(0, 10).Select(i =>
                    path.GetPointAtDistance((1 - i / 9f) * cu.distanceTravelled +
                                        (i / 9f) * path.length,
                                        PathCreation.EndOfPathInstruction.Stop)
                ).ToArray();
                lr.positionCount = points.Length;
                lr.SetPositions(points);
            }
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
            {
                selectedUnit.unit.isSelected = false;
                selectedUnit.unit.lr.enabled = false;
            }
            selectedUnit = null;

            selectedUnit = hits[0].collider.GetComponent<SoldierNew>().unit.cunit;
            selectedUnit.unit.isSelected = true;
        }
        else
        {
            if (selectedUnit)
            {
                selectedUnit.unit.isSelected = false;
                selectedUnit.unit.lr.enabled = false;
            }
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
