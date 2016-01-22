using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJson;


[System.Serializable]
public class Bezier : System.Object
{
    public Vector3[] Points = new Vector3 [4];
    public Vector3 k1, k2, k3;

    public Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        SetPoint(p0, p1, p2, p3);
    }

    public Vector3 P0 { get { return Points[0]; } }
    public Vector3 P1 { get { return Points[1]; } }
    public Vector3 P2 { get { return Points[2]; } }
    public Vector3 P3 { get { return Points[3]; } }

    private float _length = 0f;
    public float Length {
        get {
            if (_length == 0f) {
                Vector3 last = P0;
                Vector3 current;
                float step = 0.01f;
                for (float t = step; t <= 1.001f; t += step) {
                    current = GetPoint(t);
                    _length += Vector3.Distance(last, current);
                    last = current;
                }
            }
            return _length;
        }
    }

    public void SetPoint(Vector3? p0 = null, Vector3? p1 = null, Vector3? p2 = null, Vector3? p3 = null)
    {
        bool changed = false;
        if (p0.HasValue && p0 != Points[0]) {
            Points[0] = (Vector3)p0;
            changed = true;
        }
        if (p1.HasValue && p1 != Points[1]) {
            Points[1] = (Vector3)p1;
            changed = true;
        }
        if (p2.HasValue && p2 != Points[2]) {
            Points[2] = (Vector3)p2;
            changed = true;
        }
        if (p3.HasValue && p3 != Points[3]) {
            Points[3] = (Vector3)p3;
            changed = true;
        }

        if (changed) {
            k1 = P1 - P0;
            k2 = P2 - 2f * P1 + P0;
            k3 = P3 - 3f * P2 + 3f * P1 - P0;
            _length = 0f;  // 只是重置长度, 需要时再计算.
        }
    }

    public Vector3 this[int index] {
        get {
            if (index < 0)
                return Points[Points.Length + index];
            else
                return Points[index];
        }
    }

    public Vector3 GetPoint(float t)
    {
        if (t <= 0f)
            return P0;
        else if (t >= 1f)
            return P3;
        else
            return P0 + 3f * k1 * t + 3f * k2 * Mathf.Pow(t, 2f) + k3 * Mathf.Pow(t, 3f);
    }

    public Vector3 GetDirection(float t)
    {
        if (t <= 0f)
            return P1 - P0;
        else if (t >= 1f)
            return P3 - P2;
        else
            return k3 * Mathf.Pow(t, 2f) + 2f * k2 * t + k1;
    }

    public float GetDeltaT(float t, float deltaDistance)
    {
        float delta = deltaDistance / Length;
        float l = GetDirection(t + delta).magnitude;
        return deltaDistance / l / 3f;
    }

    public IEnumerator MoveAlone(Transform objTransform, float speed)
    {
        float t = 0f;
        while (t < 1f) {
            objTransform.position = GetPoint(t);
            objTransform.rotation = Quaternion.LookRotation(Vector3.forward, GetDirection(t));
            yield return null;
            t += GetDeltaT(t, speed * Time.deltaTime);
        }
    }

    static public Bezier SymmetryTranslateByPoint(Bezier bz, Vector3 center)
    {
        Vector3[] points = new Vector3[4];

        for (int i = 0; i < 4; i++) {
            if (bz.Points[i] != center)
                points[i] = 2f * center - bz.Points[i];
            else
                points[i] = bz.Points[i];
        }

        return new Bezier(points[0], points[1], points[2], points[3]);
    }

    public void SymmetryTranslateByPoint(Vector3 center)
    {
        Bezier bz = Bezier.SymmetryTranslateByPoint(this, center);
        SetPoint(bz.P0, bz.P1, bz.P2, bz.P3);
    }

    static public Bezier SymmetryTranslateByAxis(Bezier bz, Vector3 axis)
    {
        Vector3[] points = new Vector3[4];

        if (axis.x != 0f && axis.y == 0f && axis.z == 0f) {
            for (int i = 0; i < 4; i++) {
                points[i] = bz.Points[i] * -1;
                points[i].x *= -1;
            }
        } else if (axis == Vector3.up || axis == Vector3.down) {
            for (int i = 0; i < 4; i++) {
                points[i] = bz.Points[i] * -1;
                points[i].y *= -1;
            }
        } else if (axis == Vector3.forward || axis == Vector3.back) {
            for (int i = 0; i < 4; i++) {
                points[i] = bz.Points[i] * -1;
                points[i].z *= -1;
            }
        } else
            throw new System.ArgumentException("Not a valid axis.");

        return new Bezier(points[0], points[1], points[2], points[3]);
    }

    public void SymmetryTranslateByAxis(Vector3 axis)
    {
        Bezier bz = Bezier.SymmetryTranslateByAxis(this, axis);
        SetPoint(bz.P0, bz.P1, bz.P2, bz.P3);
    }

    static public Bezier TranslateTo(Bezier bz, Vector3? startPoint = null, Vector3? startDirection = null)
    {
        Vector3[] points = (Vector3[])bz.Points.Clone();
        if (startPoint.HasValue) {
            Debug.Log(startPoint);
            Vector3 displacement = (Vector3)startPoint - bz.P0;
            for (int i = 0; i < 4; i++)
                points[i] += displacement;
        }

        if (startDirection.HasValue) {
            Vector3 direction = points[1] - points[0];
            float angle = Vector3.Angle(direction, (Vector3)startDirection);
            if (0f < angle && angle < 180f) {
                Quaternion rotation = Quaternion.FromToRotation(direction, (Vector3)startDirection);
                for (int j = 1; j < 4; j++)
                    points[j] = points[0] + rotation * (points[j] - points[0]);
            } else if (angle == 180f) {
                for (int j = 1; j < 4; j++)
                    points[j] = 2f * points[0] - points[j];
            }
        }

        return new Bezier(points[0], points[1], points[2], points[3]);
    }

    public void TranslateTo(Vector3? startPoint = null, Vector3? startDirection = null)
    {
        Bezier bz = Bezier.TranslateTo(this, startPoint, startDirection);
        SetPoint(bz.P0, bz.P1, bz.P2, bz.P3);
    }

    public void DebugDraw(float step = 0.05f, float duration = 0.01f, bool controller = false)
    {
        float t = 0f;
        Vector3 a, b;
        a = GetPoint(t);
        for (int i = 0; t < 1.001f; t += step) {
            b = GetPoint(t);
            Debug.DrawLine(a, b, i % 2 == 0 ? Color.red : Color.yellow, duration);
            a = b;
            i++;
        }
        if (controller) {
            Debug.DrawLine(P0, P1, Color.gray, duration);
            Debug.DrawLine(P3, P2, Color.gray, duration);
        }
    }
}


[System.Serializable]
public class BezierGroup : System.Object
{
    public List<Bezier> beziers = new List<Bezier>();
    public int? loopFrom = null;

    public BezierGroup(params Bezier[] args)
    {
        for (int i = 0; i < args.Length; i++) {
            AddBizer(args[i]);
        }
    }

    public int Count { get { return beziers.Count; } }

    public float Length {
        get {
            float length = 0f;
            for (int i = 0; i < beziers.Count; i++)
                length += beziers[i].Length;
            return length;
        }
    }

    public Vector3[] Points {
        get {
            Vector3[] points = new Vector3[beziers.Count * 3 + 1];
            points[0] = this[0, 0];
            for (int i = 0; i < Count; i++)
                for (int j = i; j < 4; j++)
                    points[i * 3 + j] = this[i, j];
            return points;
        }
    }

    public Bezier this[int index] {
        get {
            if (index < 0)
                return beziers[beziers.Count + index];
            else
                return beziers[index];
        }
    }

    public Vector3 this[int bzIndex, int pIndex] {
        get {
            if (bzIndex < 0)
                return beziers[beziers.Count + bzIndex][pIndex];
            else
                return beziers[bzIndex][pIndex];
        }
    }

    public IEnumerator GetEnumerator()
    {
        return beziers.GetEnumerator();
    }

    public void AddBizer(Bezier bz)
    {
        // check and add the new bezier
        if (beziers.Count == 0) {
            beziers.Add(bz);
        } else if (this[-1].P3 == bz.P0) {
            bz.SetPoint(p1: ClampOppositePoint(this[-1].P2, bz.P0, bz.P1));
            beziers.Add(bz);
        } else
            throw new System.ArgumentException("Not a continuous BezierGroup.");
        // check loop
        loopFrom = null;
        for (int i = 0; i < beziers.Count; i++) {
            if (this[-1].P3 == this[i].P0) {
                loopFrom = i;
                break;
            }
        }
    }

    public Vector3 ClampOppositePoint(Vector3 self, Vector3 center, Vector3 opposite)
    {
        Vector3 v1 = self - center;
        Vector3 v2 = opposite - center;
        float angle = Vector3.Angle(v1, v2);

        if (angle == 0f) {
            return center - v2;
        } else if (0f < angle && angle < 180) {
            float l1 = Vector3.Distance(Vector3.zero, v1);
            float l2 = Vector3.Distance(Vector3.zero, v2);
            return center - v1 * (l2 / l1);
        } else
            return opposite;
    }

    public static BezierGroup FromJsonData(JsonObject data)
    {
        BezierGroup bzg = new BezierGroup();
        List<string> keys = new List<string>();
        foreach (var k in data.Keys) {
            keys.Add((string)k);
        }
        keys.Sort();

        List<Vector3> tempP = new List<Vector3>();

        for (int i = 0; i < keys.Count; i++) {
            string k = keys[i];
            List<object> xy = (List<object>)data[k];
            Vector3 p = new Vector3((float)xy[0], (float)xy[1]);
            tempP.Add(p);
            if (tempP.Count == 4) {
                var bz = new Bezier(tempP[0], tempP[1], tempP[2], tempP[3]);
                bzg.AddBizer(bz);
                tempP.Clear();
                tempP.Add(p);
            }
        }
        return bzg;
    }

    public static BezierGroup Transformation(BezierGroup bzgroup, Vector3? startPoint = null, Vector3? startDirection = null,
                                             Vector3? symmetryPoint = null, Vector3? symmetryAxis = null)
    {
        BezierGroup newGroup = new BezierGroup();

        for (int i = 0; i < bzgroup.Count; i++) {
            Bezier bz = new Bezier(bzgroup[i].P0, bzgroup[i].P1, bzgroup[i].P2, bzgroup[i].P3);
            // 如果 symmetryPoint 和 symmetryAxis 同时存在, 优先处理 symmetryPoint.
            if (symmetryPoint.HasValue)
                bz.SymmetryTranslateByPoint((Vector3)symmetryPoint);
            if (symmetryAxis.HasValue)
                bz.SymmetryTranslateByAxis((Vector3)symmetryAxis);
            if (startPoint.HasValue || startDirection.HasValue) {
                if (i == 0) {
                    bz.TranslateTo(startPoint, startDirection);
                } else {
                    bz.TranslateTo(newGroup[-1].P3, newGroup[-1].P3 - newGroup[-1].P2);
                }
            }
            newGroup.AddBizer(bz);
        }
        return newGroup;
    }

    public static BezierGroup Reverse(BezierGroup bzgroup)
    {
        BezierGroup newGroup = new BezierGroup();
        for (int i = bzgroup.Count - 1; i >= 0; i--) {
            Bezier bz = new Bezier(bzgroup[i].P3, bzgroup[i].P2, bzgroup[i].P1, bzgroup[i].P0);
            newGroup.AddBizer(bz);
        }
        return newGroup;
    }

    public IEnumerator MoveAlone(Transform objTransform, float speed)
    {
        foreach (Bezier bz in beziers) {
            float t = 0f;
            while (t < 1f) {
                objTransform.position = bz.GetPoint(t);
                objTransform.rotation = Quaternion.LookRotation(Vector3.forward, bz.GetDirection(t));
                yield return null;
                t += bz.GetDeltaT(t, speed * Time.deltaTime);
            }
        }
    }

    public void DebugDraw(float step = 0.05f, float duration = 0.01f, bool controller = false)
    {
        for (int i = 0; i < beziers.Count; i++) {
            beziers[i].DebugDraw(step, duration, controller);
        }
    }
}


public class BezierMotion : MonoBehaviour
{
    public BezierGroup curve;
    public float V;
    public bool smoothStart = false;
    public bool allowLoop = false;
    public Vector3 toward;

    private float currentT = 0f;
    private int currentIndex = 0;

    public bool atStart { get { return currentIndex == 0 && currentT == 0f; } }
    public bool atEnd { get { return currentIndex == (curve.Count - 1) && currentT >= 1.0f; } }

    public void Initialize(BezierGroup curve, float velocity, Vector3? toward = null,
                           bool smoothStart = false, bool allowLoop = false)
    {
        this.curve = curve;
        this.V = velocity;
        if (toward.HasValue)
            this.toward = (Vector3)toward;
        this.smoothStart = smoothStart;
        this.allowLoop = allowLoop;
    }

    void OnEnable()
    {
        Reset();
    }

    void Update()
    {
        curve.DebugDraw();
        float distance = V * Time.deltaTime;
        if (atStart && smoothStart && transform.position != curve[0, 0]) {
            transform.position = Vector3.MoveTowards(transform.position, curve[0, 0], distance);
            if (toward == Vector3.zero)
                transform.rotation = Quaternion.LookRotation(Vector3.forward, curve[0, 0] - transform.position);
        } else if (!atEnd) {
            if (currentT < 1f)
                currentT += curve[currentIndex].GetDeltaT(currentT, distance);
            else
                currentT = curve[++currentIndex].GetDeltaT(0f, distance);
            SetStatus();
        } else if (allowLoop && curve.loopFrom.HasValue) {
            currentIndex = (int)curve.loopFrom;
            currentT = curve[currentIndex].GetDeltaT(0f, distance);
            SetStatus();
        }
    }

    public void Reset()
    {
        currentT = 0f;
        currentIndex = 0;
        if (toward != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(Vector3.forward, toward);
    }

    void SetStatus()
    {
        transform.position = curve[currentIndex].GetPoint(currentT);
        if (toward == Vector3.zero) {
            Vector3 currToward = curve[currentIndex].GetDirection(currentT);
            transform.rotation = Quaternion.LookRotation(Vector3.forward, currToward);
        }
    }
}
