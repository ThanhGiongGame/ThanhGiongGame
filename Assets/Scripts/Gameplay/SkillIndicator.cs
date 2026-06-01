using UnityEngine;

/// <summary>
/// Runtime LineRenderer-based world-space indicators for skills.
/// Two modes: Ring (Skill 1) and LineAndCircle (Skill 2).
/// </summary>
public class SkillIndicator : MonoBehaviour
{
    private LineRenderer _ring;
    private LineRenderer _dashLine;
    private LineRenderer _endCircle;

    private const int Segments = 60;

    // ---- Factory methods ----

    public static SkillIndicator CreateRing(Color color)
    {
        GameObject go = new GameObject("SkillRingIndicator");
        SkillIndicator si = go.AddComponent<SkillIndicator>();
        si._ring = si.MakeLR(go, color, 0.10f, Segments + 1);
        return si;
    }

    public static SkillIndicator CreateLineAndCircle(Color lineColor, Color circleColor)
    {
        GameObject go = new GameObject("SkillLineIndicator");
        SkillIndicator si = go.AddComponent<SkillIndicator>();

        si._dashLine = si.MakeLR(go, lineColor, 0.10f, 2);

        GameObject cgo = new GameObject("EndCircle");
        cgo.transform.SetParent(go.transform, false);
        si._endCircle = si.MakeLR(cgo, circleColor, 0.07f, Segments + 1);

        return si;
    }

    // ---- Update calls ----

    public void UpdateRing(Vector3 center, float radius)
    {
        if (_ring == null) return;
        DrawCircle(_ring, center, radius);
    }

    public void UpdateLineAndCircle(Vector3 origin, Vector3 endPoint, float circleRadius)
    {
        if (_dashLine != null)
        {
            _dashLine.SetPosition(0, origin   + Vector3.up * 0.05f);
            _dashLine.SetPosition(1, endPoint  + Vector3.up * 0.05f);
        }
        if (_endCircle != null)
        {
            DrawCircle(_endCircle, endPoint, circleRadius);
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // ---- Internals ----

    private void DrawCircle(LineRenderer lr, Vector3 center, float radius)
    {
        lr.positionCount = Segments + 1;
        for (int i = 0; i <= Segments; i++)
        {
            float angle = (i / (float)Segments) * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(
                Mathf.Cos(angle) * radius,
                0.05f,
                Mathf.Sin(angle) * radius));
        }
    }

    private LineRenderer MakeLR(GameObject go, Color color, float width, int points)
    {
        LineRenderer lr       = go.AddComponent<LineRenderer>();
        lr.positionCount      = points;
        lr.startWidth         = width;
        lr.endWidth           = width;
        lr.useWorldSpace      = true;
        lr.loop               = false;
        lr.material           = BuildMat(color);
        lr.startColor         = color;
        lr.endColor           = new Color(color.r, color.g, color.b, 0.5f);
        lr.numCapVertices     = 4;
        lr.numCornerVertices  = 4;
        return lr;
    }

    private static Material BuildMat(Color color)
    {
        string[] candidates = {
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
            "Sprites/Default"
        };
        Shader shader = null;
        foreach (string s in candidates)
        {
            shader = Shader.Find(s);
            if (shader != null) break;
        }
        Material mat = new Material(shader ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        return mat;
    }
}
