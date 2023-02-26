using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anthelme.BlurredBufferOutlines
{
	internal class VertexColorRenderPass : ScriptableRenderPass
	{
		private readonly ProfilingSampler _profilingSampler = new("BlurredBufferOutlines.VertexColor");

		private readonly List<ShaderTagId> _shaderTags = new()
		{
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("UniversalForward"),
			new ShaderTagId("UniversalForwardOnly")
		};

		private readonly Material _vertexColorMaterial;

		private SortingCriteria _sortingCriteria;
		private LayerMask _filterSettingsLayerMask;

		private RTHandle _cameraDepthTargetRTHandle;
		private RTHandle _vertexColorRTHandle;

		private static readonly int CameraVertexColorShaderProperty = Shader.PropertyToID("_CameraVertexColor");

		public VertexColorRenderPass(Shader vertexColorShader)
		{
			_vertexColorMaterial = CoreUtils.CreateEngineMaterial(vertexColorShader);
		}

		public void UpdateProperties(RTHandle depthRTHandle, OutlinesRendererFeature.OutlinesSettings outlinesSettings)
		{
			_cameraDepthTargetRTHandle = depthRTHandle;
			
			_filterSettingsLayerMask = outlinesSettings.filterSettingsLayerMask;

			renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

			cameraTargetDescriptor.msaaSamples = 1;
			cameraTargetDescriptor.depthBufferBits = 0;

			RenderingUtils.ReAllocateIfNeeded(ref _vertexColorRTHandle, cameraTargetDescriptor,
				name: "VertexColor_Texture");

			ConfigureTarget(_vertexColorRTHandle, _cameraDepthTargetRTHandle);
			ConfigureClear(ClearFlag.Color, Color.black);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cameraData = renderingData.cameraData;

			if (cameraData.camera.cameraType != CameraType.Game)
				return;

			if (_vertexColorMaterial == null)
				return;

			var defaultSortingSettings = new SortingSettings(cameraData.camera);
			var defaultFilteringSettings = FilteringSettings.defaultValue;
			
			var defaultDrawingSettings = CreateDrawingSettings(_shaderTags, ref renderingData,
				defaultSortingSettings.criteria);

			var vertexColorFilterSettings = defaultFilteringSettings;
			vertexColorFilterSettings.layerMask = _filterSettingsLayerMask;

			var vertexColorDrawingSettings = defaultDrawingSettings;
			vertexColorDrawingSettings.overrideMaterial = _vertexColorMaterial;
			vertexColorDrawingSettings.overrideMaterialPassIndex = 0;

			context.DrawRenderers(renderingData.cullResults, ref vertexColorDrawingSettings,
				ref vertexColorFilterSettings);

			var commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, _profilingSampler))
			{
				commandBuffer.SetGlobalTexture(CameraVertexColorShaderProperty, _vertexColorRTHandle);
			}

			context.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();

			CommandBufferPool.Release(commandBuffer);
		}

		public void Dispose()
		{
			_vertexColorRTHandle?.Release();

			CoreUtils.Destroy(_vertexColorMaterial);
		}
	}
}