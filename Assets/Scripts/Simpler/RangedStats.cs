using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedStats : MonoBehaviour
{
    [System.Serializable]
    public struct RangedStatsHolder
    {
        public float arrowDamage;
        public float range;
        public float fireInterval;
        public bool freeFire;
    }

    public GameObject arrow;
    public RangedStatsHolder rangedHolder;

}
