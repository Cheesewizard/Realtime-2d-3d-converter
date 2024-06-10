using UnityEngine;

namespace Game.Scripts
{
	public class FullScreenCameraAdjuster : MonoBehaviour
	{
		[SerializeField]
		private Camera mainCamera;

		[SerializeField]
		public RenderTexture renderTexture;

		private void Start()
		{
			AdjustCamera();
		}

		private void AdjustCamera()
		{
			var screenAspect = Screen.width / (float)Screen.height;
			var textureAspect = (float)renderTexture.width / renderTexture.height;

			if (textureAspect > screenAspect)
			{
				// If the texture is wider than the screen, adjust the height FOV
				var scaleHeight = screenAspect / textureAspect;
				mainCamera.fieldOfView = 2.0f * Mathf.Atan(Mathf.Tan(Mathf.Deg2Rad * mainCamera.fieldOfView * 0.5f) / scaleHeight) * Mathf.Rad2Deg;
			}
			else
			{
				// If the texture is taller than the screen, adjust the width FOV
				float scaleWidth = textureAspect / screenAspect;
				mainCamera.fieldOfView = 2.0f * Mathf.Atan(Mathf.Tan(Mathf.Deg2Rad * mainCamera.fieldOfView * 0.5f) * scaleWidth) * Mathf.Rad2Deg;
			}
		}

		void Update()
		{
			// Call AdjustCamera() in Update() if you want to dynamically adjust the camera during runtime
			AdjustCamera();
		}
	}
}