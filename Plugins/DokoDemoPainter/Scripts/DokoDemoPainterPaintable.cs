// Copyright (c) 2018 Emiliana (twitter.com/Emiliana_vt)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class DokoDemoPainterPaintable : MonoBehaviour {
    [Header("Pen settings")]
    [Tooltip("The pixel size of a pen is multiplied by this value. This can be used to adjust line thickness on surfaces with a different scale.")]
    public float radiusFactor = 1.0f;
    [Tooltip("Pen opacity is multiplied by this value. Values other than 1.0 might slightly reduce performance.")]
    public float penOpacityFactor = 1.0f;
    [Tooltip("The pixel size of a pen in eraser mode is multiplied by this value. This can be used to adjust line thickness on surfaces with a different scale.")]
    public float eraserRadiusFactor = 1.0f;
    [Tooltip("Pen opacity in eraser mode is multiplied by this value. Values other than 1.0 might slightly reduce performance.")]
    public float eraserOpacityFactor = 1.0f;
    [Tooltip("When this flag is enabled, the target texture's transparency value will be preserved. Otherwise the pen color's alpha value will be painted.")]
    public bool preserveAlphaPen = false;
    [Header("Stamp settings")]
    [Tooltip("Each dimension of the stamp's size will be multiplied by the corresponding scale factor.")]
    public Vector2 stampScaleFactor = new Vector2(1.0f, 1.0f);
    [Tooltip("When this flag is enabled, the target texture's transparency value will be preserved. Otherwise the stamp color's alpha value will be painted. Alpha values from tints will still take precedence.")]
    public bool preserveAlphaStamp = true;
    [Header("Paint fading")]
    [Tooltip("This value sets much should paint should fade towards the original color over a given time. A value of 0 disables fading. A value of 1 would completely erase everything that was painted after fadeTimeSeconds.")]
    public float fadeFactor = 0.0f;
    [Tooltip("This value sets how much time needs to pass until colors fade by fadeFactor.")]
    public float fadeTimeSeconds = 60f * 60f * 24f;
    [Tooltip("This value sets how often fading should be applied. When fading small amounts over long times, making this value higher can prevent fades from being too small to actually change color values. If this value is below fadeTimeSeconds, the fading process will be applied in small steps. If this value is above fadeTimeSeconds, the texture will get faded less often but with values higher than fadeFactor.")]
    public float fadeIntervalSeconds = 60f * 30f;
    [Header("Texture saving")]
    [Tooltip("This field allows specifying a unique name of this object that will be included in saved texture filenames. This is useful when multiple DokoDemoPainterPaintable objects with the same GameObject name exist, as they would overwrite each others' textures.")]
    public string uniqueName = "";
    [Tooltip("This flag determines whether to load previously saved textures on initialization. The last fade time will also be loaded.")]
    public bool persistent = false;
    [Tooltip("If this flag is enabled, filenames will include a timestamp, which will lead to old copies being kept. When this flag is enabled, no timestamp is included and textures will be overwritten.")]
    public bool keepOld = true;
    [Tooltip("When this flag is enabled, Application.persistentDataPath will be prepended to the path set in savePath.")]
    public bool prependAppDir = true;
    [Tooltip("This path sets the directory to which textures should be written. If it is empty, no textures will be saved. If necessary, the directory will be created.")]
    public string savePath = "";
    
    [Header("Advanced settings")]
    [Tooltip("For every DokoDemoPainterPaintable object instance and every material on it, multiple textures need to be allocated. This can use a lot of VRAM. Enabling this flag will defer texture allocation for these materials on this object until this object comes within painting range. Texture allocation might lead to slight lag at that point in time.")]
    public bool deferTextureAllocation = false;
    [Tooltip("This value sets the maximum distance in pixels between the start and end points of line segments. If you encounter strange lines appearing on your texture while drawing quickly, lower this value. If you notice gaps in your line while drawing quickly, increase this value. If both happens, try to rearrange your UV map in such a way that line segments between connected parts of the texture will not pass through other parts of it.")]
    public float maxDistance = 48f;
    [Tooltip("When this list has a size of 0, all materials with a set main texture will be paintable. Otherwise only materials in slots corresponding to those given in this list will be paintable. This list is only processed at initialization. Use this to: 1) Protect certain materials from being painted or stamped. 2) Reduce the number of paintable materials to increase performance. 3) Set different settings for different materials by having multiple DokoDemoPainterPaintable components with non-overlapping whitelists.")]
    public List<int> materialIndexWhitelist;

    // This feature doesn't really work
    private bool historyDecay = false;
    private float historyDecayTime = 0.0f;

    private int id = -1;

    private Renderer paintableRenderer;
    private Material[] materials = null;
    private Shader[] shaders = null;
    private TextureProcessor[] tps = null;

    private bool uvMode = false;
    private bool wasUV = false;
    private bool painted = false;
    private bool fadedNow = false;
    
    private int oldLayer = 0;
    private int previouslyPainted = 0;
    private double lastFadeTime = 0;

    // Book keeping
    private static int layer = -1;
    private static Material texProcMat = null;
    private static bool ensureTextureProcessor() {
        if (layer == -1) {
            int layer = LayerMask.NameToLayer("DokoDemoPainter");
            if (layer == -1) {
                Debug.LogError("Attempting to initialize texture painting with missing Unity layer. Please add a layer named: DokoDemoPainter");
                return false;
            }
        }
        if (texProcMat == null) {
            texProcMat = new Material(Shader.Find("DokoDemoPainter/Render"));
        }
        return true;
    }
    
    // IDs start at 2 to avoid accidental matches
    private static int lastTPId = 2;
    private static Dictionary<int, TextureProcessor> tpIDMap = null;
    private static int registerTP (TextureProcessor tp) {
        if (tpIDMap == null) {
            tpIDMap = new Dictionary<int, TextureProcessor>();
        }
        int id = lastTPId++;
        tpIDMap.Add(id, tp);
        return id;
    }
    private static void deregisterTP (int id) {
        if (id == -1) {
            return;
        }
        tpIDMap.Remove(id);
    }
    
    // The same for Paintable instances
    private static int lastId = 1;
    private static Dictionary<int, DokoDemoPainterPaintable> idMap = null;
    private static int registerPaintable (DokoDemoPainterPaintable p) {
        if (idMap == null) {
            Resources.UnloadUnusedAssets();
            idMap = new Dictionary<int, DokoDemoPainterPaintable>();
        }
        int id = lastId++;
        idMap.Add(id, p);
        return id;
    }
    private static void deregisterPaintable (int id) {
        if (id == -1) {
            return;
        }
        idMap.Remove(id);
    }
    public static bool setGlobalUVMode(bool mode, Plane[] planes) {
        if (idMap == null) {
            return false;
        }
        bool res = false;
        foreach (var p in idMap.Values) {
            if (p.enabled && p.gameObject.activeInHierarchy) {
                res = p.setUVMode(mode, planes) || res;
            }
        }
        return res;
    }
    public static void globalPaintAt(int penId, RenderTexture rtPos, Color color, bool smoothTip, float smoothTipExp, float radius, float opacity, bool erase) {
        if (idMap == null) {
            return;
        }
        setGlobalUVMode(false, null);
        foreach (var p in idMap.Values) {
            if (p.enabled && p.gameObject.activeInHierarchy) {
                p.paintAt(penId, rtPos, color, smoothTip, smoothTipExp, radius, opacity, erase);
            }
        }
    }
    public static void globalStampAt(RenderTexture rtPos, Texture2D stampTexture, Vector2 stampPixelSize, float stampAngle, float stampOpacity, bool enableTint, Color tintColor, Vector4 tintStrength) {
        if (idMap == null) {
            return;
        }
        setGlobalUVMode(false, null);
        foreach (var p in idMap.Values) {
            if (p.enabled && p.gameObject.activeInHierarchy) {
                p.stampAt(rtPos, stampTexture, stampPixelSize, stampAngle, stampOpacity, enableTint, tintColor, tintStrength);
            }
        }
    }

    // Main part
    private class TextureProcessor {
        public int renderQueue;
        private int id = -1;
        private Texture2D tex = null;
        private Material targetMaterial = null;
        private RenderTexture rtCurrent = null;
        private Texture rtOriginal = null;
        private Dictionary<int, PenData> pens = null;
        
        private class PenData {
            public RenderTexture rtOldPos = null;
            public RenderTexture rtHist = null;
            public int lastDrawn = 0;
            public float lastTime = 0.0f;
            private float lastOpacity = -1.0f;
            private float lastStrength = -1.0f;
            private Color lastColor = Color.black;
            private bool lastSmoothTip = false;
            private float lastSmoothTipExp = -1.0f;
            
            public PenData() {
                if (rtOldPos == null) {
                    rtOldPos = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                    if (rtOldPos.useMipMap) {
                        rtOldPos.Release();
                        rtOldPos.useMipMap = false;
                        rtOldPos.Create();
                    }
                }       
            }
            
            public bool checkPrevPainted(float strength, float opacity, Color color, bool prevPainted, bool smoothTip, float smoothTipExp) {
                if (strength != lastStrength || opacity != lastOpacity || color != lastColor || !prevPainted || smoothTip != lastSmoothTip || smoothTipExp != lastSmoothTipExp) {
                    lastStrength = strength;
                    lastOpacity = opacity;
                    lastColor = color;
                    lastSmoothTip = smoothTip;
                    lastSmoothTipExp = smoothTipExp;
                    return false;
                }
                return true;
            }
        }
        
        private class DrawCommand {
            public int penId = -1;
            public RenderTexture rtPos = null;
            public Color color = Color.black;
            public float maxDist = 0.0f;
            public bool preserveAlpha = true;
            public bool smoothTip = false;
            public float smoothTipExp = 1.0f;
            public float width = 0.0f;
            public float strength = 0.0f;
            public float opacity = 0.0f;
            public float fade = 0.0f;
            public bool prevPainted = false;
            public bool historyDecay = false;
            public float historyDecayTime = 0.0f;
            public Texture2D stampTexture = null;
            public Vector2 stampPixelSize = new Vector2(128.0f, 128.0f);
            public float stampAngle = 0.0f;
            public Vector2 stampScaleFactor = new Vector2(1.0f, 1.0f);
            public float stampOpacity = 0.0f;
            public bool enableTint = false;
            public Color tintColor = Color.black;
            public Vector4 tintStrength = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        }

        public TextureProcessor(Material mat, Texture2D persistingTexture, bool defer) {
            id = registerTP(this);
            targetMaterial = mat;
            renderQueue = targetMaterial.renderQueue;
            tex = persistingTexture;
            if (tex == null) {
                if (mat.mainTexture != null && mat.mainTexture.GetType() == typeof(Texture2D)) {
                    tex = (Texture2D)mat.mainTexture;
                } else {
                    Debug.LogError("Received material without texture.");
                    tex = new Texture2D(128, 128);
                }
            }
            rtOriginal = (Texture2D)mat.mainTexture;
            if (persistingTexture != null) {
                mat.mainTexture = persistingTexture;
            }
            
            if (!defer)
                ensureTextures(-1);
            
            pens = new Dictionary<int, PenData>();
        }
        
        ~TextureProcessor() {
            deregisterTP(id);
        }

        bool ensureTextures(int penId) {
            if (penId > -1) {
                PenData pen = null;
                if (!pens.TryGetValue(penId, out pen)) {
                    pen = new PenData();
                    pens.Add(penId, pen);
                }
            }
            if (rtCurrent == null) {
                rtCurrent = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                if (rtCurrent.useMipMap) {
                    rtCurrent.Release();
                    rtCurrent.useMipMap = false;
                    rtCurrent.Create();
                }
                rtCurrent.wrapMode = tex.wrapMode;
                Graphics.Blit(tex, rtCurrent);
                targetMaterial.mainTexture = rtCurrent;
                tex = null;
            }
            bool ret = ensureTextureProcessor();
            return ret;
        }
        
        private void runShader(DrawCommand dc) {//int penId, RenderTexture rtPos, Color color, float maxDist, bool preserveAlpha, bool smoothTip, float smoothTipExp, float width, float strength, float opacity, float fade, bool prevPainted, bool historyDecay, float historyDecayTime) {
            if (!ensureTextures(dc.penId))
                return;
            
            RenderTexture rtHist = null;
            RenderTexture rtOldPos = null;
            PenData pen = null;
            if (pens.TryGetValue(dc.penId, out pen)) {
                
                rtHist = pen.rtHist;
                rtOldPos = pen.rtOldPos;
                if (dc.rtPos != null && (dc.strength > 0.0f || dc.opacity > 0.0f)) {
                    pen.lastDrawn = 3;
                }
                dc.prevPainted = pen.checkPrevPainted(dc.strength, dc.opacity, dc.color, dc.prevPainted, dc.smoothTip, dc.smoothTipExp);
            }
            
            texProcMat.SetTexture("_MainTex", rtCurrent);
            texProcMat.SetTexture("_OrigTex", rtOriginal);
            if (dc.prevPainted) {
                texProcMat.SetTexture("_HistTex", rtHist);
                texProcMat.SetTexture("_CoordTexA", rtOldPos);
            } else {
                if (pen != null && pen.rtHist != null) {
                    discardRT(pen.rtHist);
                    pen.rtHist = null;
                    rtHist = null;
                }
                texProcMat.SetTexture("_HistTex", null);
                texProcMat.SetTexture("_CoordTexA", dc.rtPos);
            }
            if (dc.rtPos == null) {
                texProcMat.SetTexture("_CoordTexA", null);
            }
            texProcMat.SetTexture("_CoordTexB", dc.rtPos);
            texProcMat.SetFloat("_Fade", dc.fade);
            texProcMat.SetFloat("_SurfaceID", (float)id);
            texProcMat.SetFloat("_MaxDist", dc.maxDist / (float)rtCurrent.width);
            texProcMat.SetFloat("_PreserveAlpha", dc.preserveAlpha ? 1.0f : 0.0f);
            texProcMat.SetColor("_PenColor", dc.color);
            texProcMat.SetFloat("_PenThickness", dc.width / (float)rtCurrent.width);
            texProcMat.SetFloat("_SmoothTip", dc.smoothTip ? 1.0f : 0.0f);
            texProcMat.SetFloat("_SmoothExp", dc.smoothTipExp);
            texProcMat.SetFloat("_Opacity", dc.opacity);
            texProcMat.SetFloat("_EraserStrength", dc.strength);
            texProcMat.SetTexture("_StampTex", dc.stampTexture);
            Vector4 stampSize = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            stampSize.x = (dc.stampPixelSize.x / rtCurrent.width) * dc.stampScaleFactor.x;
            stampSize.y = (dc.stampPixelSize.y / rtCurrent.width) * dc.stampScaleFactor.y;
            texProcMat.SetVector("_StampSize", stampSize);
            texProcMat.SetFloat("_StampAngle", dc.stampAngle);
            texProcMat.SetFloat("_StampAlpha", dc.stampOpacity);
            texProcMat.SetColor("_StampTint", dc.tintColor);
            texProcMat.SetColor("_StampTintFactor", dc.enableTint ? dc.tintStrength : new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            texProcMat.SetFloat("_HistoryWrite", 0.0f);
            texProcMat.SetFloat("_HistoryTime", dc.historyDecay ? dc.historyDecayTime : 0.000001f);
            if (pen != null) {
                float decay = dc.historyDecay ? Time.time - pen.lastTime : 0.0f;
                texProcMat.SetFloat("_HistoryDecay", decay);
            } else {
                texProcMat.SetFloat("_HistoryDecay", 0.0f);
            }

            RenderTexture rtNew = RenderTexture.GetTemporary(rtCurrent.width, rtCurrent.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            if (rtNew.useMipMap) {
                rtNew.Release();
                rtNew.useMipMap = false;
                rtNew.Create();
            }
            rtNew.wrapMode = rtCurrent.wrapMode;
            if (rtCurrent == targetMaterial.mainTexture) {
                Graphics.Blit(rtCurrent, rtNew, texProcMat);
            } else {
                Graphics.Blit(targetMaterial.mainTexture, rtNew, texProcMat);
            }
            targetMaterial.mainTexture = rtNew;
            discardRT(rtCurrent);
            rtCurrent = rtNew;

            if (pen != null) {
                if ((dc.opacity > 0.0f && dc.opacity < 1.0f) || (dc.strength > 0.0f && dc.strength < 1.0f) || dc.smoothTip) {
                    rtNew = RenderTexture.GetTemporary(rtCurrent.width, rtCurrent.height, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                    if (rtNew.useMipMap) {
                        rtNew.Release();
                        rtNew.useMipMap = false;
                        rtNew.Create();
                    }
                    rtNew.wrapMode = rtCurrent.wrapMode;
                    texProcMat.SetTexture("_MainTex", null);
                    texProcMat.SetTexture("_OrigTex", null);
                    texProcMat.SetTexture("_HistTex", rtHist);
                    texProcMat.SetFloat("_Fade", 0.0f);
                    texProcMat.SetColor("_PenColor", Color.white);
                    texProcMat.SetVector("_StampSize", new Vector2(0.0f, 0.0f));
                    texProcMat.SetFloat("_Opacity", 1.0f);
                    texProcMat.SetFloat("_EraserStrength", 0.0f);
                    pen.lastTime = Time.time;
                    texProcMat.SetFloat("_HistoryWrite", 1.0f);
                    Graphics.Blit(rtCurrent, rtNew, texProcMat);
                    discardRT(pen.rtHist);
                    pen.rtHist = rtNew;
                } else {
                    discardRT(pen.rtHist);
                    pen.rtHist = null;
                }
                Graphics.Blit(dc.rtPos, pen.rtOldPos);
            }
            targetMaterial.renderQueue = renderQueue;
        }

        public void draw(int penId, RenderTexture rtPos, Color color, float maxDist, bool preserveAlpha, bool smoothTip, float smoothTipExp, float widthPx, float opacity, float fadeAmount, bool prevPainted, bool historyDecay, float historyDecayTime) {
            DrawCommand dc = new DrawCommand();
            dc.penId = penId;
            dc.rtPos = rtPos;
            dc.color = color;
            dc. maxDist = maxDist;
            dc.preserveAlpha = preserveAlpha;
            dc.smoothTip = smoothTip;
            dc.smoothTipExp = smoothTipExp;
            dc.width = widthPx;
            dc.opacity = opacity;
            dc.fade = fadeAmount;
            dc.prevPainted = prevPainted;
            dc.historyDecay = historyDecay;
            dc.historyDecayTime = historyDecayTime;
            runShader(dc);
        }
        
        public void fade(float fadeAmount) {
            DrawCommand dc = new DrawCommand();
            dc.fade = fadeAmount;
            runShader(dc);
        }

        public void erase(int penId, RenderTexture rtPos, float maxDist, bool smoothTip, float smoothTipExp, float widthPx, float strength, float fadeAmount, bool prevPainted, bool historyDecay, float historyDecayTime) {
            DrawCommand dc = new DrawCommand();
            dc.penId = penId;
            dc.rtPos = rtPos;
            dc. maxDist = maxDist;
            dc.preserveAlpha = false;
            dc.smoothTip = smoothTip;
            dc.smoothTipExp = smoothTipExp;
            dc.width = widthPx;
            dc.strength = strength;
            dc.fade = fadeAmount;
            dc.prevPainted = prevPainted;
            dc.historyDecay = historyDecay;
            dc.historyDecayTime = historyDecayTime;
            runShader(dc);
        }
        
        public void stampAt(RenderTexture rtPos, bool preserveAlphaStamp, Vector2 stampScaleFactor, Texture2D stampTexture, Vector2 stampPixelSize, float stampAngle, float stampOpacity, bool enableTint, Color tintColor, Vector4 tintStrength, float fadeAmount) {
            DrawCommand dc = new DrawCommand();
            dc.rtPos = rtPos;
            dc.preserveAlpha = preserveAlphaStamp;
            dc.stampTexture = stampTexture;
            dc.stampPixelSize = stampPixelSize;
            dc.stampScaleFactor = stampScaleFactor;
            dc.stampAngle = stampAngle;
            dc.stampOpacity = stampOpacity;
            dc.enableTint = enableTint;
            dc.tintColor = tintColor;
            dc.tintStrength = tintStrength;
            dc.fade = fadeAmount;
            runShader(dc);
        }

        public void discardRT(RenderTexture rt) {
            if (rt != null) {
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                RenderTexture.active.DiscardContents();
                RenderTexture.ReleaseTemporary(rt);
                RenderTexture.active = prev;
            }
        }
        
        public void maintainPens() {
            List<int> removalQueue = new List<int>();
            foreach (int penId in pens.Keys) {
                PenData pen = pens[penId];
                pen.lastDrawn--;
                if (pen.lastDrawn <= 0) {
                    discardRT(pen.rtHist);
                    discardRT(pen.rtOldPos);
                    removalQueue.Add(penId);
                }
            }
            foreach (int penId in removalQueue) {
                pens.Remove(penId);
            }
        }

        private byte[] texToPNG(Texture tex) {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, RenderTexture.active);
            Texture2D readTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            readTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readTex.Apply();
            RenderTexture.ReleaseTemporary(RenderTexture.active);
            RenderTexture.active = prev;
            return readTex.EncodeToPNG();
        }
        
        public byte[] toPNG() {
            if (!ensureTextures(-1))
                return null;
            if (tex)
                return tex.EncodeToPNG();
            return texToPNG(rtCurrent);
        }
        
        public byte[] origToPNG() {
            if (!ensureTextures(-1))
                return null;
            if (tex)
                return tex.EncodeToPNG();
            return texToPNG(rtOriginal);
        }
        
        public int getID() {
            return id;
        }
    }

    double getTimestamp() {
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        return (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
    }

    void Start () {
        int layer = LayerMask.NameToLayer("DokoDemoPainter");
        if (layer == -1) {
            Debug.LogError("Attempting to initialize texture painting with missing Unity layer. Please add a layer named: DokoDemoPainter");
            return;
        }
        
        if (id == -1) {
            id = registerPaintable(this);
        }

        // Ensure that bounds of paintable objects are always correct (might be slow)
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        if (smr != null) {
            smr.updateWhenOffscreen = true;
        }
        
        paintableRenderer = gameObject.GetComponent<Renderer>();
        materials = paintableRenderer.materials;
        shaders = new Shader[materials.Length];
        tps = new TextureProcessor[materials.Length];
        
        HashSet<int> matWL = null;
        if (materialIndexWhitelist != null && materialIndexWhitelist.Count > 0) {
            matWL = new HashSet<int>(materialIndexWhitelist);
        }

        bool gotLastFade = false;
        for (int i = 0; i < materials.Length ; i++) {
            shaders[i] = materials[i].shader;
            tps[i] = null;
            if (matWL != null && !matWL.Contains(i)) {
                continue;
            }

            if (materials[i].mainTexture != null && (materials[i].mainTexture.GetType() == typeof(Texture2D) || materials[i].mainTexture.GetType() == typeof(RenderTexture))) {
                string savePathFull = (prependAppDir ? Application.persistentDataPath + "/" : "") + savePath;
                string objName = (uniqueName == "") ? "" : uniqueName + " ";
                string filename = savePathFull + "/Latest " + objName + gameObject.name + " " + i + " " + materials[i].name + ".txt";
                string timeFilename = savePathFull + "/Latest time " + objName + gameObject.name + " " + i + " " + materials[i].name + ".txt";
                if (persistent && File.Exists(filename) && File.Exists(timeFilename)) {
                    filename = savePathFull + "/" + File.ReadAllText(filename);
                    lastFadeTime = double.Parse(File.ReadAllText(timeFilename));
                    gotLastFade = true;
                    byte[] fileData = File.ReadAllBytes(filename);
                    Texture2D loadTex = new Texture2D(2, 2);
                    loadTex.LoadImage(fileData);
                    tps[i] = new TextureProcessor(materials[i], loadTex, deferTextureAllocation);
                } else {
                    tps[i] = new TextureProcessor(materials[i], null, deferTextureAllocation);
                }
            }
        }
        paintableRenderer.materials = materials;

        if (!gotLastFade) {
            lastFadeTime = getTimestamp();
        }
    }
    
    public bool setUVMode(bool mode, Plane[] planes) {
        if (uvMode && !mode) {
            uvMode = false;
            gameObject.layer = oldLayer;
            for (int i = 0; i < materials.Length; i++) {
                if (tps[i] != null) {
                    materials[i].shader = shaders[i];
                    materials[i].renderQueue = tps[i].renderQueue;
                }
            }
        } else if (mode && !uvMode) {
            if (!GeometryUtility.TestPlanesAABB(planes, paintableRenderer.bounds)) {
                return false;
            }
            wasUV = true;
            uvMode = true;
            oldLayer = gameObject.layer;
            gameObject.layer = LayerMask.NameToLayer("DokoDemoPainter");
            for (int i = 0; i < materials.Length; i++) {
                if (tps[i] != null) {
                    materials[i].shader = Shader.Find("DokoDemoPainter/Detect");
                    materials[i].SetFloat("_SurfaceID", tps[i].getID());
                }
            }
            return true;
        }
        return false;
    }

    float getFade() {
        double time = getTimestamp();
        if (fadeFactor == 0.0f || lastFadeTime + fadeIntervalSeconds > time) {
            return 0.0f;
        }
        float currentFactor = 1 - Mathf.Pow(1.0f - fadeFactor, (float)(time - lastFadeTime) / fadeTimeSeconds);
        if (currentFactor > 0.0f) {
            lastFadeTime = time;
        }
        return currentFactor;
    }

    public void paintAt(int penId, RenderTexture rtPos, Color color, bool smoothTip, float smoothTipExp, float radius, float opacity, bool erase) {
        if (!wasUV) {
            return;
        }
        painted = true;
        float currentFactor = getFade();
        if (currentFactor > 0.0f && !fadedNow) {
            lastFadeTime = getTimestamp();
            fadedNow = true;
        }
        foreach (var tp in tps) {
            if (tp != null) {
                if (!erase) {
                    tp.draw(penId, rtPos, color, maxDistance, preserveAlphaPen, smoothTip, smoothTipExp, radiusFactor * radius, penOpacityFactor * opacity, currentFactor, previouslyPainted > 0, historyDecay, historyDecayTime);
                } else {
                    tp.erase(penId, rtPos, maxDistance, smoothTip, smoothTipExp, eraserRadiusFactor * radius, eraserOpacityFactor * opacity, currentFactor, previouslyPainted > 0, historyDecay, historyDecayTime);
                }
            }
        }
        wasUV = false;
    }
    
    public void stampAt(RenderTexture rtPos, Texture2D stampTexture, Vector2 stampPixelSize, float stampAngle, float stampOpacity, bool enableTint, Color tintColor, Vector4 tintStrength) {
        if (!wasUV) {
            return;
        }
        painted = true;
        float currentFactor = getFade();
        if (currentFactor > 0.0f && !fadedNow) {
            lastFadeTime = getTimestamp();
            fadedNow = true;
        }
        foreach (var tp in tps) {
            if (tp != null) {
                tp.stampAt(rtPos, preserveAlphaStamp, stampScaleFactor, stampTexture, stampPixelSize, stampAngle, stampOpacity, enableTint, tintColor, tintStrength, currentFactor);
            }
        }
        wasUV = false;
    }

    void LateUpdate() {
        wasUV = false;
        float currentFactor = getFade();
        if (currentFactor > 0.0f && !fadedNow) {
            foreach (var tp in tps) {
                if (tp != null) {
                    tp.fade(currentFactor);
                }
            }
            lastFadeTime = getTimestamp();
        }
        fadedNow = false;
        if (painted) {
            previouslyPainted = 3;
            painted = false;
        }
        if (previouslyPainted > 0) {
            previouslyPainted--;
            foreach (var tp in tps) {
                if (tp != null) {
                    tp.maintainPens();
                }
            }
        }
    }
    
    public void OnDestroy() {
        if (savePath == "") {
            if (idMap != null) {
                idMap.Remove(id);
            }
            return;
        }
        System.DateTime dt = System.DateTime.Now;
        for (int i = 0; i < materials.Length; i++) {
            if (tps[i] != null) {
                string savePathFull = (prependAppDir ? Application.persistentDataPath + "/" : "") + savePath;
                string objName = (uniqueName == "") ? "" : uniqueName + " ";
                string imageFilename = objName + gameObject.name + " " + materials[i].name + " " + i + (!keepOld ? "" : " " + dt.ToString("yyyy-MM-dd-HH-mm-ss") + " (" + Time.frameCount + ")") + ".png";
                string filename = savePathFull + "/" + imageFilename;
                byte[] bytes = tps[i].toPNG();
                byte[] bytesOrig = tps[i].origToPNG();
                if (!bytes.SequenceEqual(bytesOrig)) {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(filename);
                    fileInfo.Directory.Create();
                    File.WriteAllBytes(filename, bytes);
                    if (persistent) {
                        File.WriteAllText(savePathFull + "/Latest " + objName + gameObject.name + " " + i + " " + materials[i].name + ".txt", imageFilename);
                        File.WriteAllText(savePathFull + "/Latest time " + objName + gameObject.name + " " + i + " " + materials[i].name + ".txt", lastFadeTime.ToString());
                    }
                }
            }
        }   
        idMap.Remove(id);
    }
    
    public void OnApplicationQuit() {
        OnDestroy();
    }
}
