﻿using UnityEngine;

namespace Dissertation.Util.Localisation
{
	public class LocManager
	{
		public static LocManager Instance { get; private set; } = null;

		private LocalisationScriptable _data;
		public LocManager(LocalisationScriptable data)
		{
			Debug.Assert(data != null);
			_data = data;
			_data.Setup();
			Debug.Assert(Instance == null);
			Instance = this;
		}

		public string GetTranslation(string key)
		{
			string translation = _data.GetTranslation(key);
			if(translation == null)
			{
				Debug.LogErrorFormat("Couldn't find translation matching key '{0}'", key);
				return string.Empty;
			}

			return translation;
		}
	}
}