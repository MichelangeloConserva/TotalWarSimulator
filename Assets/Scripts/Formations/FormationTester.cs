using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationTester : MonoBehaviour
{

    public GameObject selectedUnit;


    private SquareFormation selectedFormation;
    
    void Start()
    {
        selectedFormation = selectedUnit.GetComponent<SquareFormation>().InitializeFormation(Vector3.zero, 0, 5, 12, 1.5f);
    }

    
    void Update()
    {
        
    }
}
