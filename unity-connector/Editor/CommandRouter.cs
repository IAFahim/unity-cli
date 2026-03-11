using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityCliConnector
{
    /// <summary>
    /// Routes incoming command requests to the appropriate tool handler.
    /// All requests are serialized through a single queue to prevent
    /// race conditions when multiple CLI agents access the same Unity instance.
    /// </summary>
    public static class CommandRouter
    {
        static readonly SemaphoreSlim s_Lock = new(1, 1);

        public static async Task<object> Dispatch(string command, JObject parameters)
        {
            await s_Lock.WaitAsync();
            try
            {
                return await DispatchInternal(command, parameters);
            }
            finally
            {
                s_Lock.Release();
            }
        }

        static async Task<object> DispatchInternal(string command, JObject parameters)
        {
            if (command == "list_tools")
            {
                return new SuccessResponse("Available tools", ToolDiscovery.GetToolSchemas());
            }

            if (command == "tool_help")
            {
                var name = parameters?["name"]?.ToString();
                if (name == null) return new ErrorResponse("Missing 'name' parameter");
                if (ToolDiscovery.Tools.TryGetValue(name, out var info) == false)
                    return new ErrorResponse($"Unknown tool: {name}");
                return new SuccessResponse(info.Description, new
                {
                    name = info.Name,
                    description = info.Description,
                    group = info.Group,
                    parameters = ToolDiscovery.GetParameterSchema(info.ParametersType),
                });
            }

            if (ToolDiscovery.Tools.TryGetValue(command, out var tool) == false)
            {
                return new
                {
                    success = false,
                    error = $"Unknown command: {command}",
                    registered_tools = ToolDiscovery.Tools.Keys.ToArray(),
                    tool_count = ToolDiscovery.Tools.Count,
                };
            }

            return await InvokeHandler(command, tool, parameters);
        }

        static async Task<object> InvokeHandler(string command, ToolDiscovery.ToolInfo tool, JObject parameters)
        {
            try
            {
                var result = tool.Handler.Invoke(null, new object[] { parameters ?? new JObject() });

                if (result is Task<object> asyncTask)
                {
                    return await asyncTask;
                }

                if (result is Task task)
                {
                    await task;
                    return new SuccessResponse($"{command} completed");
                }

                return result ?? new SuccessResponse($"{command} completed");
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Debug.LogException(inner);
                return new ErrorResponse($"{command} failed: {inner.Message}");
            }
        }
    }
}
