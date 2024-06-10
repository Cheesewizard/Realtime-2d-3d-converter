using System.Collections.Generic;
using Doji.AI.Depth;
using Game.Scripts.Configs;
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
				midas = new Midas(modelDatabase.ModelType);
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
			width = (int) framesVideoPlayer.width / 2;
			height = (int) framesVideoPlayer.height;

			videoRenderTexture = new RenderTexture(width, height, GraphicsFormat.B8G8R8_UNorm, GraphicsFormat.None);
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
			Debug.Log("Processing new frame");

			midas.EstimateDepth(videoRenderTexture, false);
			var depthMap = midas.Result;

			videoRenderBufferQueue.Enqueue((ResizeRenderTexture(videoRenderTexture, depthMap.width, depthMap.height), depthMap));

			Debug.Log("Finished Processing new frame");
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
				// var (leftEyeBuffer, leftDepth) = leftEyeBufferQueue.Dequeue();s
				// var (rightEyeBuffer, rightDepth) =  rightEyeBufferQueue.Dequeue();

				stereoscopic3dCameras.UpdateCameraShader(mainTexture, depth);
				// stereoscopic3dCameras.UpdateLeftCamera(leftEyeBuffer, leftDepth);
				// stereoscopic3dCameras.UpdateRightCameras(rightEyeBuffer, rightDepth);
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

		private Texture2D ApplyDepthToVideo(Texture2D videoTexture, Texture2D depthMap)
		{
			// Create a new texture with the same dimensions as the video frame
			// Create a new texture with the same dimensions as the video frame
			var depthEnhancedTexture = new Texture2D(videoTexture.width, videoTexture.height);

			// Get pixels from video texture
			Color[] videoPixels = videoTexture.GetPixels();

			// Apply depth map to the alpha channel of the video texture
			for (int i = 0; i < videoPixels.Length; i++)
			{
				// Calculate UV coordinates for depth map based on current pixel position in video texture
				float u = (float)(i % videoTexture.width) / videoTexture.width;
				float v = (float)(i / videoTexture.width) / videoTexture.height;

				// Get depth value from depth map using bilinear filtering
				Color depthPixel = depthMap.GetPixelBilinear(u, v);
				float depthValue = depthPixel.r;

				// Set alpha channel of video pixel to depth value
				Color videoColor = videoPixels[i];
				videoPixels[i] = new Color(videoColor.r, videoColor.g, videoColor.b, depthValue);
			}

			// Set pixels to depth-enhanced texture
			depthEnhancedTexture.SetPixels(videoPixels);
			depthEnhancedTexture.Apply();

			return depthEnhancedTexture;
		}

		private void OnDestroy()
		{
			midas.Dispose();
			modelDatabase.OnModelChanged -= HandleModelChanged;
		}
	}
}