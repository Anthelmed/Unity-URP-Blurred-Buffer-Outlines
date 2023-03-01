using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Anthelme.BlurredBufferOutlines
{
	public class OutlinesRendererFeature : ScriptableRendererFeature
	{
		[Serializable]
		public class OutlinesSettings
		{
			public LayerMask filterSettingsLayerMask;
			[Range(0.0f, 1.0f)] public float opacity = 0.8f;
			[Range(1, 4)] public int downsample = 2;
			[Range(1, 10)] public int blurIterations = 4;
		}

		public OutlinesSettings outlinesSettings = new();
		public Shader vertexColorShader;
		public Shader copyVertexColorShader;
		public Shader blurShader;
		public Shader maskShader;

		private VertexColorRenderPass _vertexColorRenderPass;
		private BlurAndMaskRenderPass _blurAndMaskRenderPass;

		
		public override void AddRenderPasses(ScriptableRenderer renderer,
			ref RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType == CameraType.Game)
			{
				renderer.EnqueuePass(_vertexColorRenderPass);
				renderer.EnqueuePass(_blurAndMaskRenderPass);
			}
		}

		public override void SetupRenderPasses(ScriptableRenderer renderer,
			in RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType != CameraType.Game) return;
			
			_vertexColorRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
			_vertexColorRenderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
			_blurAndMaskRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);

			_vertexColorRenderPass.UpdateProperties(renderer.cameraDepthTargetHandle, outlinesSettings);
			_blurAndMaskRenderPass.UpdateProperties(renderer.cameraColorTargetHandle, outlinesSettings);
		}

		public override void Create()
		{
			if (vertexColorShader == null)
				vertexColorShader = Shader.Find("Hidden/Anthelme/BlurredBufferOutlines/VertexColorShaderGraph");
			if (copyVertexColorShader == null)
				copyVertexColorShader = Shader.Find("Hidden/Anthelme/BlurredBufferOutlines/CopyVertexColorShaderGraph");
			if (blurShader == null)
				blurShader = Shader.Find("Hidden/Anthelme/BlurredBufferOutlines/BlurShaderGraph");
			if (maskShader == null)
				maskShader = Shader.Find("Hidden/Anthelme/BlurredBufferOutlines/MaskShaderGraph");
			
			_vertexColorRenderPass = new VertexColorRenderPass(vertexColorShader);
			_blurAndMaskRenderPass = new BlurAndMaskRenderPass(copyVertexColorShader, blurShader, maskShader);
		}

		protected override void Dispose(bool disposing)
		{
			_vertexColorRenderPass?.Dispose();
			_blurAndMaskRenderPass?.Dispose();

			_vertexColorRenderPass = null;
			_blurAndMaskRenderPass = null;
		}
	}
}
