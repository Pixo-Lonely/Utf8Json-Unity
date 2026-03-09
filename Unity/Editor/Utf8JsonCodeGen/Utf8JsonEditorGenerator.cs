#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Utf8Json.CodeGenerator.Generator;
using Utf8Json.UniversalCodeGenerator;

namespace Utf8Json.Editor
{
    /// <summary>
    /// Unity Editor entry point for Utf8Json code generation.
    /// Implements IPreprocessBuildWithReport to auto-generate Resolver before build,
    /// and provides a manual menu item.
    /// </summary>
    public class Utf8JsonEditorGenerator : IPreprocessBuildWithReport
    {
        // --- Configuration ---

        /// <summary>
        /// List of directories to scan for .cs files.
        /// Defaults to the Assets folder of the current project.
        /// </summary>
        public static List<string> InputDirectories = new List<string>
        {
            Path.Combine(Application.dataPath),
        };

        /// <summary>
        /// Output file path for the generated resolver.
        /// </summary>
        public static string OutputPath = Path.Combine(Application.dataPath, "Utf8Json.Generated.cs");

        /// <summary>
        /// Name for the generated resolver class.
        /// </summary>
        public static string ResolverName = "GeneratedResolver";

        /// <summary>
        /// Root namespace for generated code (e.g. "Utf8Json").
        /// </summary>
        public static string NamespaceRoot = "Utf8Json";

        /// <summary>
        /// Conditional compiler symbols to pass to Roslyn.
        /// </summary>
        public static List<string> ConditionalSymbols = new List<string>();

        /// <summary>
        /// Whether to allow generating code for internal types.
        /// </summary>
        public static bool AllowInternal = false;

        // IPreprocessBuildWithReport
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                Generate();
            }
            catch (Exception ex)
            {
                throw new BuildFailedException($"[Utf8JsonEditorGenerator] Code generation failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [MenuItem("Tools/Utf8Json/Generate Resolver")]
        public static void GenerateFromMenu()
        {
            try
            {
                Generate();
                Debug.Log("[Utf8JsonEditorGenerator] Code generation completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Utf8JsonEditorGenerator] Code generation failed: {ex}");
            }
        }

        public static void Generate()
        {
            var namespaceDot = string.IsNullOrWhiteSpace(NamespaceRoot) ? "" : NamespaceRoot + ".";

            Debug.Log("[Utf8JsonEditorGenerator] Compilation start: " + string.Join(", ", InputDirectories));

            var collector = new TypeCollector(
                inputFiles: Enumerable.Empty<string>(),
                inputDirs: InputDirectories,
                conditinalSymbols: ConditionalSymbols,
                disallowInternal: !AllowInternal
            );

            Debug.Log("[Utf8JsonEditorGenerator] Compilation complete. Starting type collection...");

            var (objectInfo, genericInfo) = collector.Collect();

            Debug.Log($"[Utf8JsonEditorGenerator] Collected {objectInfo.Length} object types, {genericInfo.Length} generic types.");

            var objectFormatterTemplates = objectInfo
                .GroupBy(x => x.Namespace)
                .Select(x => new FormatterTemplate()
                {
                    Namespace = namespaceDot + "Formatters" + (x.Key == null ? "" : "." + x.Key),
                    objectSerializationInfos = x.ToArray(),
                })
                .ToArray();

            var resolverTemplate = new ResolverTemplate()
            {
                Namespace = namespaceDot + "Resolvers",
                FormatterNamespace = namespaceDot + "Formatters",
                ResolverName = ResolverName,
                registerInfos = genericInfo.Cast<IResolverRegisterInfo>().Concat(objectInfo).ToArray()
            };

            var sb = new StringBuilder();
            sb.AppendLine(resolverTemplate.TransformText());
            sb.AppendLine();

            foreach (var item in objectFormatterTemplates)
            {
                sb.AppendLine(item.TransformText());
            }

            WriteOutput(OutputPath, sb.ToString());

            Debug.Log($"[Utf8JsonEditorGenerator] Output written to: {OutputPath}");

            AssetDatabase.Refresh();
        }

        static void WriteOutput(string path, string text)
        {
            path = path.Replace("global::", "");

            var fi = new FileInfo(path);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            File.WriteAllText(path, text, Encoding.UTF8);
        }
    }
}
#endif
