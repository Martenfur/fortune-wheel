using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FortuneWheel
{
	public static class State
	{
		/// <summary>
		/// Full internal state of the app.
		/// </summary>
		private static Dictionary<string, List<int>> _internalState;

		/// <summary>
		/// Map of numbers and their owners. Derived from internal state.
		/// </summary>
		private static Dictionary<int, string> _ownerMap;

		/// <summary>
		/// All available numbers in order. Derived from internal state.
		/// </summary>
		private static List<int> _numbers;
		public static IReadOnlyList<int> Numbers => _numbers;

		public static string StateFileName => Path.Combine(Environment.CurrentDirectory, "state.json");
		public static string DefaultStateFileName => Path.Combine(Environment.CurrentDirectory, "default_state.json");


		public static void ParseState(string json)
		{ 
			_internalState = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(json);
			UpdateStateCache();
		}


		private static void UpdateStateCache()
		{ 
			// Owner map.
			_ownerMap = new Dictionary<int, string>();

			foreach(var pair in _internalState)
			{
				for(var i = 0; i < pair.Value.Count; i += 1)
				{
					if (_ownerMap.ContainsKey(pair.Value[i]))
					{
						// Verifying that the state is valid.
						throw new InvalidDataException("Number " + pair.Value[i] + " occurs more than once!");
					}
					_ownerMap.Add(pair.Value[i], pair.Key);
				}
			}

			// Numbers.
			_numbers = new List<int>();
			foreach (var array in _internalState.Values)
			{
				_numbers.AddRange(array);
			}
			_numbers.Sort();
		}


		public static void SaveState()
		{
			var json = JsonConvert.SerializeObject(_internalState, Formatting.Indented);
			File.WriteAllText(StateFileName, json);
		}


		public static string GetOwner(int number)
		{
			if (!_ownerMap.ContainsKey(number))
			{
				return null;
			}
			return _ownerMap[number];
		}

		public static bool RemoveNumber(int number)
		{
			var owner = GetOwner(number);

			if (owner == null)
			{
				return false;
			}

			_internalState[owner].Remove(number);
			UpdateStateCache();

			return true;
		}
	}
}
