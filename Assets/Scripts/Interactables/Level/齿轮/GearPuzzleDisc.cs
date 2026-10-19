using UnityEngine;
using UnityEngine.UI;

/// <summary>Simple UI circles/rings for axles and drag feedback.</summary>
public class GearPuzzleDisc : MaskableGraphic
{
    [Range(0,1)] public float innerRatio;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Vector2 center = rectTransform.rect.center;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
        const int n = 64;
        for (int i=0; i<n; i++)
        {
            float a = i * Mathf.PI*2/n, b = (i+1)*Mathf.PI*2/n;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a))*radius;
            Vector2 q = new Vector2(Mathf.Cos(b), Mathf.Sin(b))*radius;
            int k=vh.currentVertCount;
            vh.AddVert(center+p,color,Vector2.zero); vh.AddVert(center+q,color,Vector2.zero);
            vh.AddVert(center+q*innerRatio,color,Vector2.zero); vh.AddVert(center+p*innerRatio,color,Vector2.zero);
            vh.AddTriangle(k,k+1,k+2); vh.AddTriangle(k,k+2,k+3);
        }
    }
}
