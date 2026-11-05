using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace AssetBundleLoadingTools.Rendering
{
    // Built-in ForwardBase passes read the main directional light from these globals.
    internal sealed class LegacyLightingPass : ScriptableRenderPass
    {
        private static readonly int LightColor = Shader.PropertyToID("_LightColor0");
        private static readonly int LightPosition = Shader.PropertyToID("_WorldSpaceLightPos0");

        internal LegacyLightingPass()
        {
            // Mirrors can render at BeforeRendering, before the ordinary opaque pass.
            renderPassEvent = RenderPassEvent.BeforeRendering;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Legacy lighting", out PassData data);
            data.Color = Vector4.zero;
            data.Position = new Vector4(0, 1, 0, 0);
            int mainLight = lightData.mainLightIndex;
            if (mainLight >= 0 && mainLight < lightData.visibleLights.Length)
            {
                VisibleLight light = lightData.visibleLights[mainLight];
                if (light.lightType == LightType.Directional)
                {
                    data.Color = light.finalColor;
                    Vector4 forward = light.localToWorldMatrix.GetColumn(2);
                    data.Position = new Vector4(-forward.x, -forward.y, -forward.z, 0);
                }
            }

            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData pass, RasterGraphContext context) =>
            {
                context.cmd.SetGlobalVector(LightColor, pass.Color);
                context.cmd.SetGlobalVector(LightPosition, pass.Position);
                context.cmd.EnableShaderKeyword("DIRECTIONAL");
            });
        }

        private sealed class PassData
        {
            internal Vector4 Color { get; set; }

            internal Vector4 Position { get; set; }
        }
    }
}
