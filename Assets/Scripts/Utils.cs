using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public enum UnitStatus { WALKING, RUNNING, CHARGING }
    public enum Cardinal { NW, N, NE, E, SE, S, SW, W }


    public static Vector3 GetVector3Down(Vector3 v) { return v - Vector3.up * v.y; }

    public static float AngleToTurn(Vector3 targetPos, Vector3 startPos, Vector3 startDirection)
    {
        Vector3 cross = Vector3.Cross(startDirection, (targetPos - startPos).normalized);
        return Mathf.Clamp(cross.y, -1, 1);
    }

    public static Vector3 GetMousePosInWorld()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
            return GetVector3Down(hit.point);
        Debug.LogError("Terrain not found on click");
        return Vector3.zero;
    }



}
