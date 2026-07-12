using UnityEngine;

public static class LegendVisualHelper
{
    /// <param name="travelDirection">For projectiles: the sprite's local DOWN rotates toward this world direction. Zero = no rotation.</param>
    /// <param name="spriteScale">Scale of the sprite. 1.0 = sprite natural size at 100 pixels-per-unit.</param>
    /// <param name="spherical">True = sprite faces camera directly (best for projectiles/arrows). False = cylindrical (stays upright, best for standing objects).</param>
    public static GameObject CreateVisual(string spriteName, PrimitiveType fallbackPrimitive, Color fallbackColor,
        float emissionIntensity = 0f, bool billboard = true, bool isFlat = false,
        Vector3 travelDirection = default, float spriteScale = 1.2f, bool spherical = false)
    {
        // Try to load Sprite from Assets/Resources/Sprites/
        Sprite sprite = Resources.Load<Sprite>("Sprites/" + spriteName);
        if (sprite != null)
        {
            GameObject go = new GameObject(spriteName);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color  = Color.white;
            sr.transform.localScale = Vector3.one * spriteScale;

            // Add a 3D collider to the 2D sprite so it registers 3D physics collisions
            if (fallbackPrimitive == PrimitiveType.Sphere)
            {
                var col = go.AddComponent<SphereCollider>();
                col.radius = 0.5f * spriteScale;
            }
            else
            {
                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(1f, 1f, 0.1f);
            }

            if (emissionIntensity > 0f)
            {
                sr.material = BuildEmitMaterial(fallbackColor, emissionIntensity);
            }

            if (billboard)
            {
                var bb = go.AddComponent<Billboard>();
                bb.travelDirection = travelDirection;
                bb.useSpherical    = spherical;
                // Blob shadow beneath the sprite — makes it look grounded
                LegendParticles.AddBlobShadow(go, size: spriteScale * 0.6f, yOffset: -spriteScale * 0.5f);
            }
            else if (isFlat)
            {
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            return go;
        }
        else
        {
            // Primitive fallback — scale it up too
            GameObject go = GameObject.CreatePrimitive(fallbackPrimitive);
            go.name = spriteName + "_PrimitiveFallback";
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = BuildEmitMaterial(fallbackColor, emissionIntensity);
            }
            return go;
        }
    }


    public static Material BuildEmitMaterial(Color color, float emissionIntensity)
    {
        // URP Unlit or Standard — try URP first as it supports bloom
        string[] candidates = {
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Lit",
            "Standard",
            "Unlit/Color"
        };
        
        Shader shader = null;
        foreach (string s in candidates)
        {
            shader = Shader.Find(s);
            if (shader != null) break;
        }
        
        Material mat = new Material(shader ?? Shader.Find("Standard"));
        
        // ---- HDR base color ----
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

        if (emissionIntensity > 0f)
        {
            // Moderate HDR: 1.5x is enough to glow without overwhelming everything
            Color hdrEmission = color * Mathf.Max(emissionIntensity, 1.5f);
            
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", hdrEmission);
            if (mat.HasProperty("_Emission"))
                mat.SetColor("_Emission", hdrEmission);
            
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        
        // Standard Alpha blend (not additive)
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
        if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   0f); // Alpha blend
        
        // Disable backface culling so 2D planes/sprites are visible from both sides (good for orbiting scrolls)
        if (mat.HasProperty("_Cull"))    mat.SetFloat("_Cull",    0f);
        
        if (mat.HasProperty("_SrcBlend") && mat.HasProperty("_DstBlend"))
        {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",   0);
            mat.renderQueue = 3000;
        }
        
        return mat;
    }
}
