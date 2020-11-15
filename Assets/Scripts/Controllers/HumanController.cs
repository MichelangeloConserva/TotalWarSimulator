using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;


[ExecuteInEditMode]
public class HumanController : MonoBehaviour
{

    public float acceleration;

    public GameObject unitToAttack;


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {


        if (!Application.isPlaying)
        {

            var enemy = unitToAttack.GetComponent<UnitCreator>();
            var closestEnemy = enemy.soldiers.OrderBy(es => Vector3.Distance(transform.position, es.transform.position)).First();
            Debug.DrawLine(transform.position + Vector3.up, closestEnemy.transform.position + Vector3.up, Color.green);



            var diameter = GetComponent<UnitCreator>().soldierBase.transform.localScale.x;
            var dir = closestEnemy.transform.position - transform.position;
            var targetPos = transform.position + dir.normalized * (dir.magnitude + diameter);
            Debug.DrawLine(transform.position + Vector3.up*2, targetPos + Vector3.up*2, Color.blue);



            var rowDir = Vector3.Cross(Vector3.up, dir.normalized);
            Debug.DrawRay(targetPos + Vector3.up * 2, rowDir, Color.blue);










        }
        else
        {
            var angle = Input.GetAxis("Horizontal");
            var speed = Input.GetAxis("Vertical");

            for (int i = 0; i < transform.childCount; i++)
            {
                var rb = transform.GetChild(i).GetComponent<Rigidbody>();
                if (!rb) continue;
                if (rb.velocity.magnitude < 15)
                    rb.AddForce(transform.forward * speed * acceleration, ForceMode.Force);
            }

            transform.Rotate(Vector3.up, angle);

            var soldiers = GetComponent<UnitCreator>().soldiers;

        }






        //if (Input.GetMouseButtonDown(0))
        //{
        //    Vector3 mouseClick = GetMousePosInWorld();
        //    if (selectedUnit)
        //        selectedUnit.GetComponent<UnitController>().MoveAtPos(mouseClick);
        //}


    }
}
