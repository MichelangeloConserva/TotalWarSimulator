using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeSoldier : BaseSoldier
{

    private MeleeUnit unit;


    public MeleeSoldier(GameObject g, BaseUnitStats bstat, MeleeUnit unit) : base(g, bstat)
    {
        
        this.unit = unit;
    }


}
