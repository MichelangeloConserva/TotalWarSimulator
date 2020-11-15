using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class HumanController : MonoBehaviour
{

    public GameObject selectedUnit;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseClick = GetMousePosInWorld();
            if (selectedUnit)
                selectedUnit.GetComponent<UnitController>().MoveAtPos(mouseClick);
        }





    }
}
