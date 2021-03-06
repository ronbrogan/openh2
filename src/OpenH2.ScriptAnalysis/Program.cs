﻿using OpenH2.Core.Factories;
using OpenH2.Core.Maps.Vista;
using OpenH2.Core.Scripting.Generation;
using OpenH2.Core.Scripting.LowLevel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OpenH2.ScriptAnalysis
{
    public class Program
    {
        private static ConcurrentDictionary<string, (HashSet<ushort>, HashSet<string>, int)> MethodInfos = new();

        static void Main(string[] args)
        {
            var outRoot = $@"D:\h2scratch\scripts";
            Directory.CreateDirectory(outRoot);

            var loader = new ScriptLoader(outRoot);
            var maps = Directory.GetFiles(@"D:\H2vMaps", "*.map");

            foreach (var map in maps)
            {
                if (map.Contains("0") == false)
                {
                    //continue;
                }

                var factory = new MapFactory(Path.GetDirectoryName(map));
                var h2map = factory.Load(Path.GetFileName(map));

                if (h2map is not H2vMap scene)
                {
                    throw new NotSupportedException("Only Vista maps are supported");
                }

                loader.Load(scene.Scenario);

                var scenarioParts = scene.Scenario.Name.Split('\\', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .ToArray();

                var debugRoot = $@"{outRoot}\{scenarioParts.Last()}";
                Directory.CreateDirectory(debugRoot);

                for (int i = 0; i < scene.Scenario.ScriptMethods.Length; i++)
                {
                    var script = scene.Scenario.ScriptMethods[i];
                    var text = ScriptProcessor.GetScriptTree(scene.Scenario, script, i);
                    CollectBuiltins(text);
                    var debugTree = text.ToString(verbose: true);
                    File.WriteAllText(Path.Combine(debugRoot, script.Description + ".tree"), debugTree);
                }
            }

            File.WriteAllText(Path.Combine(outRoot, "MethodInfo.txt"), GenerateBuiltinInfo());
        }

        private static void CollectBuiltins(ScriptTreeNode root)
        {
            var nodes = new Stack<ScriptTreeNode>();

            nodes.Push(root);

            while (nodes.TryPop(out var node))
            {
                // Ignore per-scenario script method invocations
                if (node.Original.NodeType == Core.Scripting.NodeType.ScriptInvocation)
                {
                    continue;
                }

                if (node.Original.NodeType == Core.Scripting.NodeType.Expression &&
                    node.Original.DataType == Core.Scripting.ScriptDataType.MethodOrOperator)
                {
                    MethodInfos.AddOrUpdate(node.Value as string, 
                        k => (
                            new HashSet<ushort>() { node.Original.OperationId },
                            new HashSet<string>(node.Children.Select(c => c.DataType.ToString())),
                            node.Children.Count), 
                        (k, h) =>
                    {
                        h.Item1.Add(node.Original.OperationId);
                        foreach(var c in node.Children)
                        {
                            h.Item2.Add(c.DataType.ToString());
                        }
                        h.Item3 = Math.Max(h.Item3, node.Children.Count);
                        return h;
                    });
                }

                foreach (var grandChild in node.Children)
                    nodes.Push(grandChild);
            }
        }

        private static string GenerateBuiltinInfo()
        {
            return string.Join("\r\n", MethodInfos.OrderBy(i => i.Value.Item1.First()).Select(i => JsonSerializer.Serialize(i.Value.Item1) + ": " + i.Key + " <" + string.Join(",", i.Value.Item2) + "> #" + i.Value.Item3));
        }
    }
}
