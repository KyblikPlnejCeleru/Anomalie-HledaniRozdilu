using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneBuilder
{
    private const string Root = "Assets";
    private static Font _font;
    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    private static int _side;

    public static void BuildAll()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansMono-SemiCondensedSemiBold.ttf");
        CreateMaterials();
        BuildMenu();
        BuildGame();
        BuildWin();
        BuildCredits();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        string name = path.Substring(path.LastIndexOf('/') + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    // ---------- materials ----------
    private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

    private static Material Mat(string name, Color color, float metallic = 0f, float smooth = 0.5f, Color? emission = null, float alpha = 1f)
    {
        if (Materials.TryGetValue(name, out Material cached)) return cached;
        string path = Root + "/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.name = name;
        m.SetColor("_Color", color);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smooth);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        if (alpha < 1f)
        {
            m.SetFloat("_Mode", 3);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = 3000;
            Color c = color; c.a = alpha;
            m.SetColor("_Color", c);
        }
        EditorUtility.SetDirty(m);
        Materials[name] = m;
        return m;
    }

    private static void CreateMaterials()
    {
        Mat("Hull", C(0.06f, 0.09f, 0.13f), 0.45f, 0.45f);
        Mat("Deck", C(0.035f, 0.055f, 0.08f), 0.6f, 0.55f);
        Mat("Alloy", C(0.20f, 0.25f, 0.31f), 0.85f, 0.65f);
        Mat("DarkAlloy", C(0.10f, 0.13f, 0.17f), 0.7f, 0.5f);
        Mat("Cargo", C(0.26f, 0.20f, 0.13f), 0.35f, 0.4f);
        Mat("Accent", C(0.95f, 0.42f, 0.08f), 0.3f, 0.5f, C(2.2f, 0.65f, 0.05f));
        Mat("Cyan", C(0.05f, 0.55f, 0.75f), 0.2f, 0.6f, C(0.1f, 2.4f, 3.4f));
        Mat("Green", C(0.05f, 0.6f, 0.35f), 0.2f, 0.6f, C(0.1f, 2.6f, 1.1f));
        Mat("Amber", C(0.85f, 0.45f, 0.05f), 0.2f, 0.6f, C(3.0f, 1.25f, 0.05f));
        Mat("Magenta", C(0.7f, 0.1f, 0.55f), 0.2f, 0.6f, C(2.8f, 0.1f, 2.2f));
        Mat("White", C(0.75f, 0.9f, 1f), 0f, 0.3f, C(1.6f, 1.9f, 2.2f));
        Mat("Leaf", C(0.10f, 0.45f, 0.22f), 0.1f, 0.4f, C(0.02f, 0.4f, 0.1f));
        Mat("Glass", C(0.25f, 0.65f, 0.85f), 0.1f, 0.9f, null, 0.14f);
        Mat("Screen", C(0.02f, 0.10f, 0.16f), 0.2f, 0.7f, C(0.02f, 0.25f, 0.4f));
    }

    // ---------- primitive helpers ----------
    private static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (parent != null) go.transform.SetParent(parent, true);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject Box(string n, Vector3 p, Vector3 s, Material m, Transform par = null) => Prim(PrimitiveType.Cube, n, p, s, m, par);
    private static GameObject Cyl(string n, Vector3 p, Vector3 s, Material m, Transform par = null) => Prim(PrimitiveType.Cylinder, n, p, s, m, par);
    private static GameObject Sph(string n, Vector3 p, Vector3 s, Material m, Transform par = null) => Prim(PrimitiveType.Sphere, n, p, s, m, par);

    private static Vector3 P(float x, float y, float z) => new Vector3(_side * x, y, z);

    private static Anomaly AddAnomaly(GameObject go, AnomalyKind kind, string label)
    {
        Anomaly a = go.AddComponent<Anomaly>();
        SerializedObject so = new SerializedObject(a);
        so.FindProperty("_kind").enumValueIndex = (int)kind;
        so.FindProperty("_label").stringValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
        return a;
    }

    private static void Configure(Anomaly a, Action<SerializedObject> setup)
    {
        SerializedObject so = new SerializedObject(a);
        setup(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- shared room content ----------
    private static void BuildRoom(int side, bool wrong)
    {
        _side = side;
        Transform root = new GameObject(wrong ? "Laboratoř_B" : "Laboratoř_A").transform;

        // Reactor
        Transform reactor = new GameObject("Reaktor").transform;
        reactor.SetParent(root, false);
        Cyl("Podstavec", P(5.6f, 0.15f, -4.2f), new Vector3(1.7f, 0.3f, 1.7f), Materials["DarkAlloy"], reactor);
        Cyl("Tělo", P(5.6f, 1.35f, -4.2f), new Vector3(1.25f, 2.2f, 1.25f), Materials["Alloy"], reactor);
        GameObject core = Sph("Jádro", P(5.6f, 1.7f, -4.2f), new Vector3(0.85f, 0.85f, 0.85f), wrong ? Materials["Magenta"] : Materials["Cyan"], reactor);
        Cyl("Prstenec", P(5.6f, 2.5f, -4.2f), new Vector3(1.5f, 0.12f, 1.5f), Materials["Alloy"], reactor);
        GameObject antenna = Cyl("Anténa", P(5.6f, 3.15f, -4.2f), new Vector3(0.08f, 1.25f, 0.08f), Materials["Alloy"], reactor);
        if (wrong)
        {
            antenna.transform.rotation = Quaternion.Euler(0f, 0f, 75f);
            Anomaly a = AddAnomaly(antenna, AnomalyKind.Rotated, "Anténa");
            Configure(a, so => so.FindProperty("_correctRotation").quaternionValue = Quaternion.identity);

            Anomaly c = AddAnomaly(core, AnomalyKind.WrongColor, "Reaktorové jádro");
            Configure(c, so =>
            {
                so.FindProperty("_tinted").objectReferenceValue = core.GetComponent<Renderer>();
                so.FindProperty("_correctColor").colorValue = new Color(0.1f, 2.4f, 3.4f);
                so.FindProperty("_emissive").boolValue = true;
            });
        }

        // Console
        Box("Pult", P(2.6f, 0.45f, 0.8f), new Vector3(2.4f, 0.9f, 1.2f), Materials["DarkAlloy"], root);
        Box("Deska", P(2.6f, 0.95f, 0.8f), new Vector3(2.6f, 0.12f, 1.35f), Materials["Alloy"], root);
        GameObject consoleScreen = Box("Displej", P(2.6f, 1.5f, 0.45f), new Vector3(2.0f, 0.85f, 0.1f), Materials["Green"], root);
        consoleScreen.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
        for (int i = 0; i < 4; i++)
            Box("Tlačítko" + i, P(1.9f + i * 0.4f, 1.04f, 1.25f), new Vector3(0.18f, 0.05f, 0.18f), i == 2 ? Materials["Amber"] : Materials["Cyan"], root);

        // Cargo stack
        float cx = wrong ? 10.4f : 9.2f, cz = wrong ? -3.8f : -2.2f;
        Transform crates = new GameObject("Náklad").transform;
        crates.SetParent(root, false);
        crates.position = P(cx, 0f, cz);
        Box("Bedna1", Vector3.zero, new Vector3(1.35f, 1.2f, 1.35f), Materials["Cargo"], crates).transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Box("Bedna2", Vector3.zero, new Vector3(1.15f, 1.0f, 1.15f), Materials["Cargo"], crates).transform.localPosition = new Vector3(0f, 1.7f, 0f);
        Box("Pruh", Vector3.zero, new Vector3(1.38f, 0.12f, 1.38f), Materials["Accent"], crates).transform.localPosition = new Vector3(0f, 1.2f, 0f);
        if (wrong)
        {
            Anomaly a = AddAnomaly(crates.gameObject, AnomalyKind.Moved, "Náklad");
            Configure(a, so => so.FindProperty("_correctPosition").vector3Value = P(9.2f, 0f, -2.2f));
        }

        // Hydroponics
        Cyl("Květináč", P(10.4f, 0.35f, 3.2f), new Vector3(0.8f, 0.7f, 0.8f), Materials["Alloy"], root);
        Cyl("Stonek", P(10.4f, 1.1f, 3.2f), new Vector3(0.06f, 1.1f, 0.06f), Materials["Leaf"], root);
        for (int i = 0; i < 5; i++)
        {
            float ang = i * 72f * Mathf.Deg2Rad;
            GameObject leaf = Sph("List" + i, P(10.4f + Mathf.Cos(ang) * 0.35f, 1.45f + (i % 2) * 0.2f, 3.2f + Mathf.Sin(ang) * 0.35f), new Vector3(0.55f, 0.14f, 0.26f), Materials["Leaf"], root);
            leaf.transform.rotation = Quaternion.Euler(0f, -_side * i * 72f, 0f);
        }
        Box("GrowLight", P(10.4f, 2.55f, 3.2f), new Vector3(1.3f, 0.1f, 1.3f), Materials["Magenta"], root);

        // Wall monitor
        Box("Rám", P(2.2f, 2.8f, -6.95f), new Vector3(2.7f, 1.6f, 0.16f), Materials["Alloy"], root);
        GameObject panel = Box("Panel", P(2.2f, 2.8f, -6.83f), new Vector3(2.35f, 1.25f, 0.08f), wrong ? Materials["Amber"] : Materials["Green"], root);
        if (wrong)
        {
            Anomaly a = AddAnomaly(panel, AnomalyKind.WrongColor, "Nástěnný monitor");
            Configure(a, so =>
            {
                so.FindProperty("_tinted").objectReferenceValue = panel.GetComponent<Renderer>();
                so.FindProperty("_correctColor").colorValue = new Color(0.1f, 2.6f, 1.1f);
                so.FindProperty("_emissive").boolValue = true;
            });
        }

        // Drone
        Transform drone = new GameObject("Dron").transform;
        drone.SetParent(root, false);
        drone.position = P(6.2f, 2.2f, 3.4f);
        Sph("Tělo", Vector3.zero, new Vector3(0.75f, 0.32f, 0.75f), Materials["Alloy"], drone).transform.localPosition = Vector3.zero;
        Sph("Oko", Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), Materials["Cyan"], drone).transform.localPosition = new Vector3(0f, 0f, 0.38f);
        for (int i = 0; i < 4; i++)
        {
            float ang = (45f + i * 90f) * Mathf.Deg2Rad;
            GameObject rotor = Cyl("Rotor" + i, Vector3.zero, new Vector3(0.42f, 0.05f, 0.42f), Materials["DarkAlloy"], drone);
            rotor.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.62f, 0.16f, Mathf.Sin(ang) * 0.62f);
        }
        if (wrong)
        {
            drone.gameObject.SetActive(false);
            GameObject marker = new GameObject("Chybí_dron");
            marker.transform.SetParent(root, false);
            marker.transform.position = P(6.2f, 2.2f, 3.4f);
            BoxCollider bc = marker.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.7f, 0.9f, 1.7f);
            Anomaly a = AddAnomaly(marker, AnomalyKind.Missing, "Chybějící dron");
            Configure(a, so => so.FindProperty("_reveal").objectReferenceValue = drone.gameObject);
        }

        // Tool cart
        float tx = wrong ? 5.6f : 4.4f, tz = wrong ? 2.8f : 4.4f;
        Transform cart = new GameObject("Vozík").transform;
        cart.SetParent(root, false);
        cart.position = P(tx, 0f, tz);
        Box("Korpus", Vector3.zero, new Vector3(1.25f, 0.8f, 0.75f), Materials["Alloy"], cart).transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Box("Tray", Vector3.zero, new Vector3(1.35f, 0.08f, 0.85f), Materials["DarkAlloy"], cart).transform.localPosition = new Vector3(0f, 0.94f, 0f);
        for (int i = 0; i < 4; i++)
        {
            GameObject wheel = Cyl("Kolo" + i, Vector3.zero, new Vector3(0.22f, 0.09f, 0.22f), Materials["DarkAlloy"], cart);
            wheel.transform.localPosition = new Vector3(i < 2 ? -0.45f : 0.45f, 0.14f, i % 2 == 0 ? -0.28f : 0.28f);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        if (wrong)
        {
            Anomaly a = AddAnomaly(cart.gameObject, AnomalyKind.Moved, "Nástrojový vozík");
            Configure(a, so => so.FindProperty("_correctPosition").vector3Value = P(4.4f, 0f, 4.4f));
        }

        // Sample display
        Cyl("Stojan", P(7.4f, 0.5f, 1.2f), new Vector3(0.7f, 1.0f, 0.7f), Materials["DarkAlloy"], root);
        Cyl("Sklo", P(7.4f, 1.35f, 1.2f), new Vector3(0.52f, 0.75f, 0.52f), Materials["Glass"], root);
        GameObject sample = Sph("Vzorek", P(7.4f, 1.35f, 1.2f), wrong ? new Vector3(0.72f, 0.72f, 0.72f) : new Vector3(0.35f, 0.35f, 0.35f), Materials["Magenta"], root);
        if (wrong)
        {
            Anomaly a = AddAnomaly(sample, AnomalyKind.WrongScale, "Vzorek");
            Configure(a, so => so.FindProperty("_correctScale").vector3Value = new Vector3(0.35f, 0.35f, 0.35f));
        }

        // Extra crate (only in the anomalous room)
        if (wrong)
        {
            GameObject extra = Box("Navíc_bedna", P(3.9f, 0.45f, 5.6f), new Vector3(0.9f, 0.9f, 0.9f), Materials["Cargo"], root);
            AddAnomaly(extra, AnomalyKind.Extra, "Navíc bedna");
        }

        // Fixed details in both rooms
        for (int i = 0; i < 4; i++)
            Cyl("Trubka" + i, P(2f + i * 2.4f, 4.25f, -6.9f), new Vector3(0.09f, 2.4f, 0.09f), Materials["Alloy"], root).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        Box("Světelný_pás", P(3.5f, 5f, -1f), new Vector3(0.22f, 0.08f, 10f), Materials["Cyan"], root);
        Box("Světelný_pás2", P(8.5f, 5f, -1f), new Vector3(0.22f, 0.08f, 10f), Materials["Cyan"], root);
        Box("Podlahová_značka", P(6f, 0.02f, 5.5f), new Vector3(5f, 0.03f, 0.1f), Materials["Accent"], root);
    }

    // ---------- shared environment ----------
    private static void BuildShell()
    {
        Box("Podlaha", new Vector3(0f, -0.12f, 0f), new Vector3(24.5f, 0.24f, 15f), Materials["Deck"]);
        Box("Strop", new Vector3(0f, 5.25f, 0f), new Vector3(24.5f, 0.2f, 15f), Materials["Hull"]);
        Box("Zadní_stěna", new Vector3(0f, 2.5f, -7.3f), new Vector3(24.5f, 5.2f, 0.3f), Materials["Hull"]);
        Box("Levá_stěna", new Vector3(-12.2f, 2.5f, 0f), new Vector3(0.3f, 5.2f, 15f), Materials["Hull"]);
        Box("Pravá_stěna", new Vector3(12.2f, 2.5f, 0f), new Vector3(0.3f, 5.2f, 15f), Materials["Hull"]);

        // Window panels with stars
        for (int s = -1; s <= 1; s += 2)
        {
            Box("Okno", new Vector3(s * 6.5f, 2.9f, -7.12f), new Vector3(8.5f, 2.6f, 0.06f), Materials["Screen"]);
            UnityEngine.Random.InitState(s);
            for (int i = 0; i < 26; i++)
            {
                Box("Hvězda", new Vector3(s * 6.5f + UnityEngine.Random.Range(-3.9f, 3.9f), 2.9f + UnityEngine.Random.Range(-1.1f, 1.1f), -7.05f),
                    Vector3.one * UnityEngine.Random.Range(0.025f, 0.06f), Materials["White"]);
            }
        }

        // Central energy divider with a front gap
        Box("Dělítko", new Vector3(0f, 0.55f, -1.7f), new Vector3(0.34f, 1.1f, 11f), Materials["DarkAlloy"]);
        Box("Dělítko_světlo", new Vector3(0f, 1.14f, -1.7f), new Vector3(0.38f, 0.08f, 11f), Materials["Cyan"]);
        Box("Dělítko_sklo", new Vector3(0f, 2.45f, -1.7f), new Vector3(0.06f, 2.6f, 11f), Materials["Glass"]);
        for (int i = 0; i < 5; i++)
            Box("Sloupek" + i, new Vector3(0f, 2.5f, -7f + i * 2.7f), new Vector3(0.22f, 5f, 0.22f), Materials["Alloy"]);

        Light sun = new GameObject("Světlo").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = C(0.65f, 0.8f, 1f);
        sun.intensity = 0.55f;
        sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        for (int i = 0; i < 4; i++)
        {
            Light p = new GameObject("Bodové_světlo" + i).AddComponent<Light>();
            p.type = LightType.Point;
            p.color = i % 2 == 0 ? C(0.2f, 0.75f, 1f) : C(0.9f, 0.4f, 0.9f);
            p.intensity = 1.6f;
            p.range = 13f;
            p.transform.position = new Vector3(i < 2 ? -5f : 5f, 4f, i % 2 == 0 ? -3f : 3f);
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = C(0.09f, 0.11f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = C(0.02f, 0.03f, 0.06f);
        RenderSettings.fogDensity = 0.008f;
    }

    // ---------- UI helpers ----------
    private static Canvas MakeCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    private static void MakeEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static Text MakeText(Transform parent, string name, string content, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 area, TextAnchor align = TextAnchor.MiddleCenter)
    {
        RectTransform rt = Rect(parent, name, anchor, pos, area);
        rt.gameObject.AddComponent<CanvasRenderer>();
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = _font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Image MakeImage(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool stretch = false)
    {
        RectTransform rt = stretch ? RectStretch(parent, name) : Rect(parent, name, anchor, pos, size);
        rt.gameObject.AddComponent<CanvasRenderer>();
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static RectTransform RectStretch(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(parent, name, new Vector2(0.5f, 0.5f), pos, size);
        rt.gameObject.AddComponent<CanvasRenderer>();
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.02f, 0.12f, 0.18f, 0.95f);
        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.85f, 1f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(0.1f, 0.4f, 0.5f, 1f);
        cb.pressedColor = new Color(0.05f, 0.25f, 0.35f, 1f);
        b.colors = cb;
        MakeText(rt, "Popisek", label, 30, new Color(0.5f, 1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(10f, 4f));
        return b;
    }

    private static Camera MakeCamera(Vector3 pos, Quaternion rot, Color bg)
    {
        GameObject go = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        go.transform.position = pos;
        go.transform.rotation = rot;
        Camera cam = go.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        cam.fieldOfView = 72f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 250f;
        return cam;
    }

    private static void Save(Scene scene, string name)
    {
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + name + ".unity");
    }

    private static Scene NewScene(string name)
    {
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    // ---------- menu / win / credits backdrop ----------
    private static void BuildBackdrop(Color bg, float spin)
    {
        MakeCamera(new Vector3(0f, 2.2f, 12f), Quaternion.Euler(-6f, 0f, 0f), bg);
        Light l = new GameObject("Klíčové_světlo").AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
        l.color = C(0.5f, 0.8f, 1f);
        l.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
        Light rim = new GameObject("Okrajové_světlo").AddComponent<Light>();
        rim.type = LightType.Point;
        rim.color = new Color(0.9f, 0.2f, 0.8f);
        rim.intensity = 4f;
        rim.range = 18f;
        rim.transform.position = new Vector3(-4f, 3f, -2f);

        Transform station = new GameObject("Stanice").transform;
        station.position = new Vector3(0f, 1.6f, 0f);
        station.gameObject.AddComponent<Spin>().enabled = true;
        SerializedObject so = new SerializedObject(station.gameObject.GetComponent<Spin>());
        so.FindProperty("_degreesPerSecond").floatValue = spin;
        so.ApplyModifiedPropertiesWithoutUndo();
        GameObject coreModel = Cyl("Jádro", Vector3.zero, new Vector3(2.2f, 2.4f, 2.2f), Materials["Alloy"], station);
        coreModel.transform.localPosition = Vector3.zero;
        coreModel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Sph("Centrum", Vector3.zero, Vector3.one * 1.3f, Materials["Cyan"], station).transform.localPosition = Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            GameObject arm = Box("Rameno" + i, Vector3.zero, new Vector3(1.6f, 0.18f, 0.5f), Materials["Alloy"], station);
            arm.transform.localPosition = new Vector3(Mathf.Cos(a) * 2.2f, Mathf.Sin(a) * 2.2f, 0f);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
            GameObject pod = Box("Panel" + i, Vector3.zero, new Vector3(1.2f, 0.7f, 0.12f), i % 2 == 0 ? Materials["Cyan"] : Materials["Magenta"], station);
            pod.transform.localPosition = new Vector3(Mathf.Cos(a) * 3.2f, Mathf.Sin(a) * 3.2f, 0f);
            pod.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
        }
        UnityEngine.Random.InitState(9);
        for (int i = 0; i < 90; i++)
        {
            Sph("Hvězda" + i, new Vector3(UnityEngine.Random.Range(-18f, 18f), UnityEngine.Random.Range(-9f, 12f), UnityEngine.Random.Range(-22f, -7f)),
                Vector3.one * UnityEngine.Random.Range(0.03f, 0.1f), Materials["White"]);
        }
    }

    // ---------- scenes ----------
    private static void BuildMenu()
    {
        Scene s = NewScene("Menu");
        BuildBackdrop(C(0.015f, 0.025f, 0.05f), 9f);
        MakeEventSystem();
        Canvas c = MakeCanvas("MenuUI");
        Color cyan = new Color(0.45f, 0.95f, 1f);
        MakeText(c.transform, "Titul", "A N O M A L I E", 92, cyan, new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(1500f, 130f));
        MakeText(c.transform, "Podtitul", "POZOROVATELSKÁ STANICE KEPLER-9", 28, new Color(0.7f, 0.85f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(1400f, 50f));
        MakeButton(c.transform, "BtnStart", "ZAČÍT HRÁT", new Vector2(0f, 55f), new Vector2(360f, 74f));
        MakeButton(c.transform, "BtnCredits", "TITULY", new Vector2(0f, -45f), new Vector2(360f, 74f));
        MakeButton(c.transform, "BtnQuit", "UKONČIT", new Vector2(0f, -145f), new Vector2(360f, 74f));
        MakeText(c.transform, "Nápověda", "Najdi 8 odchylek mezi dvěma zrcadlovými laboratořemi.", 22, new Color(0.55f, 0.7f, 0.8f), new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(1400f, 50f));
        new GameObject("MenuController").AddComponent<MenuController>();
        Save(s, "Menu");
    }

    private static void BuildGame()
    {
        Scene s = NewScene("Game");
        BuildShell();
        BuildRoom(-1, false);
        BuildRoom(1, true);

        GameObject player = new GameObject("Hráč", typeof(CharacterController));
        player.transform.position = new Vector3(0f, 0.1f, 6.2f);
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.35f;
        player.AddComponent<StationPlayer>();
        Camera cam = MakeCamera(new Vector3(0f, 1.65f, 0f), Quaternion.identity, C(0.015f, 0.025f, 0.05f));
        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.65f, 0f);

        MakeEventSystem();
        Canvas c = MakeCanvas("HudUI");
        Color cyan = new Color(0.45f, 0.95f, 1f);
        Text score = MakeText(c.transform, "Skóre", "SKÓRE 000", 30, cyan, new Vector2(0f, 1f), new Vector2(210f, -45f), new Vector2(390f, 55f), TextAnchor.MiddleLeft);
        Text time = MakeText(c.transform, "Čas", "ČAS 00:00", 30, cyan, new Vector2(1f, 1f), new Vector2(-210f, -45f), new Vector2(390f, 55f), TextAnchor.MiddleRight);
        Text found = MakeText(c.transform, "Nálezy", "ANOMÁLIE 0 / 8", 27, new Color(1f, 0.78f, 0.3f), new Vector2(0f, 0f), new Vector2(190f, 48f), new Vector2(360f, 50f), TextAnchor.MiddleLeft);
        Text feed = MakeText(c.transform, "Zpráva", "", 34, new Color(1f, 1f, 1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(900f, 60f));
        Image flash = MakeImage(c.transform, "Záblesk", new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.zero, Vector2.zero, true);
        MakeText(c.transform, "Zaměřovač", "+", 26, new Color(0.7f, 1f, 1f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
        MakeText(c.transform, "Ovládání", "WASD pohyb   SHIFT sprint   MEZERKA skok   LEVÝ KLIK oprava   ESC pauza", 19, new Color(0.55f, 0.7f, 0.8f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1300f, 40f));
        Hud hud = new GameObject("Hud").AddComponent<Hud>();
        hud.ScoreLabel = score;
        hud.TimeLabel = time;
        hud.FoundLabel = found;
        hud.FeedLabel = feed;
        hud.Flash = flash;
        Interactor interactor = player.AddComponent<Interactor>();
        interactor.Hud = hud;

        GameObject pause = MakeImage(c.transform, "PauzaPanel", new Color(0f, 0f, 0f, 0.82f), Vector2.zero, Vector2.zero, Vector2.zero, true).gameObject;
        MakeText(pause.transform, "Název", "P A U Z A", 62, cyan, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(900f, 90f));
        Button resume = MakeButton(pause.transform, "BtnResume", "POKRAČOVAT", new Vector2(0f, 70f), new Vector2(340f, 70f));
        Button restart = MakeButton(pause.transform, "BtnRestart", "NOVÁ HRA", new Vector2(0f, -25f), new Vector2(340f, 70f));
        Button menu = MakeButton(pause.transform, "BtnMenu", "HLAVNÍ MENU", new Vector2(0f, -120f), new Vector2(340f, 70f));
        PauseController pauseController = new GameObject("PauseController").AddComponent<PauseController>();
        pauseController.Panel = pause;
        pauseController.ResumeButton = resume;
        pauseController.RestartButton = restart;
        pauseController.MenuButton = menu;
        pause.SetActive(false);
        new GameObject("GameData").AddComponent<GameData>();

        Save(s, "Game");
    }

    private static void BuildWin()
    {
        Scene s = NewScene("Win");
        BuildBackdrop(C(0.01f, 0.035f, 0.03f), 16f);
        MakeEventSystem();
        Canvas c = MakeCanvas("WinUI");
        Color green = new Color(0.4f, 1f, 0.7f);
        MakeText(c.transform, "Titul", "S K E N   D O K O N Č E N", 68, green, new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(1500f, 100f));
        Text score = MakeText(c.transform, "Skóre", "", 46, new Color(0.6f, 0.95f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1200f, 70f));
        Text detail = MakeText(c.transform, "Detail", "", 25, new Color(0.75f, 0.85f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1300f, 90f));
        Text rank = MakeText(c.transform, "Hodnocení", "", 38, new Color(1f, 0.8f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(1000f, 60f));
        MakeButton(c.transform, "BtnBack", "ZPĚT DO MENU", new Vector2(0f, -190f), new Vector2(340f, 72f));
        WinController win = new GameObject("WinController").AddComponent<WinController>();
        win.ScoreLabel = score;
        win.DetailLabel = detail;
        win.RankLabel = rank;
        Save(s, "Win");
    }

    private static void BuildCredits()
    {
        Scene s = NewScene("Credits");
        BuildBackdrop(C(0.025f, 0.015f, 0.04f), 6f);
        MakeEventSystem();
        Canvas c = MakeCanvas("CreditsUI");
        MakeText(c.transform, "Titul", "T I T U L Y   /   C R E D I T S", 52, new Color(0.6f, 0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1500f, 80f));
        string text =
            "POUŽITÉ ASSETY\n" +
            "Vestavěné Unity primitivy (Cube, Sphere, Cylinder) a vlastní materiály.\n" +
            "Font Noto Sans Mono (SIL Open Font License, systémový font).\n" +
            "Žádné assety z Unity Asset Store.\n\n" +
            "AI A LIDÉ\n" +
            "AI asistent (OpenCode / Big Pickle) vytvořil na základě zadání kompletně\n" +
            "kód, 3D scény, UI a texty – přibližně 95 % práce.\n" +
            "Člověk dodal zadání, zkontroloval hru a provedl odevzdání.\n\n" +
            "Engine: Unity 6000.3.22f1";
        MakeText(c.transform, "Text", text, 25, new Color(0.8f, 0.9f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0f, 55f), new Vector2(1450f, 560f), TextAnchor.UpperLeft);
        MakeButton(c.transform, "BtnBack", "ZPĚT DO MENU", new Vector2(0f, -320f), new Vector2(340f, 70f));
        new GameObject("MenuController").AddComponent<MenuController>();
        Save(s, "Credits");
    }
}
