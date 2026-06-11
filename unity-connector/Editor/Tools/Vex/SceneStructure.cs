using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityCliConnector.Tools.Vex
{
    /// <summary>
    /// Reports the hierarchy of the active (parent) scene plus the contents of any referenced
    /// SubScenes. Read-only: any SubScene it had to open additively is closed again, and the
    /// editor is left on the original parent scene.
    ///
    /// SubScenes are detected by component type name and resolved via SerializedObject reflection,
    /// so this tool carries no hard dependency on Unity.Scenes / Unity.Entities.
    /// </summary>
    [UnityCliTool(
        Name = "scene_structure",
        Group = "vex",
        Description = "Dump the active scene hierarchy plus the contents of referenced SubScenes (components, positions, nesting). Read-only; restores the original scene setup.")]
    public static class SceneStructure
    {
        public class Parameters
        {
            [ToolParameter("Max hierarchy depth to recurse. -1 (default) = unlimited.")]
            public int MaxDepth { get; set; }

            [ToolParameter("Expand referenced SubScenes by opening them additively. Default true.")]
            public bool Subscenes { get; set; }

            [ToolParameter("Include the component type list on each GameObject. Default true.")]
            public bool Components { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            var p = new ToolParams(@params);
            int maxDepth = p.GetInt("max_depth", -1) ?? -1;
            bool expandSub = p.GetBool("subscenes", true);
            bool includeComps = p.GetBool("components", true);

            var active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid())
                return new ErrorResponse("No active scene.");

            string parentPath = active.path;
            var openedByUs = new List<Scene>();

            try
            {
                var subScenePaths = new List<string>();
                var parentDump = DumpScene(active, maxDepth, includeComps, subScenePaths);

                var subSceneDumps = new List<object>();
                if (expandSub)
                {
                    foreach (var subPath in subScenePaths.Where(s => !string.IsNullOrEmpty(s)).Distinct())
                    {
                        var existing = EditorSceneManager.GetSceneByPath(subPath);
                        Scene sub;
                        if (existing.IsValid() && existing.isLoaded)
                        {
                            // Already open for editing — read it in place, don't disturb it.
                            sub = existing;
                        }
                        else
                        {
                            sub = EditorSceneManager.OpenScene(subPath, OpenSceneMode.Additive);
                            openedByUs.Add(sub);
                        }
                        subSceneDumps.Add(DumpScene(sub, maxDepth, includeComps, null));
                    }
                }

                var data = new
                {
                    parentScene = parentDump,
                    subScenes = subSceneDumps,
                };
                return new SuccessResponse(
                    $"Scene structure for '{active.name}' (+{subSceneDumps.Count} subscene(s)).", data);
            }
            finally
            {
                // Close only what we opened; leave the editor on the original parent scene.
                foreach (var s in openedByUs)
                {
                    if (s.IsValid() && s.isLoaded)
                        EditorSceneManager.CloseScene(s, true);
                }

                var current = EditorSceneManager.GetActiveScene();
                if (!string.IsNullOrEmpty(parentPath) && current.path != parentPath)
                    EditorSceneManager.OpenScene(parentPath, OpenSceneMode.Single);
            }
        }

        private static object DumpScene(Scene scene, int maxDepth, bool includeComps, List<string> subScenePathSink)
        {
            var rootDumps = new List<object>();
            foreach (var go in scene.GetRootGameObjects())
                rootDumps.Add(DumpGo(go.transform, 0, maxDepth, includeComps, subScenePathSink));

            return new
            {
                name = scene.name,
                path = scene.path,
                isLoaded = scene.isLoaded,
                rootCount = scene.rootCount,
                roots = rootDumps,
            };
        }

        private static object DumpGo(Transform t, int depth, int maxDepth, bool includeComps, List<string> subScenePathSink)
        {
            var go = t.gameObject;

            List<string> compNames = includeComps ? new List<string>() : null;
            string subSceneRef = null;

            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue; // missing script
                var typeName = c.GetType().Name;
                compNames?.Add(typeName);
                if (typeName == "SubScene")
                {
                    subSceneRef = ResolveSubScenePath(c);
                    subScenePathSink?.Add(subSceneRef);
                }
            }

            List<object> childDumps = null;
            bool descend = maxDepth < 0 || depth < maxDepth;
            if (descend && t.childCount > 0)
            {
                childDumps = new List<object>();
                for (int i = 0; i < t.childCount; i++)
                    childDumps.Add(DumpGo(t.GetChild(i), depth + 1, maxDepth, includeComps, subScenePathSink));
            }

            var pos = t.position;
            return new
            {
                name = go.name,
                active = go.activeInHierarchy,
                pos = $"{pos.x:F2},{pos.y:F2},{pos.z:F2}",
                components = compNames,
                subSceneAsset = subSceneRef,
                childCount = t.childCount,
                children = childDumps,
            };
        }

        // Find the SceneAsset referenced by a SubScene component without referencing Unity.Scenes.
        private static string ResolveSubScenePath(Component subSceneComponent)
        {
            var so = new SerializedObject(subSceneComponent);
            var it = so.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference &&
                    it.objectReferenceValue != null &&
                    it.objectReferenceValue.GetType().Name == "SceneAsset")
                {
                    return AssetDatabase.GetAssetPath(it.objectReferenceValue);
                }
            }
            return null;
        }
    }
}
