using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ascension.Editor
{
    // Independent Editor-only entry point. Does not generate or modify gameplay assets.
    public static class StandaloneBuild
    {
        const string ScenePath = "Assets/Scenes/Ascension.unity";
        static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;

        [MenuItem("Ascension/Open Game Scene")]
        public static void OpenScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Ascension/Build Windows Release")]
        public static void BuildWindows()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                ValidateScene();
                string output = Path.Combine(Root, "Builds", "Windows", "Ascension.exe");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { ScenePath }, locationPathName = output,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
                });
                var s = build.summary;
                Write("build.json", new Result {
                    success = s.result == BuildResult.Succeeded, result = s.result.ToString(),
                    output = output, unityVersion = Application.unityVersion,
                    errors = s.totalErrors, warnings = s.totalWarnings,
                    seconds = s.totalTime.TotalSeconds, bytes = s.totalSize
                });
                if (s.result != BuildResult.Succeeded) throw new Exception("Build failed: " + s.result);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static void ValidateScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            int missing = all.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if (missing != 0) throw new Exception("Missing scene scripts: " + missing);
            if (UnityEngine.Object.FindObjectsOfType<AscensionRuntime>().Length != 1 ||
                UnityEngine.Object.FindObjectsOfType<Ascension.Presentation.AscensionView>().Length != 1)
                throw new Exception("Expected exactly one Ascension runtime and view");
            string[] dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            if (dependencies.Any(p => !p.StartsWith("Assets/") && !p.StartsWith("Packages/")))
                throw new Exception("Scene dependency outside project");
            Write("scene.json", new SceneResult { scene = ScenePath, missingScripts = missing,
                gameObjects = all.Length, dependencies = dependencies, compiled = true });
            Debug.Log("[Ascension Independence] Scene and compilation validated: " + Application.dataPath);
        }

        static void Write(string name, object value)
        {
            string dir = Path.Combine(Root, "Docs", "Evidence");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, name), JsonUtility.ToJson(value, true));
        }
        [Serializable] class SceneResult { public string scene; public bool compiled; public int missingScripts, gameObjects; public string[] dependencies; }
        [Serializable] class Result { public bool success; public string result, output, unityVersion; public int errors, warnings; public double seconds; public ulong bytes; }
    }
}
