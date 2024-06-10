using UnityEngine;

namespace Game.Scripts
{
	public static class TextureUtils
	{
		public static Texture2D ToTexture2D(this Texture texture, int targetWidth = 0, int targetHeight = 0, bool shouldOverrideSize = false)
		{
			int width;
			int height;

			if (shouldOverrideSize)
			{
				width = targetWidth;
				height = targetHeight;
			}
			else
			{
				width = texture.width;
				height = texture.height;
			}

			var texture2D = new Texture2D(width, height);
			texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
			texture2D.Apply();

			return texture2D;
		}

		public static Texture2D ToTexture2D(this RenderTexture rTex)
		{
			var tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);

			// Ensure that the RenderTexture is active before reading pixels
			RenderTexture.active = rTex;

			// Create a temporary RenderTexture to avoid reading pixels outside of bounds
			RenderTexture tmp = RenderTexture.active;
			RenderTexture.active = rTex;

			// Read the pixels from the RenderTexture
			tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
			tex.Apply();

			// Restore the original active RenderTexture
			RenderTexture.active = tmp;

			return tex;
		}
	}
}