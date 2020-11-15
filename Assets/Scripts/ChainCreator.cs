using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ChainCreator : MonoBehaviour
{

    public GameObject soldierBase, connector;
    public int cols;
    public int numOfSoldiers;
    public float soldierDistLateral;
    public float soldierDistVertical;
    //public Vector3 starPos;
    // public Vector3 direction;


    public GameObject[][] formation;

    public List<GameObject> chain, connectors;




    private void CreateChain()
    {
        foreach (var g in chain)
            DestroyImmediate(g);
        foreach (var g in connectors)
            DestroyImmediate(g);


        chain = new List<GameObject>();
        connectors = new List<GameObject>();



        int remainingPositions = numOfSoldiers;
        int numOfRows = (int)Mathf.Ceil(numOfSoldiers / (float)cols);
        float halfRowLenght = (cols - 1) * soldierDistVertical / 2;
        float halfColLenght = (numOfRows - 1) * soldierDistLateral / 2;


        formation = new GameObject[numOfRows][];
        for (int i = 0; i < numOfRows; i++)
        {
            int curRowNum = remainingPositions > cols ? cols : remainingPositions;
            GameObject[] curRow = new GameObject[curRowNum];


            var cur = Instantiate(soldierBase, transform.position - transform.right * ((curRowNum - 1) * soldierDistLateral / 2) + transform.forward * (halfColLenght - i * soldierDistVertical), Quaternion.identity, transform);
            curRow[0] = cur;

            if (i>0)
            {
                cur.AddComponent<SpringJoint>();
                var spring = cur.GetComponent<SpringJoint>();
                spring.connectedBody = formation[i - 1][0].GetComponent<Rigidbody>();
                spring.anchor = (spring.connectedBody.transform.localPosition - cur.transform.localPosition) * 0.5f;
                spring.spring = 5000;
                spring.damper = 100;
            }




            for (int j = 1; j < curRowNum; j++)
            {
                var next = Instantiate(soldierBase,
                    transform.position - transform.right * ((curRowNum - 1) * soldierDistLateral / 2 - j * soldierDistLateral) + transform.forward * (halfColLenght - i * soldierDistVertical),
                    Quaternion.Euler(0, 0, 0), transform);
                var curConnector = Instantiate(connector, cur.transform.position * 0.5f + next.transform.position * 0.5f, Quaternion.identity, transform);
                
                var hindges = curConnector.GetComponents<HingeJoint>();
                hindges[0].connectedBody = cur.GetComponent<Rigidbody>();
                hindges[1].connectedBody = next.GetComponent<Rigidbody>();


                chain.Add(cur);
                chain.Add(next);
                connectors.Add(curConnector);
                curRow[j] = next;
                cur = next;


                if (i > 0)
                {
                    cur.AddComponent<SpringJoint>();
                    var spring = cur.GetComponent<SpringJoint>();
                    spring.connectedBody = formation[i - 1][j].GetComponent<Rigidbody>();
                    spring.anchor = (spring.connectedBody.transform.localPosition - cur.transform.localPosition) * 0.5f;
                    spring.spring = 5000;
                    spring.damper = 100;
                }



            }

            remainingPositions -= curRowNum;
            formation[i] = curRow;
        }



        

        //for (int i=0; i< columns-1; i++)
        //{
        //    var next = Instantiate(soldierBase, starPos + (1+i)* direction , Quaternion.identity, transform);
        //    var curConnector = Instantiate(connector, cur.transform.position * 0.5f + next.transform.position*0.5f, Quaternion.identity, transform);

        //    var hindges = curConnector.GetComponents<HingeJoint>();

        //    hindges[0].connectedBody = cur.GetComponent<Rigidbody>();
        //    hindges[1].connectedBody = next.GetComponent<Rigidbody>();


        //    chain.Add(cur);
        //    chain.Add(next);
        //    connectors.Add(curConnector);

        //    cur = next;
        //}


    }



    // Update is called once per frame
    void Update()
    {
        if (!Application.isPlaying)
        {
            CreateChain();
        }
    }
}
