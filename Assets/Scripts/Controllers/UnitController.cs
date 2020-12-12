using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class UnitController : MonoBehaviour
{
    public static SoldierController GetController(GameObject s) { return s.GetComponent<SoldierController>(); }


    [System.Serializable]
    public class Stats
    {
        public int numOfSoldiers;
        public int columns;
        public float distanceOfSoldiers;
    }

    public int team;
    public GameObject soldierPrefab;
    public Stats stats;
    public UnitStatus unitStatus;
    public Transform formationT;
    public GameObject[][] soldiers;

    private SquareFormation squareFormation;

    

    void Start()
    {
        unitStatus = UnitStatus.CHARGING;

        squareFormation = GetComponentInChildren<SquareFormation>().InitializeFormation(transform.position, transform.rotation.eulerAngles.y, stats.columns, stats.numOfSoldiers, stats.distanceOfSoldiers);
        formationT = transform.GetChild(1);


        soldiers = new GameObject[squareFormation.formation.Length][];
        for (int i=0; i<squareFormation.formation.Length; i++)
        {
            var curFormationRow = squareFormation.formation[i];
            var curRowSoldiers = new GameObject[curFormationRow.Length];
            for (int j=0; j<curFormationRow.Length; j++)
            {
                var sold = Instantiate(soldierPrefab, curFormationRow[j].transform.position, curFormationRow[j].transform.rotation, transform.GetChild(0));
                sold.GetComponent<SoldierController>().team = team;


                curRowSoldiers[j] = sold;

                // trying adding springs
                if (j>0)
                {
                    var dist = Vector3.Distance(sold.transform.position, curRowSoldiers[j - 1].transform.position);
                    sold.AddComponent<SpringJoint>();
                    var spring = sold.GetComponent<SpringJoint>();
                    spring.connectedBody = curRowSoldiers[j - 1].GetComponent<Rigidbody>();
                    spring.minDistance = dist * 0.9f;
                    spring.damper = 350;
                    spring.spring = 500;
                    spring.maxDistance = dist * 1.1f;
                    spring.enableCollision = true;

                    //sold.AddComponent<HingeJoint>();
                    //var hinge = sold.GetComponent<HingeJoint>();
                    //hinge.autoConfigureConnectedAnchor = false;
                    //hinge.connectedBody = curRowSoldiers[j - 1].GetComponent<Rigidbody>();
                    //hinge.autoConfigureConnectedAnchor = false;
                    //hinge.anchor = GetVector3Down((-sold.transform.position + curRowSoldiers[j - 1].transform.position));
                    //hinge.connectedAnchor = Vector3.zero;
                    //hinge.enableCollision = true;
                    //hinge.axis = Vector3.up * 180;


                }

                if (i > 0)
                {
                    var dist = Vector3.Distance(sold.transform.position, soldiers[i - 1][j].transform.position);
                    sold.AddComponent<SpringJoint>();
                    var spring = sold.GetComponent<SpringJoint>();
                    spring.connectedBody = soldiers[i - 1][j].GetComponent<Rigidbody>();
                    spring.minDistance = dist * 0.9f;
                    spring.damper = 350;
                    spring.spring = 500;
                    spring.maxDistance = dist * 1.1f;
                    spring.enableCollision = true;


                    //sold.AddComponent<HingeJoint>();
                    //var hinge = sold.GetComponent<HingeJoint>();
                    //hinge.autoConfigureConnectedAnchor = false;
                    //hinge.connectedBody = soldiers[i - 1][j].GetComponent<Rigidbody>();
                    //hinge.anchor = GetVector3Down((-sold.transform.position + soldiers[i - 1][j].transform.position));
                    //hinge.connectedAnchor = Vector3.zero;
                    //hinge.enableCollision = true;
                    //hinge.axis = Vector3.up * 180;

                }


            }
                
            soldiers[i] = curRowSoldiers;
        }

    }

    public List<GameObject> GetSoldiers()
    {
        List<GameObject> allSoldiers = new List<GameObject>();
        for (int i = 0; i < soldiers.Length; i++)
            for (int j = 0; j < soldiers[i].Length; j++)
                allSoldiers.Add(soldiers[i][j]);
        return allSoldiers;
    }


    private Vector3 GetUnitPos()
    {
        Vector3 pos = Vector3.zero;
        var allSoldiers = GetSoldiers();
        foreach (GameObject s in allSoldiers)
            pos += s.transform.position / allSoldiers.Count;
        return pos;
    }
    private void SetSoldiersDest()
    {

    }


    public void MoveAtPos(Vector3 pos)
    {
        // Moving just one soldier now
        //var soldier = soldiers.First();
        //GetController(soldier).targetPos = pos;

        formationT.transform.position = GetUnitPos();
        formationT.LookAt(pos);
        formationT.transform.position = pos;


        for (int i = 0; i < soldiers.Length; i++)
            for (int j = 0; j < soldiers[i].Length; j++)
                soldiers[i][j].GetComponent<SoldierController>().targetPos = squareFormation.formation[i][j].transform.position;


    }

    // Update is called once per frame
    void Update()
    {




    }
}
