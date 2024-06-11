using UnityEngine;

namespace Game.Scripts
{
	public class Stereoscopic3d : MonoBehaviour
	{
		[SerializeField]
		private Camera leftCamera;

		[SerializeField]
		private Camera rightCamera;

		[SerializeField]
		private GameObject videoPlayer;

		[SerializeField, Range(-1f, 1f)]
		private float targetDepthShift = 0.02f;

		[SerializeField, Range(0f, 1f)]
		private float targetDepthScale = 0.02f;

		private static readonly int mainTex = Shader.PropertyToID("_MainTex");
		private static readonly int depthTex = Shader.PropertyToID("_DepthTex");
		private static readonly int leftShift = Shader.PropertyToID("_LeftShift");
		private static readonly int rightShift = Shader.PropertyToID("_RightShift");
		private static readonly int depthFactor = Shader.PropertyToID("_DepthFactor");

		[SerializeField]
		private Material videoPlayerMaterial;

		[SerializeField]
		private Material debugDepthMaterial;

		private void Update()
		{
			videoPlayerMaterial.SetFloat(leftShift, targetDepthShift);
			videoPlayerMaterial.SetFloat(rightShift, -targetDepthShift);
			videoPlayerMaterial.SetFloat(depthFactor, targetDepthScale);
		}

		public void UpdateCameraShader(Texture texture, Texture depthTexture)
		{
			videoPlayerMaterial.SetTexture(mainTex, texture);
			videoPlayerMaterial.SetTexture(depthTex, depthTexture);

			debugDepthMaterial.SetTexture(depthTex, depthTexture);
		}
	}
}