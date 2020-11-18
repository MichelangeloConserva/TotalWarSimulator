using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

[ExecuteInEditMode]
public class UnitCreator : MonoBehaviour
{

    public GameObject soldierBase, connector;
    public int cols;
    public int numOfSoldiers;
    public float soldierDistLateral;
    public float soldierDistVertical;
    //public Vector3 starPos;
    // public Vector3 direction;

    public bool updateFormation;


    public GameObject[][] soldiersInFormation;

    public FormationResult fRes;
    public bool withJoints = false;


    public List<GameObject> GetSoldiers()
    {
        List<GameObject> soldiers = new List<GameObject>();
        for (int row = 0; row < soldiersInFormation.Length; row++)
            for (int col = 0; col < soldiersInFormation[row].Length; col++)
                soldiers.Add(soldiersInFormation[row][col]);
        return soldiers;
    }



    private void CleanUnit()
    {
        var tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
            DestroyImmediate(child.gameObject);
    }


    private void InstantiateUnit()
    {
        CleanUnit();

        fRes = GetFormAtPos(transform.position, transform.forward, numOfSoldiers, cols, soldierDistLateral, soldierDistVertical);
        Vector3[][] formationPositions = fRes.positions;
        soldiersInFormation = new GameObject[formationPositions.Length][];
        for (int i = 0; i < formationPositions.Length; i++)
        {
            GameObject[] curRow = new GameObject[formationPositions[i].Length];
            for (int j = 0; j < curRow.Length; j++)
                curRow[j] = Instantiate(soldierBase, formationPositions[i][j], transform.rotation, transform);
            soldiersInFormation[i] = curRow;
        }

    }


    private void Start()
    {
        if (Application.isPlaying)
            InstantiateUnit();
    }


    // Update is called once per frame
    void Update()
    {
        if (!Application.isPlaying)
        {
            if (updateFormation)
                InstantiateUnit();
        }
    }
}
