using System.Collections.Generic;
using System.Reflection;
using AssetBundleLoadingTools.Rendering;
using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AssetBundleLoadingTools.Patches
{
    [HarmonyPatch(typeof(ScriptableRenderer), "AddRenderPasses")]
    internal static class ScriptableRenderer_AddRenderPasses
    {
        public static void Prefix(ScriptableRenderer __instance)
        {
            if (LegacyRendering.ForwardBaseEnabled && LegacyRendering.LightingPass != null)
            {
                __instance.EnqueuePass(LegacyRendering.LightingPass);
            }

            if (LegacyRendering.GrabPassEnabled && LegacyRendering.GrabTexturePass != null)
            {
                __instance.EnqueuePass(LegacyRendering.GrabTexturePass);
            }
        }
    }

    [HarmonyPatch]
    internal static class RenderingUtils_CreateDrawingSettings
    {
        private static readonly ShaderTagId ForwardBase = new("ForwardBase");
        private static readonly ShaderTagId UniversalForward = new("UniversalForward");
        private static readonly ShaderTagId UniversalForwardOnly = new("UniversalForwardOnly");

        public static MethodBase TargetMethod() => AccessTools.Method(
            typeof(RenderingUtils),
            nameof(RenderingUtils.CreateDrawingSettings),
            new[] { typeof(List<ShaderTagId>), typeof(UniversalRenderingData), typeof(UniversalCameraData), typeof(UniversalLightData), typeof(SortingCriteria) });

        public static void Postfix(List<ShaderTagId> shaderTagIdList, ref DrawingSettings __result)
        {
            if (!LegacyRendering.ForwardBaseEnabled || shaderTagIdList == null || shaderTagIdList.Count == 0 ||
                shaderTagIdList.Count >= DrawingSettings.maxShaderPasses ||
                shaderTagIdList.Contains(ForwardBase) ||
                (!shaderTagIdList.Contains(UniversalForward) && !shaderTagIdList.Contains(UniversalForwardOnly)))
            {
                return;
            }

            // Appending a fallback preserves native pass priority, sorting and per-object data.
            __result.SetShaderPassName(shaderTagIdList.Count, ForwardBase);
        }
    }

    [HarmonyPatch]
    internal static class DrawObjectsPass_CanDisableZWrite
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("UnityEngine.Rendering.Universal.Internal.DrawObjectsPass"), "CanDisableZWrite");

        public static void Postfix(ref bool __result)
        {
            if (LegacyRendering.ForwardBaseEnabled)
            {
                // Legacy materials can lack the depth prepass required by depth priming's Equal test.
                __result = false;
            }
        }
    }
}
