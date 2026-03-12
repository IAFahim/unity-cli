using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityCliConnector.Tools
{
    [UnityCliTool(Description = "Force reserialize Unity assets. No params = entire project.")]
    public static class ReserializeAssets
    {
        public class Parameters
        {
            [ToolParameter("Single asset path to reserialize")]
            public string Path { get; set; }

            [ToolParameter("Multiple asset paths to reserialize")]
            public string[] Paths { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            var pathToken = parameters["path"];
            var pathsToken = parameters["paths"];

            string[] paths;
            if (pathsToken != null && pathsToken.Type == JTokenType.Array)
                paths = pathsToken.ToObject<string[]>();
            else if (pathToken != null)
                paths = new[] { pathToken.ToString() };
            else
                paths = null;

            if (paths == null || paths.Length == 0)
            {
                AssetDatabase.ForceReserializeAssets();
                Debug.Log("[UnityCliConnector] ForceReserializeAssets: entire project");
                return new SuccessResponse("Reserialized entire project");
            }

            AssetDatabase.ForceReserializeAssets(paths);
            Debug.Log($"[UnityCliConnector] ForceReserializeAssets: {string.Join(", ", paths)}");
            return new SuccessResponse($"Reserialized {paths.Length} asset(s)", new { paths });
        }
    }
}
