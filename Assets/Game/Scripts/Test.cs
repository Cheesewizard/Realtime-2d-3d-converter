using System.Collections.Generic;
using Doji.AI.Depth;
using Game.Scripts.Configs;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Video;

namespace Game.Scripts
{
	public class Test : MonoBehaviour
	{
		[SerializeField]
		private VideoPlayer framesVideoPlayer;

		[SerializeField]
		private GameObject videoPlayer;

		[SerializeField]
		private GameObject depthVideoPlayer;

		[SerializeField]
		private MidasDatabase modelDatabase;

		[SerializeField]
		private RenderTexture videoRenderTexture;

		[SerializeField]
		private Stereoscopic3d stereoscopic3dCameras;

		[SerializeField]
		private int bufferSize = 30;

		private readonly Queue<(Texture, Texture)> videoRenderBufferQueue = new();

		private Midas midas;

		private int width;
		private int height;

		private void Start()
		{
			modelDatabase.OnModelChanged += HandleModelChanged;

			try
			{
				midas = new Midas(modelDatabase.ModelType)
				{
					NormalizeDepth = true,
					Backend = BackendType.CPU
				};
			}
			catch
			{
				midas = new Midas();
				Debug.LogError("No model found for the selected Midas type. Setting as the default.");
			}

			StartFramesVideoPlayer();
			SetupRenderTexture();
		}

		private void SetupRenderTexture()
		{
			width = (int) framesVideoPlayer.width;
			height = (int) framesVideoPlayer.height;

			videoRenderTexture = new RenderTexture(width, height, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D32_SFloat_S8_UInt);
			videoRenderTexture.Create();
			framesVideoPlayer.targetTexture = videoRenderTexture;
		}

		private void HandleModelChanged()
		{
			try
			{
				var tempMidas = new Midas(modelDatabase.ModelType);
				midas.Dispose();
				midas = tempMidas;
			}
			catch
			{
				Debug.LogError("No model found for the selected Midas type. Reverting to previous model.");
			}
		}

		private void StartFramesVideoPlayer()
		{
			framesVideoPlayer.Prepare();

			framesVideoPlayer.sendFrameReadyEvents = true;
			framesVideoPlayer.frameReady += HandleFrameReady;
			framesVideoPlayer.Play();

			Debug.Log("Finished frames video player");
		}

		private void HandleFrameReady(VideoPlayer source, long frameidx)
		{
			var previous = RenderTexture.active;
			RenderTexture.active = videoRenderTexture;

			var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();

			RenderTexture.active = previous;

			midas.EstimateDepth(tex);

			var depthMap = midas.Result;
			if (depthMap != null)
			{
				videoRenderBufferQueue.Enqueue((videoRenderTexture, depthMap));
			}
			else
			{
				Debug.LogError("Failed to generate depth map");
			}
		}

		// Helper method to save render texture to file (for debugging)
		private void SaveRenderTextureToFile(Texture2D texture, string fileName)
		{
			byte[] bytes = texture.EncodeToPNG();
			System.IO.File.WriteAllBytes(Application.dataPath + $"/{fileName}", bytes);
		}

		// Method to resize texture
		private RenderTexture ResizeRenderTexture(RenderTexture source, int width, int height)
		{
			var result = new RenderTexture(width, height, source.depth, source.format);
			result.Create();

			Graphics.Blit(source, result);
			return result;
		}

		private void Update()
		{
			if (videoRenderBufferQueue.Count >= bufferSize)
			{
				var (mainTexture, depth) = videoRenderBufferQueue.Dequeue();
				stereoscopic3dCameras.UpdateCameraShader(mainTexture, depth);
			}
		}

		private Texture2D ScaleTexture(Texture2D sourceTexture, int newWidth, int newHeight)
		{
			// Create a new texture with the desired dimensions
			Texture2D scaledTexture = new Texture2D(newWidth, newHeight);

			// Iterate over each pixel in the scaled texture
			for (int y = 0; y < newHeight; y++)
			{
				for (int x = 0; x < newWidth; x++)
				{
					// Calculate the corresponding position in the source texture
					float u = (float)x / newWidth;
					float v = (float)y / newHeight;

					// Sample the source texture using bilinear filtering
					Color color = sourceTexture.GetPixelBilinear(u, v);

					// Assign the sampled color to the corresponding pixel in the scaled texture
					scaledTexture.SetPixel(x, y, color);
				}
			}

			// Apply the changes to the scaled texture
			scaledTexture.Apply();

			return scaledTexture;
		}

		private void OnDestroy()
		{
			midas.Dispose();
			modelDatabase.OnModelChanged -= HandleModelChanged;
		}
	}
}