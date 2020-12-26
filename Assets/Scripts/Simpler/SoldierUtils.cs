using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class SoldierUtils : MonoBehaviour
{

    private CUnit cunit;



    private void OnDrawGizmos()
    {

    }

    private void Start()
    {
        cunit = GetComponentInParent<CUnit>();
    }


    private void Update()
    {
    }



}
