using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class HungarianAlgorithm2
{
    private readonly int[,] _costMatrix;
    private int _inf;
    private int _n; //number of elements
    private int[] _lx; //labels for workers
    private int[] _ly; //labels for jobs 
    private bool[] _s;
    private bool[] _t;
    private int[] _matchX; //vertex matched with x
    private int[] _matchY; //vertex matched with y
    private int _maxMatch;
    private int[] _slack;
    private int[] _slackx;
    private int[] _prev; //memorizing paths

    /// <summary>
    /// 
    /// </summary>
    /// <param name="costMatrix"></param>
    public HungarianAlgorithm2(int[,] costMatrix)
    {
        _costMatrix = costMatrix;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int[] Run()
    {
        _n = _costMatrix.GetLength(0);

        _lx = new int[_n];
        _ly = new int[_n];
        _s = new bool[_n];
        _t = new bool[_n];
        _matchX = new int[_n];
        _matchY = new int[_n];
        _slack = new int[_n];
        _slackx = new int[_n];
        _prev = new int[_n];
        _inf = int.MaxValue;


        InitMatches();

        if (_n != _costMatrix.GetLength(1))
            return null;

        InitLbls();

        _maxMatch = 0;

        InitialMatching();

        var q = new Queue<int>();

        #region augment

        while (_maxMatch != _n)
        {
            q.Clear();

            InitSt();
            //Array.Clear(S,0,n);
            //Array.Clear(T, 0, n);


            //parameters for keeping the position of root node and two other nodes
            var root = 0;
            int x;
            var y = 0;

            //find root of the tree
            for (x = 0; x < _n; x++)
            {
                if (_matchX[x] != -1) continue;
                q.Enqueue(x);
                root = x;
                _prev[x] = -2;

                _s[x] = true;
                break;
            }

            //init slack
            for (var i = 0; i < _n; i++)
            {
                _slack[i] = _costMatrix[root, i] - _lx[root] - _ly[i];
                _slackx[i] = root;
            }

            //finding augmenting path
            while (true)
            {
                while (q.Count != 0)
                {
                    x = q.Dequeue();
                    var lxx = _lx[x];
                    for (y = 0; y < _n; y++)
                    {
                        if (_costMatrix[x, y] != lxx + _ly[y] || _t[y]) continue;
                        if (_matchY[y] == -1) break; //augmenting path found!
                        _t[y] = true;
                        q.Enqueue(_matchY[y]);

                        AddToTree(_matchY[y], x);
                    }
                    if (y < _n) break; //augmenting path found!
                }
                if (y < _n) break; //augmenting path found!
                UpdateLabels(); //augmenting path not found, update labels

                for (y = 0; y < _n; y++)
                {
                    //in this cycle we add edges that were added to the equality graph as a
                    //result of improving the labeling, we add edge (slackx[y], y) to the tree if
                    //and only if !T[y] &&  slack[y] == 0, also with this edge we add another one
                    //(y, yx[y]) or augment the matching, if y was exposed

                    if (_t[y] || _slack[y] != 0) continue;
                    if (_matchY[y] == -1) //found exposed vertex-augmenting path exists
                    {
                        x = _slackx[y];
                        break;
                    }
                    _t[y] = true;
                    if (_s[_matchY[y]]) continue;
                    q.Enqueue(_matchY[y]);
                    AddToTree(_matchY[y], _slackx[y]);
                }
                if (y < _n) break;
            }

            _maxMatch++;

            //inverse edges along the augmenting path
            int ty;
            for (int cx = x, cy = y; cx != -2; cx = _prev[cx], cy = ty)
            {
                ty = _matchX[cx];
                _matchY[cy] = cx;
                _matchX[cx] = cy;
            }
        }

        #endregion

        return _matchX;
    }

    private void InitMatches()
    {
        for (var i = 0; i < _n; i++)
        {
            _matchX[i] = -1;
            _matchY[i] = -1;
        }
    }

    private void InitSt()
    {
        for (var i = 0; i < _n; i++)
        {
            _s[i] = false;
            _t[i] = false;
        }
    }

    private void InitLbls()
    {
        for (var i = 0; i < _n; i++)
        {
            var minRow = _costMatrix[i, 0];
            for (var j = 0; j < _n; j++)
            {
                if (_costMatrix[i, j] < minRow) minRow = _costMatrix[i, j];
                if (minRow == 0) break;
            }
            _lx[i] = minRow;
        }
        for (var j = 0; j < _n; j++)
        {
            var minColumn = _costMatrix[0, j] - _lx[0];
            for (var i = 0; i < _n; i++)
            {
                if (_costMatrix[i, j] - _lx[i] < minColumn) minColumn = _costMatrix[i, j] - _lx[i];
                if (minColumn == 0) break;
            }
            _ly[j] = minColumn;
        }
    }

    private void UpdateLabels()
    {
        var delta = _inf;
        for (var i = 0; i < _n; i++)
            if (!_t[i])
                if (delta > _slack[i])
                    delta = _slack[i];
        for (var i = 0; i < _n; i++)
        {
            if (_s[i])
                _lx[i] = _lx[i] + delta;
            if (_t[i])
                _ly[i] = _ly[i] - delta;
            else _slack[i] = _slack[i] - delta;
        }
    }

    private void AddToTree(int x, int prevx)
    {
        //x-current vertex, prevx-vertex from x before x in the alternating path,
        //so we are adding edges (prevx, matchX[x]), (matchX[x],x)

        _s[x] = true; //adding x to S
        _prev[x] = prevx;

        var lxx = _lx[x];
        //updateing slack
        for (var y = 0; y < _n; y++)
        {
            if (_costMatrix[x, y] - lxx - _ly[y] >= _slack[y]) continue;
            _slack[y] = _costMatrix[x, y] - lxx - _ly[y];
            _slackx[y] = x;
        }
    }

    private void InitialMatching()
    {
        for (var x = 0; x < _n; x++)
        {
            for (var y = 0; y < _n; y++)
            {
                if (_costMatrix[x, y] != _lx[x] + _ly[y] || _matchY[y] != -1) continue;
                _matchX[x] = y;
                _matchY[y] = x;
                _maxMatch++;
                break;
            }
        }
    }
}


public static class Utils
{
    // public enum UnitStatus { IDLE, CHARGING }
    public enum Cardinal { NW, N, NE, E, SE, S, SW, W }
    public enum UnitState { IDLE, MOVING, FIGHTING, ESCAPING }
    public enum UnitCombactState { ATTACKING, DEFENDING }
    public enum UnitMovementState { WALKING, RUNNING }
    public enum ArmyRole { ATTACKER, DEFENDER }


    public static int GetNumRows(int numOfSoldiers, int cols) { return (int)Mathf.Ceil(numOfSoldiers / (float)cols); }
    public static float GetHalfLenght(float dist, int num) { return (num - 1) * dist / 2; }
    public static Vector3[] GetEquispacedRow(int cols, float lateralDist, Vector3 pos, Vector3 dir)
    {
        Vector3[] curRow = new Vector3[cols];
        for (int j = 0; j < cols; j++)
            curRow[j] = pos - Vector3.Cross(Vector3.up, dir) * ((cols - 1) * lateralDist / 2 - j * lateralDist);
        return curRow;
    }


    public static float3[] GetFormationAtPos(float3 pos, float3 dir, int numOfSoldiers, int cols, float lateralDist, float verticalDist)
    {

        dir = math.normalize(dir);
        var numOfRows = GetNumRows(numOfSoldiers, cols);
        int remainingPositions = numOfSoldiers;
        var halfColLenght = GetHalfLenght(verticalDist, numOfRows);


        var allPositions = new float3[numOfSoldiers];

        int k = 0;
        for (int i = 0; i < numOfRows; i++)
        {
            int curRowNum = remainingPositions > cols ? cols : remainingPositions;
            float3 curPos = pos + dir * (halfColLenght - i * verticalDist);

            for (int j = 0; j < curRowNum; j++)
                allPositions[k++] = curPos - math.cross(new float3(0, 1, 0), dir) * ((curRowNum - 1) * lateralDist / 2 - j * lateralDist);

            remainingPositions -= curRowNum;
        }

        return allPositions;
    }


    public static int[] LSCAssignment(float3[] startPositions, float3[] endPositions)
    {
        double[,] cost = new double[startPositions.Length, endPositions.Length];
        for (int i = 0; i < startPositions.Length; i++)
            for (int j = 0; j < endPositions.Length; j++)
                cost[i, j] = math.distance(startPositions[i], endPositions[j]);
        var res = LinearAssignment.Solver.Solve(cost);
        return res.ColumnAssignment;

        //int length = startPositions.Length;
        //int[,] cost = new int[length, length];
        //for (int j = 0; j < length; j++)
        //    for (int k = 0; k < length; k++)
        //        cost[j, k] = (int)(1000 * Vector3.Distance(startPositions[j], endPositions[k]));
        //return HungarianAlgorithm.HungarianAlgorithm.FindAssignments(cost);
    }
    public static int[] LSCAssignment(NativeArray<float3> startPositions, NativeArray<float3> endPositions)
    {
        double[,] cost = new double[startPositions.Length, endPositions.Length];
        for (int i = 0; i < startPositions.Length; i++)
            for (int j = 0; j < endPositions.Length; j++)
                cost[i, j] = math.distance(startPositions[i], endPositions[j]);
        var res = LinearAssignment.Solver.Solve(cost);
        return res.ColumnAssignment;

        //int length = startPositions.Length;
        //int[,] cost = new int[length, length];
        //for (int j = 0; j < length; j++)
        //    for (int k = 0; k < length; k++)
        //        cost[j, k] = (int)(1000 * Vector3.Distance(startPositions[j], endPositions[k]));
        //return HungarianAlgorithm.HungarianAlgorithm.FindAssignments(cost);
    }



    public static float3 GetVector3Down(float3 v) { return v - new float3(0,v.y,0); }
    public static float AngleToTurn(Vector3 targetPos, Vector3 startPos, Vector3 startDirection)
    {
        Vector3 cross = Vector3.Cross(startDirection, (targetPos - startPos).normalized);
        return Mathf.Clamp(cross.y, -1, 1);
    }
    public static Vector3 GetMousePosInWorld()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, LayerMask.GetMask("Terrain")))
            return GetVector3Down(hit.point);
        Debug.LogError("Terrain not found on click");
        return Vector3.zero;
    }


    public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color, float time)
    {
        DrawBox(new Box(origin, halfExtents, orientation), color, time);
    }
    public static void DrawBox(Box box, Color color, float time)
    {
        Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color, time);
        Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color, time);
        Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color, time);
        Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color, time);

        Debug.DrawLine(box.backTopLeft, box.backTopRight, color, time);
        Debug.DrawLine(box.backTopRight, box.backBottomRight, color, time);
        Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color, time);
        Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color, time);

        Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color, time);
        Debug.DrawLine(box.frontTopRight, box.backTopRight, color, time);
        Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color, time);
        Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color, time);
    }



    public struct Box
    {
        public Vector3 localFrontTopLeft { get; private set; }
        public Vector3 localFrontTopRight { get; private set; }
        public Vector3 localFrontBottomLeft { get; private set; }
        public Vector3 localFrontBottomRight { get; private set; }
        public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
        public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
        public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
        public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

        public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
        public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
        public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
        public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
        public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
        public Vector3 backTopRight { get { return localBackTopRight + origin; } }
        public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
        public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

        public Vector3 origin { get; private set; }

        public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
        {
            Rotate(orientation);
        }
        public Box(Vector3 origin, Vector3 halfExtents)
        {
            this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
            this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
            this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
            this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

            this.origin = origin;
        }


        public void Rotate(Quaternion orientation)
        {
            localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
            localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
            localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
            localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
        }
    }
    static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
    {
        return origin + (direction.normalized * hitInfoDistance);
    }
    static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        Vector3 direction = point - pivot;
        return pivot + rotation * direction;
    }


}
