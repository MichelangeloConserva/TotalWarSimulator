using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Utils;

public class HumanNew : MonoBehaviour
{

    [Header("Settings")]
    public float timeForNewFormation = 1;
    public float rangedShaderSkiptime;
    public ArmyRole team;


    [Header("Linking")]
    public ArmyNew[] armies;
    public GameObject ghostPrefab;
    public RectTransform selectionBox;
    public LineRenderer mylr;


    [Header("UnitControl")]
    public List<Vector3> mouseTraj;
    public CUnitNew selectedUnit;
    public ArcherNew selectedArcher;


    public ArmyNew army { get { return armies[(int)team]; } }


    // Cache variables
    private LineRenderer lr;
    private List<GameObject> ghosts;
    private Vector3 mouseClick, startMouseClick, lastDir, finalDir, curPos, startMousePos, endMousePos, diff;
    private float maxWidth, curWidth;

    private int allUnits;

    private void Start()
    {
        allUnits = LayerMask.GetMask(armies[0].allyUnitLayer, armies[1].allyUnitLayer);
        StartCoroutine(DrawRangedUnitsShaderCO());
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
                RenderPath(selectedUnit);
            }
        }


        if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftShift))
            StartCoroutine(RMBUpCO());


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

    private IEnumerator DrawRangedUnitsShaderCO()
    {
        var wfs = new WaitForSeconds(rangedShaderSkiptime);
        yield return wfs;

        while (true)
        {
            ArcherNew c;
            foreach (var a in armies[0].archerUnits)
                if ((c = a.GetComponent<ArcherNew>()) != selectedArcher)
                    c.rangeShader.enabled = false;
            foreach (var a in armies[1].archerUnits)
                if ((c = a.GetComponent<ArcherNew>()) != selectedArcher)
                    c.rangeShader.enabled = false;


            ArcherNew aa;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits;
            if ((hits = Physics.RaycastAll(ray, 10000, allUnits)).Length != 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject.CompareTag("Melee"))
                        if (aa = hit.collider.gameObject.GetComponentInParent<ArcherNew>())
                            aa.rangeShader.enabled = true;
                }
            }
            yield return wfs;
        }
        
        
    }



    private IEnumerator RMBUpCO()
    {
        mouseClick = GetMousePosInWorld();
        var start = Time.time;

        yield return new WaitUntil(() => Input.GetMouseButtonUp(1) || Time.time - start > timeForNewFormation);

        if (Time.time - start < timeForNewFormation)
        {
            selectedUnit.MoveAt(mouseClick);
            RenderPath(selectedUnit);
        }
        else
            StartCoroutine(DrawGhostFormationCO(mouseClick));
    }

    private IEnumerator DrawGhostFormationCO(Vector3 mouseClick)
    {

        int numOfCols = selectedUnit.unit.numCols;
        float latDist = selectedUnit.unit.soldierDistLateral;
        finalDir = mouseClick - selectedUnit.transform.position;
        ghosts = Enumerable.Range(0, selectedUnit.unit.numOfSoldiers).Select(i => Instantiate(ghostPrefab, transform)).ToList();

        yield return new WaitUntil(() =>
        {
            if (!selectedUnit) return true;

            while (ghosts.Count > selectedUnit.unit.numOfSoldiers)
                ghosts.RemoveAt(0);
            maxWidth = 2*GetHalfLenght(selectedUnit.unit.soldierDistLateral, selectedUnit.unit.numOfSoldiers);

            curPos = GetMousePosInWorld();
            curWidth = (curPos - mouseClick).magnitude;
            if(curWidth > 0)
                lastDir = Vector3.ClampMagnitude(curPos - mouseClick, maxWidth);


            numOfCols = (int)(lastDir.magnitude / latDist) + 1;
            Vector2 perp = Vector2.Perpendicular(new Vector2(lastDir.x, lastDir.z));
            finalDir = new Vector3(perp.x, 0, perp.y);
            var curLength = GetHalfLenght(selectedUnit.unit.soldierDistLateral, numOfCols);
            var res = GetFormationAtPos(mouseClick + lastDir.normalized * curLength, finalDir, selectedUnit.unit.numOfSoldiers, numOfCols, selectedUnit.unit.soldierDistLateral, selectedUnit.unit.soldierDistVertical);

            for(int i=0; i<selectedUnit.unit.numOfSoldiers; i++)
            {
                ghosts[i].transform.position = res[i];
                ghosts[i].transform.rotation = Quaternion.LookRotation(finalDir);
            }

            return Input.GetMouseButtonUp(1);
        });

        foreach (var g in ghosts)
            Destroy(g);

        if (selectedUnit)
        {
            selectedUnit.unit.numCols = numOfCols;
            selectedUnit.MoveAt(mouseClick, finalDir);
        }
    }





    private void RenderPath(params CUnitNew[] cunits)
    {
        foreach(var cu in cunits)
        {
            var path = cu.pathCreator.path;
            lr = cu.unit.lr;
            lr.enabled = true;

            Debug.Log(path.NumPoints);

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
                if (selectedUnit.unit.GetType() == typeof(ArcherNew))
                    ((ArcherNew)selectedUnit.unit).rangeShader.enabled = false;
            }
            selectedUnit = null;

            selectedUnit = hits[0].collider.GetComponent<SoldierNew>().unit.cunit;

            if (selectedUnit.unit.GetType() == typeof(ArcherNew))
            {
                selectedArcher = (ArcherNew)selectedUnit.unit;
                selectedArcher.rangeShader.enabled = true;
            }


            selectedUnit.unit.isSelected = true;
        }
        else
        {
            if (selectedUnit)
            {
                selectedUnit.unit.isSelected = false;
                selectedUnit.unit.lr.enabled = false;
                if (selectedUnit.unit.GetType() == typeof(ArcherNew))
                    ((ArcherNew)selectedUnit.unit).rangeShader.enabled = false;
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
