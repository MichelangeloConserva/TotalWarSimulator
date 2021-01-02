using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeStats : MonoBehaviour
{
    [System.Serializable]
    public struct MeleeStatsHolder
    {

        public float topSpeed;
        public float movementForce;
        public float meeleRange;
        public float health;
        public float meeleDefence;
        public float meeleAttack;
        public float pathSpeed;
        public float soldierDistVertical;
        public float soldierDistLateral;

        public int startingNumOfSoldiers;
        public int startingCols;
    }

    public GameObject soldierPrefab;
    public MeleeStatsHolder meleeHolder;
}
