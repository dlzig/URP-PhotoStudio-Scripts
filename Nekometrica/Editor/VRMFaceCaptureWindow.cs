using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using VRM; // UniVRM 0.x

public class VRMFaceCaptureWindow : EditorWindow
{
    // ---------------------------------------------------------
    // 設定変数
    // ---------------------------------------------------------
    private GameObject targetModel;
    private string saveFolderPath = "";
    
    private int imageWidth = 1024;
    private int imageHeight = 1024;

    private float cameraDistance = 0.6f;
    private float heightOffset = 0.12f;
    private float fieldOfView = 25f;

    // 内部データ
    private VRMBlendShapeProxy proxy;
    private Animator animator;
    private Dictionary<BlendShapeKey, bool> selectionDict = new Dictionary<BlendShapeKey, bool>();
    private Vector2 scrollPos;
    private Texture2D previewTexture;

    // キャプチャ進行用
    private List<BlendShapeKey> captureQueue;
    private int currentCaptureIndex;
    private Camera tempCamera;
    private RenderTexture tempRT;
    private Texture2D tempTexture;
    private string currentExportPath;
    
    // コンポーネント退避用
    private Dictionary<Behaviour, bool> componentStateBackup;

    // ---------------------------------------------------------
    // ウィンドウ表示
    // ---------------------------------------------------------
    [MenuItem("Tools/VRM Face Capturer")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRMFaceCaptureWindow>("VRMFaceCaptureWindow");
        window.minSize = new Vector2(400, 750);
    }

    private void OnEnable()
    {
        if (targetModel != null) RefreshBlendShapes();
    }

    private void OnDisable()
    {
        if (previewTexture != null) DestroyImmediate(previewTexture);
        CleanupTempObjects();
    }

    private void OnGUI()
    {
        GUILayout.Label("VRM表情キャプチャ", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("プレイモードにしてから実行してください", MessageType.Info);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("1. モデル設定", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            targetModel = (GameObject)EditorGUILayout.ObjectField("Target Model", targetModel, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshBlendShapes();
            }

            if (targetModel == null)
            {
                EditorGUILayout.HelpBox("VRMモデルをセットしてください", MessageType.Warning);
                return;
            }
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("2. カメラ・画像設定", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            imageWidth = EditorGUILayout.IntField("Width", imageWidth);
            imageHeight = EditorGUILayout.IntField("Height", imageHeight);
            GUILayout.EndHorizontal();

            cameraDistance = EditorGUILayout.Slider("距離", cameraDistance, 0.1f, 2.0f);
            heightOffset = EditorGUILayout.Slider("高さ", heightOffset, -0.5f, 0.5f);
            fieldOfView = EditorGUILayout.Slider("画角", fieldOfView, 10f, 90f);

            EditorGUILayout.Space();

            if (GUILayout.Button("プレビュー更新")) UpdatePreview();

            if (previewTexture != null)
            {
                EditorGUILayout.Space();
                float aspect = (float)imageWidth / imageHeight;
                float previewW = position.width - 40;
                float previewH = previewW / aspect;
                
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(previewTexture, GUILayout.Width(previewW), GUILayout.Height(previewH));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("3. 出力する表情", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全選択")) SetAllToggles(true);
            if (GUILayout.Button("全解除")) SetAllToggles(false);
            GUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            if (selectionDict != null && selectionDict.Count > 0)
            {
                var keys = selectionDict.Keys.ToList();
                foreach (var key in keys)
                {
                    selectionDict[key] = EditorGUILayout.ToggleLeft(key.Name, selectionDict[key]);
                }
            }
            else
            {
                GUILayout.Label("表情データがありません");
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("4. 実行", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.TextField("保存先", saveFolderPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("保存先", saveFolderPath, "");
                if (!string.IsNullOrEmpty(path)) saveFolderPath = path;
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("キャプチャ開始", GUILayout.Height(40)))
            {
                StartCaptureSequence();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    // ---------------------------------------------------------
    // 設定・プレビュー関連
    // ---------------------------------------------------------

    private void RefreshBlendShapes()
    {
        if (targetModel == null) return;
        proxy = targetModel.GetComponent<VRMBlendShapeProxy>();
        animator = targetModel.GetComponent<Animator>();

        if (proxy != null && proxy.BlendShapeAvatar != null)
        {
            selectionDict.Clear();
            foreach (var clip in proxy.BlendShapeAvatar.Clips)
            {
                if (!selectionDict.ContainsKey(clip.Key)) selectionDict.Add(clip.Key, true);
            }
        }
        if (previewTexture != null) DestroyImmediate(previewTexture);
    }

    private void SetAllToggles(bool value)
    {
        var keys = selectionDict.Keys.ToList();
        foreach (var key in keys) selectionDict[key] = value;
    }

    private Camera CreateTempCamera(GameObject container, out RenderTexture rt)
    {
        Camera cam = container.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.fieldOfView = fieldOfView;
        cam.nearClipPlane = 0.01f;

        Transform headBone = (animator != null) ? animator.GetBoneTransform(HumanBodyBones.Head) : targetModel.transform;
        if (headBone == null) headBone = targetModel.transform;

        Vector3 forward = targetModel.transform.forward;
        Vector3 faceCenter = headBone.position + new Vector3(0, heightOffset, 0);

        cam.transform.position = faceCenter + (forward * cameraDistance);
        cam.transform.LookAt(faceCenter);
        cam.transform.rotation = Quaternion.LookRotation(-forward);
        cam.transform.position = faceCenter + (forward * cameraDistance);
        cam.transform.LookAt(faceCenter);

        rt = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        return cam;
    }

    private void UpdatePreview()
    {
        if (targetModel == null) return;
        GameObject tempObj = new GameObject("Preview_Cam");
        RenderTexture rt;
        Camera cam = CreateTempCamera(tempObj, out rt);
        cam.Render();

        if (previewTexture != null) DestroyImmediate(previewTexture);
        previewTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        previewTexture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        previewTexture.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(tempObj);
    }

    // ---------------------------------------------------------
    // キャプチャ処理 
    // ---------------------------------------------------------

    private void StartCaptureSequence()
    {
        if (targetModel == null || proxy == null)
        {
            EditorUtility.DisplayDialog("Error", "モデル設定を確認してください。", "OK");
            return;
        }

        captureQueue = selectionDict.Where(x => x.Value).Select(x => x.Key).ToList();
        if (captureQueue.Count == 0) return;

        currentExportPath = string.IsNullOrEmpty(saveFolderPath) 
            ? Path.Combine(Application.dataPath, "../Captured_Expressions") 
            : saveFolderPath;
        if (!Directory.Exists(currentExportPath)) Directory.CreateDirectory(currentExportPath);

        // コンポーネント一時停止
        var anims = targetModel.GetComponentsInChildren<Animator>();
        var lookAts = targetModel.GetComponentsInChildren<VRMLookAtHead>();
        
        componentStateBackup = new Dictionary<Behaviour, bool>();
        foreach (var a in anims) { componentStateBackup[a] = a.enabled; a.enabled = false; }
        foreach (var l in lookAts) { componentStateBackup[l] = l.enabled; l.enabled = false; }

        // 撮影用オブジェクト準備
        GameObject tempObj = new GameObject("Capture_Cam_Sequence");
        tempCamera = CreateTempCamera(tempObj, out tempRT);
        tempTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);

        // 表情リセット
        proxy.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Neutral), 0);
        foreach (var clip in proxy.BlendShapeAvatar.Clips)
        {
            proxy.ImmediatelySetValue(clip.Key, 0.0f);
        }

        currentCaptureIndex = 0;
        ProcessNextCapture();
    }

    private void ProcessNextCapture()
    {
        if (currentCaptureIndex >= captureQueue.Count)
        {
            FinishCaptureSequence();
            return;
        }

        BlendShapeKey key = captureQueue[currentCaptureIndex];
        string progressInfo = $"撮影中 ({currentCaptureIndex + 1}/{captureQueue.Count}): {key.Name}";
        
        if (EditorUtility.DisplayCancelableProgressBar("キャプチャ中", progressInfo, (float)currentCaptureIndex / captureQueue.Count))
        {
            FinishCaptureSequence();
            return;
        }

        proxy.ImmediatelySetValue(key, 1.0f);

        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () =>
                {
                    CaptureCurrentFrame(key);
                    proxy.ImmediatelySetValue(key, 0.0f);
                    currentCaptureIndex++;
                    ProcessNextCapture();
                };
            };
        };
    }

    private void CaptureCurrentFrame(BlendShapeKey key)
    {
        if (tempCamera == null) return;

        tempCamera.Render();
        RenderTexture.active = tempRT;
        tempTexture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        tempTexture.Apply();

        byte[] bytes = tempTexture.EncodeToPNG();

        // ★ファイル名「モデル名_表情名」
        string modelName = targetModel.name;
        string blendShapeName = key.Name;
        string fileName = $"{modelName}_{blendShapeName}.png";

        // ファイル名として不適切な文字を置換
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        File.WriteAllBytes(Path.Combine(currentExportPath, fileName), bytes);
    }

    private void FinishCaptureSequence()
    {
        EditorUtility.ClearProgressBar();
        CleanupTempObjects();

        if (componentStateBackup != null)
        {
            foreach (var kvp in componentStateBackup)
            {
                if (kvp.Key != null) kvp.Key.enabled = kvp.Value;
            }
            componentStateBackup = null;
        }

        Debug.Log($"キャプチャ完了: {currentExportPath}");
        EditorUtility.RevealInFinder(currentExportPath);
    }

    private void CleanupTempObjects()
    {
        RenderTexture.active = null;

        if (tempCamera != null)
        {
            tempCamera.targetTexture = null;
            if (tempCamera.gameObject != null) DestroyImmediate(tempCamera.gameObject);
        }
        if (tempRT != null) DestroyImmediate(tempRT);
        if (tempTexture != null) DestroyImmediate(tempTexture);

        tempCamera = null;
        tempRT = null;
        tempTexture = null;
    }
}