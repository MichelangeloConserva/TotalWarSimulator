using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Formation : MonoBehaviour
{
    
    public abstract Vector3 GetPosition();
    public abstract float GetRotation();
    public abstract float GetSoldierDist();
    public abstract int GetCols();
    public abstract int GetNumSoldiers();





}
