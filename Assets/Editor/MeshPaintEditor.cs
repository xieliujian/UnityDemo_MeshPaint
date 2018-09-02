using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

[CustomEditor(typeof(MeshPaint))]
public class MeshPaintEditor : Editor
{
    public enum EBrushSplat
    {
        EBS_Splat1 = 0,
        EBS_Splat2,
        EBS_Splat3,
        EBS_Splat4
    }

    private bool mIsPaint = false;

    private int mSelTexIdx;

    private int mSelBrushIdx;

    private int mBrushSize = 18;

    private float mBrushStronger = 0.5f;

    private Texture[] mTexLayers;

    private List<Texture> mBrushTexs = new List<Texture>();

    #region 内置函数

    public override void OnInspectorGUI()
    {
        if (!CheckHasMask())
            return;

        GUIStyle boolbtn = new GUIStyle(GUI.skin.GetStyle("Button"));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();      // Toggle 居中 
        mIsPaint = GUILayout.Toggle(mIsPaint, EditorGUIUtility.IconContent("EditCollider"), boolbtn, GUILayout.Width(35), GUILayout.Height(35));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        mBrushSize = (int)EditorGUILayout.Slider("Brush Size", mBrushSize, 1.0f, 36.0f);
        mBrushStronger = EditorGUILayout.Slider("Brush Stronger", mBrushStronger, 0.0f, 1.0f);

        InitLayerTex();
        InitBrushTex();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();          // SelectionGrid居中
        GUILayout.BeginHorizontal("box", GUILayout.Width(400));
        mSelTexIdx = GUILayout.SelectionGrid(mSelTexIdx, mTexLayers, 4, "gridlist", GUILayout.Width(400), GUILayout.Height(100));
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();          // SelectionGrid居中
        GUILayout.BeginHorizontal("box", GUILayout.Width(400));
        mSelBrushIdx = GUILayout.SelectionGrid(mSelBrushIdx, mBrushTexs.ToArray(), 10, "gridlist", GUILayout.Width(400), GUILayout.Height(100));
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void OnSceneGUI()
    {
        if (mIsPaint)
        {
            Painter();
        }
    }

    #endregion

    #region 函数

    private void Painter()
    {
        GameObject selectgo = Selection.activeGameObject;
        MeshRenderer meshrender = selectgo.GetComponent<MeshRenderer>();
        if (meshrender == null)
            return;

        Texture2D blendtex = meshrender.sharedMaterial.GetTexture("_Blend") as Texture2D;
        if (blendtex == null)
            return;

        MeshFilter meshfilter = selectgo.GetComponent<MeshFilter>();
        if (meshfilter == null)
            return;

        int brushsize = Mathf.FloorToInt((float)mBrushSize * 2.0f * blendtex.width / meshfilter.sharedMesh.bounds.size.x);

        Event evt = Event.current;
        HandleUtility.AddDefaultControl(0);

        RaycastHit rayhit = new RaycastHit();
        Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
        bool hasray = Physics.Raycast(ray, out rayhit, Mathf.Infinity, LayerMask.GetMask("Ground"));
        if (!hasray)
            return;

        Handles.color = new Color(1.0f, 1.0f, 0.0f, 1.0f);
        Handles.DrawWireDisc(rayhit.point, rayhit.normal, mBrushSize);

        if (evt.type == EventType.mouseDown || evt.type == EventType.mouseDrag)
        {
            Vector2 uv = rayhit.textureCoord;
            int centerx = Mathf.FloorToInt(uv.x * blendtex.width);
            int centery = Mathf.FloorToInt(uv.y * blendtex.height);
            int x = Mathf.Clamp(centerx - brushsize / 2, 0, blendtex.width - 1);
            int y = Mathf.Clamp(centery - brushsize / 2, 0, blendtex.height - 1);
            int width = Mathf.Clamp(centerx + brushsize / 2, 0, blendtex.width) - x;
            int height = Mathf.Clamp(centery + brushsize / 2, 0, blendtex.height) - y;

            Color[] terrainpixs = blendtex.GetPixels(x, y, width, height, 0);

            Texture2D brushtex = mBrushTexs[mSelBrushIdx] as Texture2D;
            float[] brushalphas = new float[brushsize * brushsize];
            for (int j = 0; j < brushsize; j++)
            {
                for (int i = 0; i < brushsize; i++)
                {
                    int index = j * brushsize + i;
                    float uvx = (float)i / brushsize;
                    float uvy = (float)j / brushsize;
                    brushalphas[index] = brushtex.GetPixelBilinear(uvx, uvy).a;
                }
            }

            for (int j = 0; j < brushsize; j++)
            {
                for (int i = 0; i < brushsize; i++)
                {
                    int index = j * brushsize + i;

                    if (index >= terrainpixs.Length)
                        continue;

                    float brushalpha = brushalphas[index];
                    if (mSelTexIdx == (int)EBrushSplat.EBS_Splat1)
                    {
                        terrainpixs[index].r = Mathf.Lerp(terrainpixs[index].r, 1.0f, brushalpha);
                    }
                    else if (mSelTexIdx == (int)EBrushSplat.EBS_Splat2)
                    {
                        terrainpixs[index].g = Mathf.Lerp(terrainpixs[index].g, 1.0f, brushalpha);
                    }
                    else if (mSelTexIdx == (int)EBrushSplat.EBS_Splat3)
                    {
                        terrainpixs[index].b = Mathf.Lerp(terrainpixs[index].b, 1.0f, brushalpha);
                    }
                    else if (mSelTexIdx == (int)EBrushSplat.EBS_Splat4)
                    {
                        terrainpixs[index].a = Mathf.Lerp(terrainpixs[index].a, 1.0f, brushalpha);
                    }
                }
            }

            Undo.RegisterCompleteObjectUndo(blendtex, "MeshPaint");

            blendtex.SetPixels(x, y, width, height, terrainpixs, 0);
            blendtex.Apply();

        }
        else if(evt.type == EventType.mouseUp)
        {
            SaveBlendTex(blendtex);
        }
    }

    private void SaveBlendTex(Texture2D blendtex)
    {
        string path = AssetDatabase.GetAssetPath(blendtex);
        byte[] bytes = blendtex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private void InitBrushTex()
    {
        mBrushTexs.Clear();

        string path = "Assets/Editor/Brushes/";
        var files = Directory.GetFiles(path, "*.png");
        foreach (var file in files)
        {
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(file);
            mBrushTexs.Add(tex);
        }
    }

    private void InitLayerTex()
    {
        Transform select = Selection.activeTransform;
        Material sharedMaterial = select.gameObject.GetComponent<MeshRenderer>().sharedMaterial;
        if (sharedMaterial == null)
            return;

        mTexLayers = new Texture[4];
        mTexLayers[0] = AssetPreview.GetAssetPreview(sharedMaterial.GetTexture("_Splat1"));
        mTexLayers[1] = AssetPreview.GetAssetPreview(sharedMaterial.GetTexture("_Splat2"));
        mTexLayers[2] = AssetPreview.GetAssetPreview(sharedMaterial.GetTexture("_Splat3"));
        mTexLayers[3] = AssetPreview.GetAssetPreview(sharedMaterial.GetTexture("_Splat4"));
    }

    private bool CheckHasMask()
    {
        Transform select = Selection.activeTransform;
        if (select.gameObject == null)
            return false;

        MeshRenderer meshrender = select.gameObject.GetComponent<MeshRenderer>();
        if (meshrender == null)
            return false;

        Texture blendtex = meshrender.sharedMaterial.GetTexture("_Blend");
        if (blendtex == null)
        {
            EditorGUILayout.HelpBox("当前模型材质球中未找到Blend贴图，绘制功能不可用！", MessageType.Error);
            if (GUILayout.Button("创建Blend贴图"))
            {
                CreateBlendTex(meshrender.sharedMaterial);
            }

            return false;
        }

        return true;
    }

    private void CreateBlendTex(Material sharedMaterial)
    {
        Texture2D blendtex = new Texture2D(512, 512, TextureFormat.ARGB32, false);
        Color[] colors = new Color[512 * 512];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }
        blendtex.SetPixels(colors);     

        string path = "Assets/Textures/Blend.png";
        byte[] bytes = blendtex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter teximp = AssetImporter.GetAtPath(path) as TextureImporter;
        teximp.textureCompression = TextureImporterCompression.Uncompressed;
        teximp.mipmapEnabled = false;
        teximp.wrapMode = TextureWrapMode.Clamp;
        teximp.isReadable = true;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        Texture2D texres = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        sharedMaterial.SetTexture("_Blend", texres);
    }

    #endregion
}
