using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CustomItems
{
    // [2026-10-07] Icon pipeline: copy an existing item's sprite (works for atlas
    // sub-rects via a RenderTexture round-trip, so the source texture does NOT need
    // to be CPU-readable) and optionally multiply it by a tint color. Used for
    // Satay Madura = Roasted Droop Core sprite tinted red.

    public static class SpriteTinter
    {
        public static Sprite TintFromSprite(Sprite source, Color tint, Action<string> warn)
        {
            if (source == null) { warn("TintFromSprite: source sprite is null."); return null; }
            try
            {
                Texture2D srcTex = source.texture;
                if (srcTex == null) { warn("TintFromSprite: source texture is null."); return null; }

                Rect rect = source.rect;
                int w = Mathf.Max(1, Mathf.RoundToInt(rect.width));
                int h = Mathf.Max(1, Mathf.RoundToInt(rect.height));

                // Round-trip through a RenderTexture so we can ReadPixels even when the
                // source texture is not marked Read/Write enabled.
                RenderTexture rt = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0, RenderTextureFormat.ARGB32);
                RenderTexture prev = RenderTexture.active;
                Graphics.Blit(srcTex, rt);
                RenderTexture.active = rt;

                Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(rect.x, rect.y, w, h), 0, 0);
                copy.Apply();

                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);

                Color[] px = copy.GetPixels();
                for (int i = 0; i < px.Length; i++)
                {
                    px[i].r *= tint.r;
                    px[i].g *= tint.g;
                    px[i].b *= tint.b;
                    px[i].a *= tint.a;
                }
                copy.SetPixels(px);
                copy.Apply();

                Vector2 pivotPx = source.pivot;
                Vector2 pivot = new Vector2(pivotPx.x / w, pivotPx.y / h);
                return Sprite.Create(copy, new Rect(0, 0, w, h), pivot, source.pixelsPerUnit);
            }
            catch (Exception ex)
            {
                warn($"TintFromSprite failed: {ex.Message}");
                return source;
            }
        }

        public static Sprite SolidColor(Color color)
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] px = new Color[32 * 32];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
        }

        public static bool TryParseColor(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(hex)) return false;
            return ColorUtility.TryParseHtmlString(hex, out color);
        }
    }
}
