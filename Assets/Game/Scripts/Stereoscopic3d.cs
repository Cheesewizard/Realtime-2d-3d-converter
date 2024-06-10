using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField, Range(0f, 1f)]
        private float targetDepthScale = 0.01f;

        [SerializeField, Range(-1f, 1f)]
        private float targetDepthShift;

        private static readonly int mainTex  = Shader.PropertyToID("_MainTex");
        private static readonly int depthTex = Shader.PropertyToID("_DepthTex");
        private static readonly int leftShift = Shader.PropertyToID("_LeftShift");
        private static readonly int rightShift = Shader.PropertyToID("_RightShift");
        private static readonly int depthFactor = Shader.PropertyToID("_DepthFactor");


        private static readonly int leftEyeTex = Shader.PropertyToID("_LeftTex");
        private static readonly int leftEyeDepthTex = Shader.PropertyToID("_LeftDepthTex");
        private static readonly int rightEyeTex = Shader.PropertyToID("_RightTex");
        private static readonly int rightEyeDepthTex = Shader.PropertyToID("_RightDepthTex");


        private Material videoPlayerMaterial;

        private void Awake()
        {
            videoPlayerMaterial = videoPlayer.GetComponent<MeshRenderer>().sharedMaterial;
        }

        private void Update()
        {
            videoPlayerMaterial.SetFloat(depthFactor, targetDepthScale);
            videoPlayerMaterial.SetFloat(leftShift, targetDepthScale);
            videoPlayerMaterial.SetFloat(rightShift, -targetDepthScale);
        }

        public void UpdateCameraShader(Texture leftTexture, Texture depthTexture)
        {
            videoPlayerMaterial.SetTexture(mainTex, leftTexture);
            videoPlayerMaterial.SetTexture(depthTex, depthTexture);
        }
    }
}