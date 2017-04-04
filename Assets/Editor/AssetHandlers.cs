﻿using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using PW;

public class NewBehaviourScript {

    [OnOpenAssetAttribute(1)]
	public static bool OnOpenAssetAttribute(int instanceId, int line)
	{
		Object instance = EditorUtility.InstanceIDToObject(instanceId);

		if (instance.GetType() == typeof(PWNodeGraph))
		{
			//open PWNodeGraph window:
			ProceduralWorldsWindow window = (ProceduralWorldsWindow)EditorWindow.GetWindow(typeof(ProceduralWorldsWindow));
			window.currentGraph = instance as PWNodeGraph;
			Debug.Log(instance.name + " opened");
		}
		return false;
	}
}
