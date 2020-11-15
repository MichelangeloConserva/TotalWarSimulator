using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareFormation : Formation
{

    public GameObject soldierGhost;

    public GameObject[][] formation;
    private Vector3 position;
    private float rotation, soldierDist;
    private int cols, numOfSoldiers;

    public override Vector3 GetPosition() { return position; }
    public override float GetRotation() { return rotation; }
    public override float GetSoldierDist() { return rotation; }
    public override int GetCols() { return cols; }
    public override int GetNumSoldiers() { return numOfSoldiers; }


    public SquareFormation InitializeFormation(Vector3 position, float rotation, int cols, int numOfSoldiers, float soldierDist)
    {
        this.position = position;
        this.rotation = rotation;
        this.cols = cols;
        this.numOfSoldiers = numOfSoldiers;
        this.soldierDist = soldierDist;


        int remainingPositions = numOfSoldiers;
        int numOfRows = (int)Mathf.Ceil(numOfSoldiers / (float)cols);
        float halfRowLenght = (cols - 1) * soldierDist / 2;
        float halfColLenght = (numOfRows - 1) * soldierDist / 2;


        formation = new GameObject[numOfRows][];
        for (int i=0; i<numOfRows; i++)
        {
            int curRowNum = remainingPositions > cols ? cols : remainingPositions;
            GameObject[] curRow = new GameObject[curRowNum];
            for (int j = 0; j < curRowNum; j++)
                curRow[j] = Instantiate(soldierGhost, 
                    transform.position - transform.right * ((curRowNum - 1) * soldierDist / 2 - j * soldierDist) + transform.forward * (halfColLenght - i * soldierDist), 
                    Quaternion.Euler(0, rotation, 0), transform);
            remainingPositions -= curRowNum;
            formation[i] = curRow;
        }
        return this;
    }

}
