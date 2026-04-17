// StudioEnvironmentEditor.cs
// 床・天井・四方の壁を生成・管理するEditorウィンドウ
using UnityEngine;
using UnityEditor;
using Nekometrica;

public class StudioEnvironmentWindow : EditorWindow
{
    // -------- 設定 --------
    private float roomWidth  = 10f;
    private float roomDepth  = 10f;
    private float roomHeight = 5f;

    // 6面の設定: [床, 天井, 奥, 手前, 左, 右]
    private bool[]  panelEnabled = { true,  false, true,  false, false, false };
    private Color[] panelColor   =
    {
        new Color(0.85f, 0.85f, 0.85f), // 床
        new Color(0.95f, 0.95f, 0.95f), // 天井
        new Color(0.90f, 0.90f, 0.90f), // 奥の壁
        new Color(0.90f, 0.90f, 0.90f), // 手前の壁
        new Color(0.88f, 0.88f, 0.88f), // 左の壁
        new Color(0.88f, 0.88f, 0.88f), // 右の壁
    };
    private static readonly string[] PanelNames = { "床", "天井", "奥の壁", "手前の壁", "左の壁", "右の壁" };

    private StudioEnvironment currentEnv;
    private Vector2 scrollPos;

    // -------- メニュー --------
    [MenuItem("Tools/Studio Environment")]
    public static void ShowWindow()
    {
        var w = GetWindow<StudioEnvironmentWindow>("Studio Environment");
        w.minSize = new Vector2(340, 560);
        w.RefreshFromScene();
    }

    private void OnFocus() { RefreshFromScene(); }

    private void RefreshFromScene()
    {
        currentEnv = FindObjectOfType<StudioEnvironment>();
        if (currentEnv == null) return;

        roomWidth  = currentEnv.roomWidth;
        roomDepth  = currentEnv.roomDepth;
        roomHeight = currentEnv.roomHeight;

        var panels = currentEnv.AllPanels;
        for (int i = 0; i < panels.Length; i++)
        {
            panelEnabled[i] = panels[i].enabled;
            panelColor[i]   = panels[i].color;
        }
    }

    // -------- GUI --------
    private void OnGUI()
    {
        GUILayout.Label("Studio Environment", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ---- サイズ ----
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("部屋のサイズ", EditorStyles.boldLabel);
            roomWidth  = EditorGUILayout.FloatField("横幅 (m)",   roomWidth);
            roomDepth  = EditorGUILayout.FloatField("奥行き (m)", roomDepth);
            roomHeight = EditorGUILayout.FloatField("高さ (m)",   roomHeight);
        }

        EditorGUILayout.Space(6);

        // ---- 6面パネル設定 ----
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("パネル設定（表示・色）", EditorStyles.boldLabel);

            // ヘッダー行
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("",         GUILayout.Width(12));
            GUILayout.Label("パネル",   GUILayout.Width(80));
            GUILayout.Label("表示",     GUILayout.Width(36));
            GUILayout.Label("色",       GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            DrawDivider();

            for (int i = 0; i < PanelNames.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("",                    GUILayout.Width(12));
                GUILayout.Label(PanelNames[i],         GUILayout.Width(80));
                panelEnabled[i] = EditorGUILayout.Toggle(panelEnabled[i], GUILayout.Width(36));
                GUI.enabled = panelEnabled[i];
                panelColor[i]   = EditorGUILayout.ColorField(panelColor[i], GUILayout.ExpandWidth(true));
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space(6);

        // ---- ボタン ----
        bool envExists = (currentEnv != null);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("操作", EditorStyles.boldLabel);

            GUI.backgroundColor = envExists ? new Color(1f, 0.85f, 0.5f) : new Color(0.6f, 1f, 0.7f);
            if (GUILayout.Button(envExists ? "スタジオを再生成" : "スタジオを生成", GUILayout.Height(36)))
                CreateOrRebuild();
            GUI.backgroundColor = Color.white;

            if (envExists)
            {
                EditorGUILayout.Space(4);
                if (GUILayout.Button("色・表示だけ更新"))
                    ApplyToExisting();

                EditorGUILayout.Space(4);
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("スタジオを削除"))
                {
                    if (EditorUtility.DisplayDialog("確認", "スタジオを削除しますか？", "削除", "キャンセル"))
                    {
                        Undo.DestroyObjectImmediate(currentEnv.gameObject);
                        currentEnv = null;
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }

        EditorGUILayout.Space(6);

        if (envExists)
        {
            EditorGUILayout.HelpBox(
                "各パネルはヒエラルキーで選択してTransformを直接編集できます。\n" +
                "位置・サイズ調整はUnityのMove/Scaleツールをどうぞ。",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("シーンにスタジオがありません。", MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawDivider()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }

    // -------- 生成 --------
    private void CreateOrRebuild()
    {
        var existing = FindObjectOfType<StudioEnvironment>();
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject root = new GameObject("StudioEnvironment");
        Undo.RegisterCreatedObjectUndo(root, "Create Studio Environment");
        var env = root.AddComponent<StudioEnvironment>();

        env.roomWidth  = roomWidth;
        env.roomDepth  = roomDepth;
        env.roomHeight = roomHeight;

        float hw = roomWidth  * 0.5f;
        float hd = roomDepth  * 0.5f;
        float hh = roomHeight * 0.5f;

        // 6面の定義: (localPos, eulerAngles, scale)
        // 法線方向はCull Offで両面表示するので飾りだが、ライティングのために正しく設定
        var defs = new (Vector3 pos, Vector3 euler, Vector3 scale)[]
        {
            // 床 (Y=0, 法線 +Y)
            (new Vector3(0, 0, 0),           new Vector3(90, 0, 0),    new Vector3(roomWidth, roomDepth, 1)),
            // 天井 (Y=roomHeight, 法線 -Y)
            (new Vector3(0, roomHeight, 0),  new Vector3(-90, 0, 0),   new Vector3(roomWidth, roomDepth, 1)),
            // 奥の壁 (+Z, 法線 -Z)
            (new Vector3(0, hh, hd),         new Vector3(0, 180, 0),   new Vector3(roomWidth, roomHeight, 1)),
            // 手前の壁 (-Z, 法線 +Z)
            (new Vector3(0, hh, -hd),        new Vector3(0, 0, 0),     new Vector3(roomWidth, roomHeight, 1)),
            // 左の壁 (-X, 法線 +X)
            (new Vector3(-hw, hh, 0),        new Vector3(0, 90, 0),    new Vector3(roomDepth, roomHeight, 1)),
            // 右の壁 (+X, 法線 -X)
            (new Vector3(hw, hh, 0),         new Vector3(0, -90, 0),   new Vector3(roomDepth, roomHeight, 1)),
        };

        var panels = env.AllPanels;
        for (int i = 0; i < panels.Length; i++)
        {
            var p = panels[i];
            p.enabled = panelEnabled[i];
            p.color   = panelColor[i];
            p.go      = CreatePanel(root, PanelNames[i], defs[i].pos, defs[i].euler, defs[i].scale, p.color);
            p.go.SetActive(p.enabled);
        }

        // StudioPanel は参照型なので AllPanels から書き戻し不要（同一インスタンス）
        // ただし floor/ceiling などのフィールドに反映されているか確認のため手動設定
        env.floor.go     = panels[0].go; env.floor.enabled     = panelEnabled[0]; env.floor.color     = panelColor[0];
        env.ceiling.go   = panels[1].go; env.ceiling.enabled   = panelEnabled[1]; env.ceiling.color   = panelColor[1];
        env.wallBack.go  = panels[2].go; env.wallBack.enabled  = panelEnabled[2]; env.wallBack.color  = panelColor[2];
        env.wallFront.go = panels[3].go; env.wallFront.enabled = panelEnabled[3]; env.wallFront.color = panelColor[3];
        env.wallLeft.go  = panels[4].go; env.wallLeft.enabled  = panelEnabled[4]; env.wallLeft.color  = panelColor[4];
        env.wallRight.go = panels[5].go; env.wallRight.enabled = panelEnabled[5]; env.wallRight.color = panelColor[5];

        currentEnv = env;
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(env);
    }

    private void ApplyToExisting()
    {
        if (currentEnv == null) return;
        Undo.RecordObject(currentEnv, "Update Studio Environment");

        var panels = currentEnv.AllPanels;
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].enabled = panelEnabled[i];
            panels[i].color   = panelColor[i];
        }
        currentEnv.ApplyAll();
        EditorUtility.SetDirty(currentEnv);
    }

    // -------- パネル作成 --------
    private GameObject CreatePanel(GameObject parent, string panelName,
                                   Vector3 localPos, Vector3 localEuler, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = panelName;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition    = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale       = scale;

        // コライダー不要
        var col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        // シャドウ設定: 受け取り ON、両面キャスト
        var mr = go.GetComponent<MeshRenderer>();
        mr.receiveShadows    = true;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        // マテリアル
        var mat = new Material(GetLitShader());
        mat.name = panelName + "_Mat";
        mat.color = color;
        mat.SetFloat("_Metallic",   0f);
        mat.SetFloat("_Smoothness", 0.05f);
        // 両面レンダリング (Cull Off)
        mat.SetFloat("_Cull",     0f);
        mat.SetFloat("_CullMode", 0f);
        // URP: 両面法線 — 裏面のライティングを正常にする
        mat.EnableKeyword("_DOUBLESIDED_ON");
        mat.SetFloat("_DoubleSidedEnable",     1f);
        mat.SetFloat("_DoubleSidedNormalMode", 1f); // 1 = Mirror
        mr.sharedMaterial = mat;

        return go;
    }

    private Shader GetLitShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Diffuse");
    }
}
