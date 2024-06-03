using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WireframeFeature : ScriptableRendererFeature
{
    class WireframePass : ScriptableRenderPass
    {
        Material material;
        private LayerMask layerMask;
        private List<ShaderTagId> shaderTagIdList;

        public bool Initialized { get; private set; }

        public WireframePass(LayerMask layerMask, bool fixedWidth)
        {
            if (fixedWidth) { material = Resources.Load<Material>("wireframe_fixed_mat"); }
            else { material = Resources.Load<Material>("wireframe_mat"); }
            
            this.layerMask = layerMask;
            shaderTagIdList = new List<ShaderTagId>();

            shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));

            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

            if (material == null) { Debug.LogError("Could not load the wireframe material."); Initialized = false; }
            else { Initialized = true; }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
            drawingSettings.overrideMaterial = material;
            drawingSettings.overrideMaterialPassIndex = 0;
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
            RenderStateBlock renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
        }
    }

    //class WireframePass_Stencil : ScriptableRenderPass
    //{
    //    private LayerMask layerMask;

    //    public WireframePass_Stencil(LayerMask layerMask)
    //    {
    //        this.layerMask = layerMask;
    //        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    //    }

    //    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    //    {
            
    //    }
    //}

    WireframePass wireframePass;

    [SerializeField]
    private LayerMask layerMask;
    [SerializeField]
    private bool fixedWidth;

    public override void Create()
    {
        wireframePass = new WireframePass(layerMask, fixedWidth);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (wireframePass.Initialized)
        {
            renderer.EnqueuePass(wireframePass);
        }
    }
}


