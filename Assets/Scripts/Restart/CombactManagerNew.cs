using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombactManagerNew : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private WaitForEndOfFrame wfeof;
    IEnumerator UpdateCombactManager()
    {
        wfeof = new WaitForEndOfFrame();


        int k = 0;
        while (true)
        {

            if (k < 5)
            {
                k++;
                yield return wfeof;
            }
            else
                k = 0;


            if (!Application.isPlaying)
            {
                Start();
            }


            PreUpdate();
        }
    }
}