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
    const string ModelPath = "Assets/Models/den-soi-dot/scene.glb";
    const string ModelTitle = "Mô hình cấu tạo đèn sợi đốt";
    const string PlanePrefabPath = "Assets/Prefabs/ARPlaneHienThi.prefab";
    const string PlaneMatPath = "Assets/Materials/MatPhangAR.mat";
    const string AppId = "vn.arexperiment.arlab";

    [MenuItem("AR Lab/1. Thiết lập dự án và dựng cảnh")]
    public static void Setup()
    {
        ConfigurePlayer();
        ConfigureXR();
        ConfigureUrp();
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
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.SetApplicationIdentifier(android, AppId);
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if ((int)PlayerSettings.Android.minSdkVersion < 26)
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
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

        var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            settings.name = "Android Settings";
            AssetDatabase.AddObjectToAsset(settings, assetPath);
            perTarget.SetSettingsForBuildTarget(BuildTargetGroup.Android, settings);
        }
        if (settings.Manager == null)
        {
            var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            manager.name = "Android Providers";
            AssetDatabase.AddObjectToAsset(manager, assetPath);
            settings.Manager = manager;
        }
        settings.InitManagerOnStart = true;
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(perTarget);
        AssetDatabase.SaveAssets();

        bool ok = XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android);
        AssetDatabase.SaveAssets();
        var loaders = string.Join(", ", settings.Manager.activeLoaders.Select(l => l.GetType().Name));
        Debug.Log($"ARLAB xr: gan ARCoreLoader = {ok}; loader Android: [{loaders}]");
        if (!settings.Manager.activeLoaders.Any(l => l.GetType().Name == "ARCoreLoader"))
            throw new Exception("Chua gan duoc ARCoreLoader cho Android");
        ConfigureARCoreRequirements();
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
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;   // không sửa tài nguyên nằm trong gói
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
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

    // ------------------------------------------------------------------ Cảnh
    static void BuildScene()
    {
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelPrefab == null) throw new Exception("Khong nap duoc mo hinh " + ModelPath + " (glTFast da cai chua?)");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightGO = new GameObject("Den chieu sang");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var sessionGO = new GameObject("AR Session");
        sessionGO.AddComponent<ARSession>();
        sessionGO.AddComponent<ARInputManager>();

        var originGO = new GameObject("XR Origin");
        var origin = originGO.AddComponent<XROrigin>();
        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(originGO.transform, false);
        var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
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
        var raycast = originGO.AddComponent<ARRaycastManager>();
        originGO.AddComponent<ARAnchorManager>();

        var appGO = new GameObject("AR Lab");
        var placer = appGO.AddComponent<ARLabPlacer>();
        placer.raycastManager = raycast;
        placer.planeManager = planeManager;
        placer.modelPrefab = modelPrefab;
        placer.modelTitle = ModelTitle;

        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        var names = string.Join(", ", scene.GetRootGameObjects().Select(g => g.name));
        Debug.Log($"ARLAB scene: {ScenePath} [{names}]; mo hinh = {modelPrefab.name}, " +
                  $"{modelPrefab.GetComponentsInChildren<MeshFilter>(true).Length} mesh");
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
