using System;
using UnityEngine;

namespace ARFurniture
{
    // 圖片解碼：PNG/JPG 用 Unity LoadImage；WebP 在 Android 用原生 BitmapFactory 解(轉成 PNG 再餵回 LoadImage)。
    // 編輯器/其他平台不支援 runtime WebP，會回 null(圖不顯示但不會壞)。
    public static class ImageLoader
    {
        public static Sprite SpriteFromBytes(byte[] data, string name)
        {
            Texture2D tex = DecodeToTexture(data, name);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        public static Texture2D DecodeToTexture(byte[] data, string name)
        {
            if (data == null || data.Length == 0) return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(data)) { tex.name = name; return tex; }   // PNG / JPG
            UnityEngine.Object.Destroy(tex);

#if IMAGESHARP
            // 純託管解碼(ImageSharp)：編輯器 + 所有平台都能解 WebP
            var managed = DecodeWithImageSharp(data, name);
            if (managed != null) return managed;
#endif

            // LoadImage 失敗 → 可能是 WebP，試平台原生解碼(Android)
            byte[] png = TryDecodeWebPToPng(data);
            if (png != null)
            {
                var t2 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (t2.LoadImage(png)) { t2.name = name; return t2; }
                UnityEngine.Object.Destroy(t2);
            }

            Debug.LogWarning($"[ImageLoader] 無法解碼圖片: {name}（非 PNG/JPG，且平台無法解 WebP）");
            return null;
        }

#if IMAGESHARP
        // 純託管解碼(SixLabors.ImageSharp)。需安裝 ImageSharp 並加 scripting define: IMAGESHARP
        static Texture2D DecodeWithImageSharp(byte[] data, string name)
        {
            try
            {
                using (var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(data))
                {
                    int w = img.Width, h = img.Height;
                    var pixels = new Color32[w * h];
                    img.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < h; y++)
                        {
                            var row = accessor.GetRowSpan(y);
                            int ty = h - 1 - y;            // ImageSharp 由上而下、Unity 由下而上 → 翻 Y
                            int baseIdx = ty * w;
                            for (int x = 0; x < w; x++)
                            {
                                var p = row[x];
                                pixels[baseIdx + x] = new Color32(p.R, p.G, p.B, p.A);
                            }
                        }
                    });
                    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tex.SetPixels32(pixels);
                    tex.Apply(false);
                    tex.name = name;
                    return tex;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ImageLoader] ImageSharp 解碼失敗: {e.Message}");
                return null;
            }
        }
#endif

        // 用 Android BitmapFactory 解碼任意格式(含 WebP) → 重新壓成 PNG bytes，避免 JNI 傳 pixel 陣列的麻煩
        static byte[] TryDecodeWebPToPng(byte[] data)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bf = new AndroidJavaClass("android.graphics.BitmapFactory"))
                {
                    var bitmap = bf.CallStatic<AndroidJavaObject>("decodeByteArray", data, 0, data.Length);
                    if (bitmap == null) return null;
                    using (bitmap)
                    using (var fmtClass = new AndroidJavaClass("android.graphics.Bitmap$CompressFormat"))
                    using (var pngFmt = fmtClass.GetStatic<AndroidJavaObject>("PNG"))
                    using (var baos = new AndroidJavaObject("java.io.ByteArrayOutputStream"))
                    {
                        bitmap.Call<bool>("compress", pngFmt, 100, baos);
                        bitmap.Call("recycle");
                        return baos.Call<byte[]>("toByteArray");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ImageLoader] WebP 原生解碼失敗: {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }
    }
}
