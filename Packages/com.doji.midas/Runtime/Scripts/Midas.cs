using System;
using Unity.Sentis;
using UnityEngine;

namespace Doji.AI.Depth {

    /// <summary>
    /// A class that allows to run Midas models
    /// to do monocular depth estimation.
    /// </summary>
    public class Midas : IDisposable {

        /// <summary>
        /// Which of the MiDaS models to run.
        /// </summary>
        public ModelType ModelType {
            get => _modelType;
            set {
                if (_modelType != value) {
                    Dispose();
                    _modelType = value;
                    InitializeNetwork();
                }
            }
        }
        private ModelType _modelType;

        /// <summary>
        /// Which <see cref="BackendType"/> to run the model with.
        /// </summary>
        public BackendType Backend {
            get => _backend;
            set {
                if (_backend != value) {
                    Dispose();
                    _backend = value;
                    InitializeNetwork();
                }
            }
        }
        private BackendType _backend = BackendType.GPUCompute;

        /// <summary>
        /// Whether to normalize the estimated depth.
        /// </summary>
        /// <remarks>
        /// MiDaS predicts depth values as inverse relative depth.
        /// (small values for far away objects, large values for near objects)
        /// If NormalizeDepth is enabled, these values are mapped to the (0, 1) range,
        /// which is mostly useful for visualization.
        /// </remarks>
        public bool NormalizeDepth { get; set; } = true;

        /// <summary>
        /// A RenderTexture that contains the estimated depth.
        /// </summary>
        public RenderTexture Result { get; set; }

        /// <summary>
        /// The runtime model.
        /// </summary>
        private Model _model;

        private IWorker _worker;
        private Ops _ops;

        /// <summary>
        /// the name of the Midas model
        /// </summary>
        private string _name;

        /// <summary>
        /// the (possibly resized) input texture;
        /// </summary>
        private RenderTexture _resizedInput;

        /// <summary>
        /// Caches the last predicted output
        /// </summary>
        private Tensor _predictedDepth;

#if UNITY_EDITOR
        public static event Action<ModelType> OnModelRequested = (x) => {};
#endif

        /// <summary>
        /// Initializes a new instance of MiDaS.
        /// </summary>
        public Midas(ModelType modelType = ModelType.midas_v21_small_256) {
            _modelType = modelType;
            InitializeNetwork();
        }

        public Midas(ModelAsset modelAsset) {
            _modelType = ModelType.Unknown;
            InitializeNetwork(modelAsset);
        }

        private void InitializeNetwork() {
            if (_modelType == ModelType.Unknown) {
                throw new InvalidOperationException("Not a valid model type.");
            }

#if UNITY_EDITOR
            OnModelRequested?.Invoke(_modelType);
#endif

            ModelAsset modelAsset = Resources.Load<ModelAsset>(_modelType.ResourcePath());

            if (modelAsset == null) {
                throw new Exception($"Could not load model '{ModelType}'. Make sure the model exists in your project.");
            }

            InitializeNetwork(modelAsset);
        }

        private void InitializeNetwork(ModelAsset modelAsset) {
            if (modelAsset == null) {
                throw new ArgumentException("ModelAsset was null", nameof(modelAsset));
            }

            _model = ModelLoader.Load(modelAsset);
            _name = modelAsset.name;
            Resources.UnloadAsset(modelAsset);
            _worker = WorkerFactory.CreateWorker(Backend, _model);
            _ops = WorkerFactory.CreateOps(Backend, null);

            int width = _model.inputs[0].shape[2].value;
            int height = _model.inputs[0].shape[3].value;

            InitInputTexture(width, height);
            InitOutputTexture(width, height);
        }

        private void InitInputTexture(int width, int height) {
            if (_resizedInput == null || _resizedInput.width != width || _resizedInput.height != height) {
                _resizedInput = new RenderTexture(width, height, 0) {
                    autoGenerateMips = false,
                };
            }
        }
        
        private void InitOutputTexture(int width, int height) {
            if (Result != null) {
                if (Result.width != width || Result.height != height) {
                    Result.Release();
                    Result.width = width;
                    Result.height = height;
                    Result.Create();
                }
            } else {
                Result = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
            }
            Result.name = $"depth_{_name}";
        }

        public void EstimateDepth(Texture input, bool autoResize = true) {
            // resize
            if (autoResize) {
                Resize(ref input);
            }

            using (var tensor = TextureConverter.ToTensor(_resizedInput, _resizedInput.width, _resizedInput.height, 3)) {
                _worker.Execute(tensor);
            }

            _predictedDepth = _worker.PeekOutput();
            int height = _predictedDepth.shape[1];
            int width = _predictedDepth.shape[2];

            // normalize
            if (NormalizeDepth) {
                _predictedDepth = Normalize(_predictedDepth);
            }
            TextureConverter.RenderToTexture(_predictedDepth.ShallowReshape(new TensorShape(1, 1, height, width)) as TensorFloat, Result);

            Result.name = $"{input.name}_depth_{_name}";
        }

        /// <summary>
        /// Returns the minimum and maximum values of the last depth prediction.
        /// </summary>
        /// <remarks>
        /// Keep in mind that the predictions are relative *inverse* depth values,
        /// i.e. min refers to the furthest away point and max to the closest point.
        /// </remarks>
        public (float min, float max) GetMinMax() {
            if (_predictedDepth == null) {
                throw new InvalidOperationException("No depth estimation has been executed yet. " +
                    "Call 'EstimateDepth' before trying to retrieve min/max");
            }
            TensorFloat minT = _ops.ReduceMin(_predictedDepth as TensorFloat, null, false);
            TensorFloat maxT = _ops.ReduceMax(_predictedDepth as TensorFloat, null, false);
            minT.MakeReadable();
            maxT.MakeReadable();
            return (minT[0], maxT[0]);
        }

        private void Resize(ref Texture input) {
            Graphics.Blit(input, _resizedInput);
            _resizedInput.name = input.name;
            input = _resizedInput;
        }

        /// <summary>
        /// Normalize on-device using Tensor Ops.
        /// </summary>
        private Tensor Normalize(Tensor depth) {
            TensorFloat minT = _ops.ReduceMin(depth as TensorFloat, null, false);
            TensorFloat maxT = _ops.ReduceMax(depth as TensorFloat, null, false);
            TensorFloat a = _ops.Sub(depth as TensorFloat, minT);
            TensorFloat b = _ops.Sub(maxT, minT);
            Tensor normalized = _ops.Div(a, b);
            return normalized;
        }

        public void Dispose() {
            _worker?.Dispose();
            _ops?.Dispose();
            if (_resizedInput != null) {
                _resizedInput.Release();
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(_resizedInput);
#else
                UnityEngine.Object.Destroy(_resizedInput);
#endif
            }
        }
    }
}