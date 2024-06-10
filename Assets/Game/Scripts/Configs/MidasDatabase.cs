using System;
using Doji.AI.Depth;
using UnityEngine;

namespace Game.Scripts.Configs
{
	[CreateAssetMenu(fileName = "MidasDatabase", menuName = "Config/MidasDatabase")]
	public class MidasDatabase : ScriptableObject
	{
		[field: SerializeField]
		public ModelType ModelType { get; private set; }

		[SerializeField, HideInInspector]
		private ModelType previousModelType;

		public event Action OnModelChanged;

		private void OnValidate()
		{
			if (ModelType != previousModelType)
			{
				previousModelType = ModelType;
				OnModelChanged?.Invoke();
			}
		}
	}
}