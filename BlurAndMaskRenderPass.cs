using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Anthelme.BlurredBufferOutlines
{
	internal class BlurAndMaskRenderPass : ScriptableRenderPass
	{
		private readonly ProfilingSampler _profilingSampler = new("BlurredBufferOutlines.BlurAndMask");

		private enum BlurDirection
		{
			Horizontal = 0,
			Vertical = 1
		}
		
		private int _downsample;
		private float _opacity;
		private int _blurIterations;

		private readonly Material _copyVertexColorMaterial;
		private readonly Material _horizontalBlurMaterial;
		private readonly Material _verticalBlurMaterial;
		private readonly Material _maskMaterial;

		private RTHandle _cameraColorTargetRTHandle;

		private RTHandle _cameraColorRTHandle;
		private RTHandle _tempRTHandle0;
		private RTHandle _tempRTHandle1;
		private RTHandle _maskRTHandle;

		private static readonly int BlurDirectionShaderProperty = Shader.PropertyToID("_BlurDirection");

		private static readonly int CameraColorTextureShaderProperty = Shader.PropertyToID("_CameraColorTexture");
		private static readonly int OpacityShaderProperty = Shader.PropertyToID("_Opacity");

		public BlurAndMaskRenderPass(Shader copyVertexColorShader, Shader blurShader, Shader maskShader)
		{
			_copyVertexColorMaterial = CoreUtils.CreateEngineMaterial(copyVertexColorShader);
			_horizontalBlurMaterial = CoreUtils.CreateEngineMaterial(blurShader);
			_verticalBlurMaterial = CoreUtils.CreateEngineMaterial(blurShader);
			_maskMaterial = CoreUtils.CreateEngineMaterial(maskShader);

			_horizontalBlurMaterial.SetInt(BlurDirectionShaderProperty, (int)BlurDirection.Horizontal);
			_verticalBlurMaterial.SetInt(BlurDirectionShaderProperty, (int)BlurDirection.Vertical);
		}

		public void UpdateProperties(RTHandle colorRTHandle, OutlinesRendererFeature.OutlinesSettings outlinesSettings)
		{
			_cameraColorTargetRTHandle = colorRTHandle;
			
			_downsample = outlinesSettings.downsample;
			_opacity = outlinesSettings.opacity;
			_blurIterations = outlinesSettings.blurIterations;

			renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

			cameraTargetDescriptor.msaaSamples = 1;
			cameraTargetDescriptor.depthBufferBits = 0;

			RenderingUtils.ReAllocateIfNeeded(ref _cameraColorRTHandle, cameraTargetDescriptor,
				name: "Camera_Color_Texture");
			RenderingUtils.ReAllocateIfNeeded(ref _maskRTHandle, cameraTargetDescriptor, name: "Mask_Texture");

			cameraTargetDescriptor.width /= _downsample;
			cameraTargetDescriptor.height /= _downsample;

			RenderingUtils.ReAllocateIfNeeded(ref _tempRTHandle0, cameraTargetDescriptor, name: "Temp_Texture0");
			RenderingUtils.ReAllocateIfNeeded(ref _tempRTHandle1, cameraTargetDescriptor, name: "Temp_Texture1");

			ConfigureTarget(_cameraColorTargetRTHandle);
			ConfigureClear(ClearFlag.None, Color.white);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cameraData = renderingData.cameraData;

			if (cameraData.camera.cameraType != CameraType.Game)
				return;

			if (_copyVertexColorMaterial == null
			    || _horizontalBlurMaterial == null
			    || _maskMaterial == null)
				return;

			var commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, _profilingSampler))
			{
				Blitter.BlitCameraTexture(commandBuffer, _cameraColorTargetRTHandle, _cameraColorRTHandle);
				Blitter.BlitCameraTexture(commandBuffer, _tempRTHandle0, _tempRTHandle0, _copyVertexColorMaterial, 0);

				for (var index = 0; index < _blurIterations; index++)
				{
					BlitBlurTexture(commandBuffer, _tempRTHandle0, _tempRTHandle1, BlurDirection.Vertical);
					BlitBlurTexture(commandBuffer, _tempRTHandle1, _tempRTHandle0, BlurDirection.Horizontal);
				}

				BlitMaskTexture(commandBuffer);

				Blitter.BlitCameraTexture(commandBuffer, _maskRTHandle, _cameraColorTargetRTHandle);
			}

			context.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();

			CommandBufferPool.Release(commandBuffer);
		}

		private void BlitBlurTexture(CommandBuffer commandBuffer, RTHandle source, RTHandle destination,
			BlurDirection blurDirection)
		{
			var material = blurDirection switch
			{
				BlurDirection.Horizontal => _horizontalBlurMaterial,
				BlurDirection.Vertical => _verticalBlurMaterial,
				_ => null
			};

			Blitter.BlitCameraTexture(commandBuffer, source, destination, material, 0);
		}

		private void BlitMaskTexture(CommandBuffer commandBuffer)
		{
			_maskMaterial.SetFloat(OpacityShaderProperty, _opacity);
			_maskMaterial.SetTexture(CameraColorTextureShaderProperty, _cameraColorRTHandle.rt);

			Blitter.BlitCameraTexture(commandBuffer, _tempRTHandle0, _maskRTHandle, _maskMaterial, 0);
		}

		public void Dispose()
		{
			_cameraColorRTHandle?.Release();
			_tempRTHandle0?.Release();
			_tempRTHandle1?.Release();
			_maskRTHandle?.Release();

			CoreUtils.Destroy(_copyVertexColorMaterial);
			CoreUtils.Destroy(_horizontalBlurMaterial);
			CoreUtils.Destroy(_verticalBlurMaterial);
			CoreUtils.Destroy(_maskMaterial);
		}
	}
}