using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace MCPForUnity.Editor.Tools.Build
{
    public static class BuildSettingsHelper
    {
        // 说明：NamedBuildTarget 为 Unity 2021.2 引入，2020.3 不存在。此处统一以 BuildTargetGroup
        // 作为传递类型（全版本通用），PlayerSettings 的版本差异集中在文件底部的兼容访问器中。
        public static object ReadProperty(string property, BuildTargetGroup targetGroup)
        {
            switch (property.ToLowerInvariant())
            {
                case "product_name":
                    return new { property, value = PlayerSettings.productName };
                case "company_name":
                    return new { property, value = PlayerSettings.companyName };
                case "version":
                    return new { property, value = PlayerSettings.bundleVersion };
                case "bundle_id":
                    return new { property, value = GetBundleId(targetGroup) };
                case "scripting_backend":
                    var backend = GetBackend(targetGroup);
                    return new { property, value = backend == ScriptingImplementation.IL2CPP ? "il2cpp" : "mono" };
                case "defines":
                    return new { property, value = GetDefines(targetGroup) };
                case "architecture":
                    var arch = GetArch(targetGroup);
                    string archName = arch switch { 0 => "x86_64", 1 => "arm64", 2 => "universal", _ => "unknown" };
                    return new { property, value = archName, raw = arch };
                default:
                    return null;
            }
        }

        public static string WriteProperty(string property, string value, BuildTargetGroup targetGroup)
        {
            try
            {
                switch (property.ToLowerInvariant())
                {
                    case "product_name":
                        PlayerSettings.productName = value;
                        return null;
                    case "company_name":
                        PlayerSettings.companyName = value;
                        return null;
                    case "version":
                        PlayerSettings.bundleVersion = value;
                        return null;
                    case "bundle_id":
                        SetBundleId(targetGroup, value);
                        return null;
                    case "scripting_backend":
                        var backendValue = value.ToLowerInvariant();
                        if (backendValue != "il2cpp" && backendValue != "mono")
                            return $"Unknown scripting_backend '{value}'. Valid: mono, il2cpp";
                        var impl = backendValue == "il2cpp"
                            ? ScriptingImplementation.IL2CPP
                            : ScriptingImplementation.Mono2x;
                        SetBackend(targetGroup, impl);
                        return null;
                    case "defines":
                        SetDefines(targetGroup, value);
                        return null;
                    case "architecture":
                        int arch = value.ToLowerInvariant() switch
                        {
                            "x86_64" => 0,
                            "none" => 0,
                            "default" => 0,
                            "arm64" => 1,
                            "universal" => 2,
                            _ => -1
                        };
                        if (arch < 0)
                            return $"Unknown architecture '{value}'. Valid: x86_64, arm64, universal";
                        SetArch(targetGroup, arch);
                        return null;
                    default:
                        return $"Unknown property '{property}'. Valid: product_name, company_name, version, bundle_id, scripting_backend, defines, architecture";
                }
            }
            catch (Exception ex)
            {
                return $"Failed to set {property}: {ex.Message}";
            }
        }

        public static readonly IReadOnlyList<string> ValidProperties = new[]
        {
            "product_name", "company_name", "version", "bundle_id",
            "scripting_backend", "defines", "architecture"
        };

        // ── 版本兼容访问器：隔离 2020.3（BuildTargetGroup）与 2021.2+（NamedBuildTarget）的 PlayerSettings API 差异 ──
#if UNITY_2021_2_OR_NEWER
        private static NamedBuildTarget NT(BuildTargetGroup g) => NamedBuildTarget.FromBuildTargetGroup(g);
        private static string GetBundleId(BuildTargetGroup g) => PlayerSettings.GetApplicationIdentifier(NT(g));
        private static void SetBundleId(BuildTargetGroup g, string v) => PlayerSettings.SetApplicationIdentifier(NT(g), v);
        private static ScriptingImplementation GetBackend(BuildTargetGroup g) => PlayerSettings.GetScriptingBackend(NT(g));
        internal static void SetBackend(BuildTargetGroup g, ScriptingImplementation i) => PlayerSettings.SetScriptingBackend(NT(g), i);
        private static string GetDefines(BuildTargetGroup g) => PlayerSettings.GetScriptingDefineSymbols(NT(g));
        private static void SetDefines(BuildTargetGroup g, string v) => PlayerSettings.SetScriptingDefineSymbols(NT(g), v);
        private static int GetArch(BuildTargetGroup g) => PlayerSettings.GetArchitecture(NT(g));
        private static void SetArch(BuildTargetGroup g, int a) => PlayerSettings.SetArchitecture(NT(g), a);
#else
        private static string GetBundleId(BuildTargetGroup g) => PlayerSettings.GetApplicationIdentifier(g);
        private static void SetBundleId(BuildTargetGroup g, string v) => PlayerSettings.SetApplicationIdentifier(g, v);
        private static ScriptingImplementation GetBackend(BuildTargetGroup g) => PlayerSettings.GetScriptingBackend(g);
        internal static void SetBackend(BuildTargetGroup g, ScriptingImplementation i) => PlayerSettings.SetScriptingBackend(g, i);
        private static string GetDefines(BuildTargetGroup g) => PlayerSettings.GetScriptingDefineSymbolsForGroup(g);
        private static void SetDefines(BuildTargetGroup g, string v) => PlayerSettings.SetScriptingDefineSymbolsForGroup(g, v);
        private static int GetArch(BuildTargetGroup g) => PlayerSettings.GetArchitecture(g);
        private static void SetArch(BuildTargetGroup g, int a) => PlayerSettings.SetArchitecture(g, a);
#endif
    }
}
