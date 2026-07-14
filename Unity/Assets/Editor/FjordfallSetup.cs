#if UNITY_EDITOR
using System.IO;
using Fjordfall;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fjordfall.EditorTools
{
    [InitializeOnLoad]
    public static class FjordfallSetup
    {
        private const string ScenePath = "Assets/Scenes/Fjordfall.unity";

        static FjordfallSetup()
        {
            EditorApplication.delayCall += AutoPrepareOnce;
        }

        private static void AutoPrepareOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(ScenePath)) PrepareProject();
        }

        [MenuItem("Fjordfall/1. 프로젝트 준비", priority = 1)]
        public static void PrepareProject()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Fjordfall Game");
            root.AddComponent<FjordfallGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "gpt0081";
            PlayerSettings.productName = "Fjordfall";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.gpt0081.fjordfall");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            Selection.activeGameObject = root;
            AssetDatabase.SaveAssets();
            Debug.Log("[Fjordfall] 프로젝트 준비 완료. 중앙 상단의 ▶ 버튼으로 실행하십시오.");
        }

        [MenuItem("Fjordfall/2. Android APK 빌드", priority = 2)]
        public static void BuildAndroidApk()
        {
            if (!File.Exists(ScenePath)) PrepareProject();
            Directory.CreateDirectory("Builds");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Fjordfall-Unity3D.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Fjordfall] Android build result: {report.summary.result} / {report.summary.outputPath}");
        }

        [MenuItem("Fjordfall/3. 플레이 테스트 씬 열기", priority = 3)]
        public static void OpenPlayScene()
        {
            if (!File.Exists(ScenePath)) PrepareProject();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
