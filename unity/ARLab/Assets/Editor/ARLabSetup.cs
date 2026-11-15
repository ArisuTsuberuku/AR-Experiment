using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using InputTrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;
using Object = UnityEngine.Object;

/// <summary>
/// Thiết lập dự án AR Lab và build APK — chạy được trong Editor (menu "AR Lab")
/// hoặc ở chế độ nền:
///   Unity.exe -batchmode -projectPath unity/ARLab -buildTarget Android
///             -executeMethod ARLabSetup.SetupAndBuild -logFile work/logs/build.log
/// Mã thoát 0 = APK tạo xong ở work/build/ARLab.apk.
/// </summary>
public static class ARLabSetup
{
    const string ScenePath = "Assets/Scenes/ARLab.unity";
    const string StreamingDir = "Assets/StreamingAssets/experiments";
    const string ModelsDir = "Assets/Models";
    const string VariantDir = "Assets/Resources/BienTheShader";
    const string UiDir = "Assets/UI";
    const string PanelSettingsPath = UiDir + "/ARLab_PanelSettings.asset";
    const string PlanePrefabPath = "Assets/Prefabs/ARPlaneHienThi.prefab";
    const string PlaneMatPath = "Assets/Materials/MatPhangAR.mat";
    const string AppId = "vn.arexperiment.arlab";

    [MenuItem("AR Lab/1. Thiết lập dự án và dựng cảnh")]
    public static void Setup()
    {
        ConfigurePlayer();
        ConfigureXR();
        ConfigureUrp();
        CopyPackages();
        KeepGltfShaderVariants();
        BuildScene();
        AssetDatabase.SaveAssets();
        Debug.Log("ARLAB_SETUP_OK");
    }

    [MenuItem("AR Lab/2. Build APK")]
    public static void BuildApkMenu() => BuildApk();

    public static void SetupAndBuild()
    {
        int code = 1;
        try
        {
            Setup();
            code = BuildApk();
        }
        catch (Exception e)
        {
            Debug.LogError("ARLAB_FAIL " + e);
        }
        EditorApplication.Exit(code);
    }

    // ------------------------------------------------------------------ Player
    static void ConfigurePlayer()
    {
        var android = NamedBuildTarget.Android;
        PlayerSettings.companyName = "AR Experiment";
        PlayerSettings.productName = "AR Lab";
        PlayerSettings.bundleVersion = "0.2.0";
        PlayerSettings.Android.bundleVersionCode = 2;
        PlayerSettings.SetApplicationIdentifier(android, AppId);
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if ((int)PlayerSettings.Android.minSdkVersion < 26)
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.Android.forceSDCardPermission = true;   // nút Chụp trên Android 8–9 cần quyền ghi bộ nhớ (10+ không dùng)
        Debug.Log($"ARLAB player: {AppId}, minSdk {PlayerSettings.Android.minSdkVersion}, IL2CPP ARM64, GLES3");
    }

    // ------------------------------------------------------------------ XR (ARCore)
    static void ConfigureXR()
    {
        EnsureFolder("Assets/XR");
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
        if (perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        }
        string assetPath = AssetDatabase.GetAssetPath(perTarget);

        // Android: ARCore. Standalone: XR Simulation để thử màn AR trong Editor (không vào APK).
        AssignLoader(perTarget, assetPath, BuildTargetGroup.Android, "Android", "UnityEngine.XR.ARCore.ARCoreLoader");
        AssignLoader(perTarget, assetPath, BuildTargetGroup.Standalone, "Standalone", "UnityEngine.XR.Simulation.SimulationLoader");
        ConfigureARCoreRequirements();
    }

    static void AssignLoader(XRGeneralSettingsPerBuildTarget perTarget, string assetPath, BuildTargetGroup group, string ten, string loader)
    {
        var settings = perTarget.SettingsForBuildTarget(group);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            settings.name = ten + " Settings";
            AssetDatabase.AddObjectToAsset(settings, assetPath);
            perTarget.SetSettingsForBuildTarget(group, settings);
        }
        if (settings.Manager == null)
        {
            var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            manager.name = ten + " Providers";
            AssetDatabase.AddObjectToAsset(manager, assetPath);
            settings.Manager = manager;
        }
        settings.InitManagerOnStart = false;   // XR chỉ nạp khi vào màn AR (PhienAR)
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(perTarget);
        AssetDatabase.SaveAssets();

        bool ok = XRPackageMetadataStore.AssignLoader(settings.Manager, loader, group);
        AssetDatabase.SaveAssets();
        var loaders = string.Join(", ", settings.Manager.activeLoaders.Select(l => l.GetType().FullName));
        Debug.Log($"ARLAB xr: {ten}: gan {loader} = {ok}; loader [{loaders}], InitManagerOnStart = {settings.InitManagerOnStart}");
        if (!settings.Manager.activeLoaders.Any(l => l.GetType().FullName == loader))
            throw new Exception($"Chua gan duoc {loader} cho {ten}");
    }

    // ARCore bắt buộc (app cần AR), nhưng Depth chỉ là tuỳ chọn để máy không có Depth vẫn cài được.
    static void ConfigureARCoreRequirements()
    {
        var t = Type.GetType("UnityEditor.XR.ARCore.ARCoreSettings, Unity.XR.ARCore.Editor");
        var get = t?.GetMethod("GetOrCreateSettings", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var settings = get?.Invoke(null, null) as Object;
        var depth = t?.GetProperty("depth", BindingFlags.Instance | BindingFlags.Public);
        if (settings == null || depth == null)
        {
            Debug.LogWarning("ARLAB arcore: khong tim thay ARCoreSettings.depth, giu mac dinh");
            return;
        }
        depth.SetValue(settings, Enum.Parse(depth.PropertyType, "Optional"));
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"ARLAB arcore: depth = {depth.GetValue(settings)}");
    }

    // ------------------------------------------------------------------ URP: nền camera AR
    static void ConfigureUrp()
    {
        // Kính khúc xạ cần Opaque Texture; bóng của vật cao ~15 cm cần khoảng cách bóng ngắn để đủ nét.
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path));
            so.FindProperty("m_RequireOpaqueTexture").boolValue = true;
            so.FindProperty("m_ShadowDistance").floatValue = 3f;
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"ARLAB urp: {path}: opaque texture, bong 3 m, bong mem");
        }
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;   // không sửa tài nguyên nằm trong gói
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var soR = new SerializedObject(data);   // mặt nhận bóng ARLab/BongDo là vật trong suốt
            soR.FindProperty("m_ShadowTransparentReceive").boolValue = true;
            soR.ApplyModifiedPropertiesWithoutUndo();
            if (data == null || data.rendererFeatures.Any(f => f is ARBackgroundRendererFeature)) continue;

            var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            feature.name = "ARBackgroundRendererFeature";
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var so = new SerializedObject(data);
            var feats = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            feats.arraySize++;
            feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            Debug.Log($"ARLAB urp: them ARBackgroundRendererFeature vao {path}");
        }
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ Gói thí nghiệm
    static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));

    // shortcut: đóng gói sẵn vào StreamingAssets trong lúc chưa có Firebase (GĐ 5 bỏ bước chép này).
    // Chép experiment.json + media/thumb.png + model/scene.glb của mọi gói, sinh catalog.json theo docs/05 mục 5.
    // Bản chép scene.glb vào Assets/Models chỉ để lấy biến thể shader glTFast (xem KeepGltfShaderVariants).
    static void CopyPackages()
    {
        string src = Path.Combine(RepoRoot, "experiments");
        string dst = Path.GetFullPath(StreamingDir);
        if (Directory.Exists(dst)) Directory.Delete(dst, true);
        var muc = new System.Collections.Generic.List<string>();
        foreach (var dir in Directory.GetDirectories(src).OrderBy(d => d))
        {
            string json = Path.Combine(dir, "experiment.json");
            if (!File.Exists(json)) continue;
            string id = Path.GetFileName(dir);
            var goi = JsonUtility.FromJson<GoiThiNghiem>(File.ReadAllText(json));
            foreach (var rel in new[] { "experiment.json", "media/thumb.png", goi.scene?.staticModel })
            {
                if (string.IsNullOrEmpty(rel) || !File.Exists(Path.Combine(dir, rel))) continue;
                string to = Path.Combine(dst, id, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                File.Copy(Path.Combine(dir, rel), to, true);
            }
            if (goi.CoMoHinh && File.Exists(Path.Combine(dir, goi.scene.staticModel)))
            {
                string model = Path.GetFullPath(Path.Combine(ModelsDir, id, "scene.glb"));
                Directory.CreateDirectory(Path.GetDirectoryName(model));
                File.Copy(Path.Combine(dir, goi.scene.staticModel), model, true);
            }
            muc.Add($"    {{ \"id\": \"{id}\", \"title\": \"{goi.title}\", \"version\": \"{goi.version}\", \"path\": \"{id}/\" }}");
        }
        File.WriteAllText(Path.Combine(dst, "catalog.json"),
            "{\n  \"catalogVersion\": 1,\n  \"updatedAt\": \"" + DateTime.Now.ToString("yyyy-MM-dd") + "\",\n  \"experiments\": [\n" +
            string.Join(",\n", muc) + "\n  ]\n}\n");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"ARLAB goi: chep {muc.Count} goi vao {StreamingDir}");
    }

    // glTFast tạo vật liệu lúc chạy bằng shader graph của nó; shader và biến thể phải có sẵn trong build.
    // Cách của tài liệu glTFast: để vật liệu mẫu (cùng shader, cùng keyword) trong thư mục Resources.
    // shortcut: chỉ phủ các tổ hợp vật liệu của gói đóng sẵn; khi tải gói mới từ mạng cần ShaderVariantCollection đầy đủ hơn.
    static void KeepGltfShaderVariants()
    {
        if (AssetDatabase.IsValidFolder(VariantDir)) AssetDatabase.DeleteAsset(VariantDir);
        EnsureFolder(VariantDir);
        int n = 0;
        foreach (var glb in Directory.GetFiles(ModelsDir, "scene.glb", SearchOption.AllDirectories))
        {
            string path = glb.Replace('\\', '/');
            string id = Path.GetFileName(Path.GetDirectoryName(path));
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                var mau = new Material(m);
                foreach (var tex in mau.GetTexturePropertyNames()) mau.SetTexture(tex, null);   // giữ keyword, bỏ ảnh cho nhẹ APK
                AssetDatabase.CreateAsset(mau, $"{VariantDir}/{id}_{m.name}.mat");
                n++;
            }
        }
        if (n == 0) throw new Exception("Khong lay duoc vat lieu glTFast nao tu " + ModelsDir);
        AssetDatabase.SaveAssets();
        Debug.Log($"ARLAB shader: {n} vat lieu mau trong {VariantDir}");
    }

    // ------------------------------------------------------------------ Giao diện (UI Toolkit)
    static PanelSettings CreatePanelSettings()
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (ps == null)
        {
            ps = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(ps, PanelSettingsPath);
        }
        ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiDir + "/ARLab.tss")
                             ?? throw new Exception("Khong nap duoc " + UiDir + "/ARLab.tss");
        ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(390, 844);   // khung mockup
        ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0f;
        EditorUtility.SetDirty(ps);
        AssetDatabase.SaveAssets();
        return ps;
    }

    // ------------------------------------------------------------------ Cảnh
    static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // Nạp asset SAU NewScene: mở cảnh mới (Single) gỡ các asset đang không được tham chiếu,
        // PanelSettings nạp trước đó bị huỷ → cảnh lưu m_PanelSettings = 0 → app không vẽ UI (màn đen, 16/11).
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiDir + "/ARLab.uxml")
                   ?? throw new Exception("Khong nap duoc " + UiDir + "/ARLab.uxml");
        var panel = CreatePanelSettings();

        var lightGO = new GameObject("Den chieu sang");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Camera thường để xem 3D: không phải camera của máy, không cần XR.
        var camGO = new GameObject("Camera Xem3D") { tag = "MainCamera" };
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x16, 0x1B, 0x23, 0xFF);
        var xem = camGO.AddComponent<XemMoHinh3D>();

        var gocMoHinh = new GameObject("MoHinh").transform;

        var uiGO = new GameObject("Giao dien");
        var doc = uiGO.AddComponent<UIDocument>();
        doc.panelSettings = panel;
        doc.visualTreeAsset = uxml;
        var app = uiGO.AddComponent<AppARLab>();
        app.giaoDien = doc;
        app.xem3D = xem;
        app.gocMoHinh = gocMoHinh;

        var phien = uiGO.AddComponent<PhienAR>();
        phien.mauKhungAR = CreateKhungARPrefab();
        phien.cameraXem = camGO;
        var manAR = uiGO.AddComponent<ManHinhAR>();
        manAR.phien = phien;
        manAR.vatLieuVong = CreateVongMaterial();
        manAR.den = light;
        app.shaderKinh = Shader.Find("ARLab/KinhKhucXa") ?? throw new Exception("Khong tim thay shader ARLab/KinhKhucXa");
        app.vatLieuBong = CreateBongMaterial();
        app.manAR = manAR;

        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        var names = string.Join(", ", scene.GetRootGameObjects().Select(g => g.name));
        Debug.Log($"ARLAB scene: {ScenePath} [{names}]");
        if (!File.ReadAllText(ScenePath).Contains("m_PanelSettings: {fileID: 11400000"))
            throw new Exception("Canh luu ra UIDocument khong co PanelSettings (app se man den)");
    }

    // ------------------------------------------------------------------ Tự kiểm tra (chạy nền)
    // Unity.exe -batchmode -projectPath unity/ARLab -executeMethod ARLabSetup.KiemTra -logFile work/logs/...
    // Chạy đúng lớp nạp gói của app: đọc catalog, ảnh, nạp GLB bằng glTFast. Tìm dòng ARLAB_KIEMTRA trong log.
    public static void KiemTra()
    {
        CopyPackages();
        ChayKiemTra();   // không thoát ở đây: Editor tiếp tục chạy để các tác vụ bất đồng bộ xong
    }

    static async void ChayKiemTra()
    {
        int loi = 0;
        void Sai(string s) { loi++; Debug.LogError("ARLAB_KIEMTRA sai: " + s); }
        try
        {
            var ds = await KhoGoi.NapDanhMuc();
            Debug.Log("ARLAB_KIEMTRA danh muc: " + string.Join(", ", ds.Select(g => $"{g.id} ({g.title}, mo hinh {g.CoMoHinh})")));
            if (ds.Count(g => g.CoMoHinh) < 2) Sai("can it nhat 2 goi co mo hinh");
            foreach (var g in ds)
            {
                var anh = await KhoGoi.NapAnh(g);
                if (g.CoMoHinh && anh == null) Sai(g.id + ": khong co anh thumb");
                if (!g.CoMoHinh) continue;

                var hop = new GameObject("KiemTra_" + g.id);
                var imp = await KhoGoi.NapMoHinh(g, hop.transform, new GLTFast.UninterruptedDeferAgent());
                var b = KhoGoi.HopBao(hop.transform);
                var nut = hop.GetComponentsInChildren<Transform>().Select(t => t.name).ToList();
                var bp = BangTenBoPhan.LietKe(hop.transform).Select(x => x.ten);
                Debug.Log($"ARLAB_KIEMTRA {g.id}: anh {anh?.width}x{anh?.height}, cao {b.size.y * 100f:0.0} cm, " +
                          $"{hop.GetComponentsInChildren<Renderer>().Length} renderer, bo phan [{string.Join("; ", bp)}]");
                if (b.size.y < 0.05f || b.size.y > 0.5f) Sai($"{g.id}: chieu cao {b.size.y} m bat thuong");
                if (g.id == "thau-kinh-f200" && (g.thauKinh == null || Mathf.Abs(g.thauKinh.f_m - 0.2f) > 1e-4f))
                    Sai("thau-kinh-f200: khong doc duoc f = 0,2 m tu extras cua nut ThauKinh");
                if (g.id == "thau-kinh-f200" && !(nut.Contains("DeKep") && nut.Contains("ThauKinh") && nut.Contains("TamQuang")))
                    Sai("thau-kinh-f200: thieu nut DeKep / ThauKinh / TamQuang");
                if (hop.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterials.Any(m => m == null || m.shader == null)))
                    Sai(g.id + ": co renderer thieu vat lieu");
                imp.Dispose();
                Object.DestroyImmediate(hop);
            }
        }
        catch (Exception e) { Sai(e.ToString()); }
        Debug.Log(loi == 0 ? "ARLAB_KIEMTRA OK" : $"ARLAB_KIEMTRA LOI {loi}");
        EditorApplication.Exit(loi == 0 ? 0 : 1);
    }

    // ------------------------------------------------------------------ Khung AR (prefab, PhienAR tạo khi vào màn AR)
    const string KhungARPath = "Assets/Prefabs/KhungAR.prefab";
    const string VongMatPath = "Assets/Materials/VongNgam.mat";

    static GameObject CreateKhungARPrefab()
    {
        EnsureFolder("Assets/Prefabs");
        var root = new GameObject("KhungAR");

        var sessionGO = new GameObject("AR Session");
        sessionGO.transform.SetParent(root.transform, false);
        sessionGO.AddComponent<ARSession>();
        sessionGO.AddComponent<ARInputManager>();

        var originGO = new GameObject("XR Origin");
        originGO.transform.SetParent(root.transform, false);
        var origin = originGO.AddComponent<XROrigin>();
        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(originGO.transform, false);
        var camGO = new GameObject("AR Camera") { tag = "MainCamera" };
        camGO.transform.SetParent(offset.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 30f;
        camGO.AddComponent<ARCameraManager>();
        camGO.AddComponent<ARCameraBackground>();
        var tpd = camGO.AddComponent<InputTrackedPoseDriver>();
        var pos = new InputAction("Vi tri", InputActionType.Value, expectedControlType: "Vector3");
        pos.AddBinding("<XRHMD>/centerEyePosition");
        pos.AddBinding("<HandheldARInputDevice>/devicePosition");
        var rot = new InputAction("Huong", InputActionType.Value, expectedControlType: "Quaternion");
        rot.AddBinding("<XRHMD>/centerEyeRotation");
        rot.AddBinding("<HandheldARInputDevice>/deviceRotation");
        tpd.positionInput = new InputActionProperty(pos);
        tpd.rotationInput = new InputActionProperty(rot);
        origin.Camera = cam;
        origin.CameraFloorOffsetObject = offset;

        var planeManager = originGO.AddComponent<ARPlaneManager>();
        planeManager.planePrefab = CreatePlanePrefab();
        planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        originGO.AddComponent<ARRaycastManager>();
        originGO.AddComponent<ARAnchorManager>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, KhungARPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static Material CreateBongMaterial()
    {
        const string path = "Assets/Materials/BongDo.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("ARLab/BongDo") ?? throw new Exception("Khong tim thay shader ARLab/BongDo"));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material CreateVongMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(VongMatPath);
        if (mat != null) return mat;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? throw new Exception("Khong tim thay shader URP Unlit");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", new Color32(0x4C, 0xC3, 0xFF, 0xFF));   // xanh lơ của mockup
        AssetDatabase.CreateAsset(mat, VongMatPath);
        return mat;
    }

    // ------------------------------------------------------------------ Tự kiểm tra AR (Play Mode + XR Simulation)
    // Unity.exe -batchmode -projectPath unity/ARLab -executeMethod ARLabSetup.KiemTraAR -logFile work/logs/...
    // Mở cảnh, vào Play Mode, kiểm tra: mở app không nạp XR; vào màn AR thì nạp, rời thì gỡ (2 lượt).
    const string CoKiemTraAR = "ARLAB_KIEMTRA_AR";

    public static void KiemTraAR()
    {
        Setup();
        EditorSceneManager.OpenScene(ScenePath);
        SessionState.SetBool(CoKiemTraAR, true);
        EditorApplication.playModeStateChanged += KhiVaoPlay;   // dự án tắt Reload Domain: không có nạp lại
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]   // phòng khi bật lại Reload Domain: đăng ký lại sau lúc nạp lại mã
    static void TheoDoiKiemTraAR()
    {
        if (SessionState.GetBool(CoKiemTraAR, false)) EditorApplication.playModeStateChanged += KhiVaoPlay;
    }

    static void KhiVaoPlay(PlayModeStateChange s)
    {
        if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(CoKiemTraAR, false)) return;
        SessionState.SetBool(CoKiemTraAR, false);
        EditorApplication.playModeStateChanged -= KhiVaoPlay;
        ChayKiemTraAR();
    }

    static async void ChayKiemTraAR()
    {
        int loi = 0;
        void Sai(string s) { loi++; Debug.LogError("ARLAB_KIEMTRA_AR sai: " + s); }
        static XRManagerSettings QuanLy() => XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        try
        {
            await Awaitable.WaitForSecondsAsync(2f);
            var m = QuanLy();
            if (m == null) throw new Exception("khong co XRGeneralSettings cho Editor (Standalone)");
            if (m.activeLoader != null) Sai("mo app da nap XR: " + m.activeLoader.name);
            if (Object.FindAnyObjectByType<ARSession>() != null) Sai("mo app da co ARSession trong canh");
            Debug.Log("ARLAB_KIEMTRA_AR mo app: XR chua nap, khong co ARSession");

            var man = Object.FindAnyObjectByType<ManHinhAR>();
            var phien = Object.FindAnyObjectByType<PhienAR>();
            for (int lan = 1; lan <= 2; lan++)
            {
                var gia = new GameObject("MoHinhGia").transform;
                man.Mo(new GoiThiNghiem { id = "thu", title = "Thử" }, gia, 0.151f);
                float t0 = Time.realtimeSinceStartup;
                while (!phien.DangChay && phien.Loi == null && Time.realtimeSinceStartup - t0 < 30f)
                    await Awaitable.NextFrameAsync();
                if (!phien.DangChay) { Sai($"lan {lan}: khong bat duoc AR ({phien.Loi ?? "qua 30 s"})"); break; }

                await Awaitable.WaitForSecondsAsync(5f);
                int matPhang = Object.FindAnyObjectByType<UnityEngine.XR.ARFoundation.ARPlaneManager>()?.trackables.count ?? -1;
                Debug.Log($"ARLAB_KIEMTRA_AR lan {lan}: loader {QuanLy().activeLoader?.name}, ARSession.state {ARSession.state}, " +
                          $"{matPhang} mat phang, camera xem 3D {(phien.cameraXem.activeSelf ? "BAT" : "tat")}");
                if (phien.cameraXem.activeSelf) Sai("camera xem 3D van bat khi o man AR");

                man.Dong();
                await Awaitable.WaitForSecondsAsync(1f);
                if (QuanLy().activeLoader != null) Sai($"lan {lan}: roi man AR nhung XR van nap");
                if (Object.FindAnyObjectByType<ARSession>() != null) Sai($"lan {lan}: roi man AR nhung van con ARSession");
                if (!phien.cameraXem.activeSelf) Sai($"lan {lan}: camera xem 3D khong bat lai");
                Debug.Log($"ARLAB_KIEMTRA_AR lan {lan}: roi man AR -> XR da go");
                Object.Destroy(gia.gameObject);
            }
        }
        catch (Exception e) { Sai(e.ToString()); }
        Debug.Log(loi == 0 ? "ARLAB_KIEMTRA_AR OK" : $"ARLAB_KIEMTRA_AR LOI {loi}");
        EditorApplication.Exit(loi == 0 ? 0 : 1);
    }

    // ------------------------------------------------------------------ Chụp giao diện (Play Mode → PNG trong work/logs)
    // Unity.exe -batchmode -projectPath unity/ARLab -executeMethod ARLabSetup.ChupGiaoDien -logFile work/logs/...
    const string CoChup = "ARLAB_CHUP";

    public static void ChupGiaoDien()
    {
        Setup();
        EditorSceneManager.OpenScene(ScenePath);
        SessionState.SetBool(CoChup, true);
        EditorApplication.playModeStateChanged += KhiVaoPlayChup;
        EditorApplication.EnterPlaymode();
    }

    static void KhiVaoPlayChup(PlayModeStateChange s)
    {
        if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(CoChup, false)) return;
        SessionState.SetBool(CoChup, false);
        EditorApplication.playModeStateChanged -= KhiVaoPlayChup;
        ChayChup();
    }

    static async void ChayChup()
    {
        int code = 1;
        var doc = Object.FindAnyObjectByType<UIDocument>();
        var rt = new RenderTexture(780, 1688, 24);
        try
        {
            var app = Object.FindAnyObjectByType<AppARLab>();
            var cam = Object.FindAnyObjectByType<XemMoHinh3D>().GetComponent<Camera>();
            cam.targetTexture = rt;
            doc.panelSettings.targetTexture = rt;
            await Awaitable.WaitForSecondsAsync(4f);
            MoTaGiaoDien(doc.rootVisualElement, "thu vien");
            LuuAnh(rt, "18-chup-thu-vien.png");

            var goi = (System.Collections.IList)typeof(AppARLab).GetField("goi", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(app);
            if (goi.Count > 0)
            {
                typeof(AppARLab).GetMethod("MoChiTiet", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(app, new[] { goi[0] });
                await Awaitable.WaitForSecondsAsync(5f);
                MoTaGiaoDien(doc.rootVisualElement, "chi tiet");
                LuuAnh(rt, "18-chup-chi-tiet.png");
            }

            // Thấu kính: màn chi tiết, rồi màn AR (XR Simulation), mô hình đặt trước camera
            object tk = null;
            foreach (var x in goi) if (((GoiThiNghiem)x).id == "thau-kinh-f200") tk = x;
            if (tk != null)
            {
                const BindingFlags an = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(AppARLab).GetMethod("MoChiTiet", an).Invoke(app, new[] { tk });
                await Awaitable.WaitForSecondsAsync(5f);
                LuuAnh(rt, "18-chup-thau-kinh.png");
                // Kiểm chứng khúc xạ: tấm hình mẫu sau kính (trái đỏ, phải xanh dương, dải trên xanh lá)
                var camXem = Object.FindAnyObjectByType<XemMoHinh3D>().transform;
                var tam = GameObject.Find("TamQuang").transform;
                var mau = TaoHinhMau(camXem, tam);
                // d < f: ảnh cùng chiều (đỏ bên trái quang tâm); d > f: ảnh ngược chiều (đỏ sang phải)
                foreach (var (d, ten, doBenTrai) in new[] { (0.1f, "18-chup-kinh-d010.png", true), (0.6f, "18-chup-kinh-d060.png", false) })
                {
                    mau.position = tam.position + (tam.position - camXem.position).normalized * d;
                    ThauKinhKhucXa.KhoangCachNen = d;
                    await Awaitable.WaitForSecondsAsync(0.5f);
                    var anh = LuuAnh(rt, ten);
                    Vector3 c = camXem.GetComponent<Camera>().WorldToScreenPoint(tam.position);
                    Color trai = anh.GetPixel((int)c.x - 12, (int)c.y), phai = anh.GetPixel((int)c.x + 12, (int)c.y);
                    bool dung = doBenTrai ? trai.r > trai.b && phai.b > phai.r : trai.b > trai.r && phai.r > phai.b;
                    Debug.Log($"ARLAB_CHUP khuc xa d = {d} m: trai {trai}, phai {phai} -> {(dung ? "DUNG" : "SAI")} ({(doBenTrai ? "cung chieu" : "nguoc chieu")})");
                    if (!dung) throw new Exception($"kinh khuc xa sai chieu anh o d = {d} m");
                }
                Object.Destroy(mau.gameObject);
                ThauKinhKhucXa.KhoangCachNen = ThauKinhKhucXa.NenMacDinh;

                typeof(AppARLab).GetMethod("VaoAR", an).Invoke(app, null);
                var phien = Object.FindAnyObjectByType<PhienAR>();
                float t0 = Time.realtimeSinceStartup;
                while (!phien.DangChay && Time.realtimeSinceStartup - t0 < 30f) await Awaitable.NextFrameAsync();
                if (!phien.DangChay) throw new Exception("khong bat duoc AR: " + phien.Loi);
                await Awaitable.WaitForSecondsAsync(3f);
                Camera.main.targetTexture = rt;
                var man = Object.FindAnyObjectByType<ManHinhAR>();
                man.DatDeKiemTra(0.35f, 0.12f);
                await Awaitable.WaitForSecondsAsync(3f);
                Debug.Log($"ARLAB_CHUP ar: kinh = {Object.FindAnyObjectByType<ThauKinhKhucXa>() != null}, nen sau kinh = {ThauKinhKhucXa.KhoangCachNen} m");
                LuuAnh(rt, "18-chup-ar-thau-kinh.png");
                typeof(ManHinhAR).GetMethod("TachRoi", an).Invoke(man, null);
                typeof(ManHinhAR).GetMethod("BatTatChuThich", an).Invoke(man, null);
                await Awaitable.WaitForSecondsAsync(1.5f);
                LuuAnh(rt, "18-chup-ar-tach-roi.png");
                Debug.Log("ARLAB_CHUP nhan: " + string.Join("; ", doc.rootVisualElement.Query<Label>(className: "nhan-bo-phan").ToList()
                    .Select(l => $"{l.text}@{l.resolvedStyle.left:0},{l.resolvedStyle.top:0} {l.resolvedStyle.width:0}x{l.resolvedStyle.height:0} hien={l.resolvedStyle.display}")));
                man.Dong();
            }
            code = 0;
        }
        catch (Exception e) { Debug.LogError("ARLAB_CHUP loi: " + e); }
        finally
        {
            if (doc != null && doc.panelSettings != null) doc.panelSettings.targetTexture = null;   // không để lại thay đổi trên asset
        }
        Debug.Log("ARLAB_CHUP xong " + code);
        EditorApplication.Exit(code);
    }

    static Transform TaoHinhMau(Transform cam, Transform tam)
    {
        var tex = new Texture2D(64, 64) { filterMode = FilterMode.Point };
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                tex.SetPixel(x, y, y > 44 ? Color.green : x < 32 ? Color.red : Color.blue);
        tex.Apply();
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "HinhMauKiemTra";
        var vl = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        vl.SetTexture("_BaseMap", tex);
        q.GetComponent<Renderer>().sharedMaterial = vl;
        q.transform.rotation = Quaternion.LookRotation(tam.position - cam.position);   // mặt hình quay về camera
        q.transform.localScale = Vector3.one * 0.12f;
        return q.transform;
    }

    static void MoTaGiaoDien(VisualElement goc, string ten)
    {
        var chu = goc.Query<Label>().ToList().Where(l => l.resolvedStyle.display == DisplayStyle.Flex && !string.IsNullOrEmpty(l.text))
                     .Take(12).Select(l => $"'{l.text}' {l.worldBound.width:0}x{l.worldBound.height:0} font={l.resolvedStyle.unityFontDefinition.font?.name ?? l.resolvedStyle.unityFontDefinition.fontAsset?.name ?? "NULL"} mau={l.resolvedStyle.color}");
        Debug.Log($"ARLAB_CHUP {ten}: goc {goc.worldBound}, so phan tu {goc.Query<VisualElement>().ToList().Count}\n  " + string.Join("\n  ", chu));
    }

    static Texture2D LuuAnh(RenderTexture rt, string tenFile)
    {
        var cu = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = cu;
        string path = Path.Combine(RepoRoot, "work", "logs", tenFile);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log("ARLAB_CHUP luu " + path);
        return tex;
    }

    static GameObject CreatePlanePrefab()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Prefabs");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(PlaneMatPath);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new Exception("Khong tim thay shader URP Unlit");
            mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(0.3f, 0.75f, 1f, 0.25f));
            AssetDatabase.CreateAsset(mat, PlaneMatPath);
        }

        var go = new GameObject("ARPlaneHienThi");
        go.AddComponent<ARPlane>();
        go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        go.AddComponent<ARPlaneMeshVisualizer>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlanePrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------ Build
    static int BuildApk()
    {
        string apk = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../work/build/ARLab.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(apk));
        EditorUserBuildSettings.buildAppBundle = false;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = apk,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        });
        var s = report.summary;
        Debug.Log($"ARLAB_BUILD {s.result} | {s.totalSize / 1048576f:0.0} MB | loi {s.totalErrors} | canh bao {s.totalWarnings} | {s.totalTime} | {apk}");
        return s.result == BuildResult.Succeeded ? 0 : 1;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
