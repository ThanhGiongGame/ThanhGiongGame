using UnityEngine;
using System.Collections.Generic;

public class LegendaryUpgradeSystem : MonoBehaviour
{
    private LegendSystemCoLoa coLoa;
    private LegendSystemDongA dongA;
    private LegendSystemSonTinh sonTinh;
    private LegendSystemThanhGiong thanhGiong;
    private LegendSystemLeLoi leLoi;

    public void UpdateLegendLevels(LegendSystemType type, int w1, int w2, int evo)
    {
        Debug.Log($"[Legend System] {type} Updated! W1: {w1}, W2: {w2}, Evo: {evo}");
        
        switch (type)
        {
            case LegendSystemType.CoLoa:
                if (coLoa == null) coLoa = gameObject.AddComponent<LegendSystemCoLoa>();
                coLoa.UpdateLevels(w1, w2, evo);
                break;
            case LegendSystemType.DongA:
                if (dongA == null) dongA = gameObject.AddComponent<LegendSystemDongA>();
                dongA.UpdateLevels(w1, w2, evo);
                break;
            case LegendSystemType.SonTinh:
                if (sonTinh == null) sonTinh = gameObject.AddComponent<LegendSystemSonTinh>();
                sonTinh.UpdateLevels(w1, w2, evo);
                break;
            case LegendSystemType.ThanhGiong:
                if (thanhGiong == null) thanhGiong = gameObject.AddComponent<LegendSystemThanhGiong>();
                thanhGiong.UpdateLevels(w1, w2, evo);
                break;
            case LegendSystemType.LeLoi:
                if (leLoi == null) leLoi = gameObject.AddComponent<LegendSystemLeLoi>();
                leLoi.UpdateLevels(w1, w2, evo);
                break;
        }
    }
}
