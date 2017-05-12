﻿using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace PW
{
	public enum PWGUIStyleType {
		PrefixLabelWidth,
		FieldWidth,
	}

	public class PWGUIStyle {
		
		public int				data;
		public PWGUIStyleType	type;

		public PWGUIStyle(int data, PWGUIStyleType type)
		{
			this.data = data;
			this.type = type;
		}

		public PWGUIStyle SliderLabelWidth(int pixels)
		{
			return new PWGUIStyle(pixels, PWGUIStyleType.PrefixLabelWidth);
		}
	}

	public class PWGUI {

		public static Rect						currentWindowRect;
		public static PWGUISettingsStorage		currentGUISettingsStorage;

		static Texture2D	ic_color;
		static Texture2D	ic_edit;
		static Texture2D	ic_settings;
		static Texture2D	colorPickerTexture;
		static Texture2D	colorPickerThumb;
		static Texture2D	settingsBackgroundTexture;
		static GUIStyle		colorPickerStyle;
		static GUIStyle		centeredLabel;

		[System.NonSerializedAttribute]
		static MethodInfo	gradientField;

		static PWGUI() {
			ic_color = Resources.Load("ic_color") as Texture2D;
			ic_edit = Resources.Load("ic_edit") as Texture2D;
			ic_settings = Resources.Load("ic_settings") as Texture2D;
			colorPickerTexture = Resources.Load("colorPicker") as Texture2D;
			colorPickerStyle = GUI.skin.FindStyle("ColorPicker");
			colorPickerThumb = Resources.Load("colorPickerThumb") as Texture2D;
			settingsBackgroundTexture = PWColorPalette.ColorToTexture(PWColorPalette.GetColor("transparentBackground"));
			centeredLabel = new GUIStyle();
			centeredLabel.alignment = TextAnchor.MiddleCenter;
			gradientField = typeof(EditorGUILayout).GetMethod(
				"GradientField",
				BindingFlags.NonPublic | BindingFlags.Static,
				null,
				new Type[] { typeof(string), typeof(Gradient), typeof(GUILayoutOption[]) },
				null
			);
		}
		
		public static void ColorPicker(string prefix, ref Color c, bool displayColorPreview = true)
		{
			Rect colorFieldRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
			ColorPicker(prefix, colorFieldRect, ref c, displayColorPreview);
		}
		
		public static void ColorPicker(ref Color c, bool displayColorPreview = true)
		{
			Rect colorFieldRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
			ColorPicker(null, colorFieldRect, ref c, displayColorPreview);
		}

		public static void ColorPicker(Rect rect, ref Color c, bool displayColorPreview = true)
		{
			ColorPicker("", rect, ref c, displayColorPreview);
		}

		public static void ColorPicker(Rect rect, ref SerializableColor c, bool displayColorPreview = true)
		{
			Color color = c;
			ColorPicker("", rect, ref color, displayColorPreview);
			c = (SerializableColor)color;
		}

		public static void ColorPicker(string prefix, Rect rect, ref SerializableColor c, bool displayColorPreview = true)
		{
			Color color = c;
			ColorPicker(prefix, rect, ref color, displayColorPreview);
			c = (SerializableColor)color;
		}
	
		public static void ColorPicker(string prefix, Rect rect, ref Color c, bool displayColorPreview = true)
		{
			var		e = Event.current;
			Rect	iconRect = rect;
			int		icColorSize = 18;

			var fieldSettings = currentGUISettingsStorage.GetSettingData(() => {
				return new PWColorPickerSettings();
			});

			if (fieldSettings.active)
			{
				if (e.type == EventType.KeyDown)
				{
					if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
					{
						fieldSettings.InActive();
						e.Use();
					}
					if (e.keyCode == KeyCode.Escape)
					{
						c = (Color)fieldSettings.InActive();
						e.Use();
					}
				}
				
				//draw the color picker window
				colorPickerStyle = GUI.skin.FindStyle("ColorPicker");
				int colorPickerWidth = 170;
				int	colorPickerHeight = 270;
				Rect colorPickerRect = new Rect(iconRect.position + new Vector2(iconRect.width + 5, 0), new Vector2(colorPickerWidth, colorPickerHeight));
				GUILayout.BeginArea(colorPickerRect, colorPickerStyle);
				{
					Rect localColorPickerRect = new Rect(Vector2.zero, new Vector2(colorPickerWidth, colorPickerHeight));
					GUILayout.Label(colorPickerTexture, GUILayout.Width(150), GUILayout.Height(150));

					Vector2 colorPickerMousePosition = e.mousePosition - new Vector2(colorPickerStyle.padding.left + 1, colorPickerStyle.padding.top + 5);

					if (colorPickerMousePosition.x >= 0 && colorPickerMousePosition.y >= 0 && colorPickerMousePosition.x <= 150 && colorPickerMousePosition.y <= 150)
					{
						if (e.isMouse)
						{
							Vector2 textureCoord = colorPickerMousePosition * (colorPickerTexture.width / 150f);
							textureCoord.y = colorPickerTexture.height - textureCoord.y;
							c = colorPickerTexture.GetPixel((int)textureCoord.x, (int)textureCoord.y);
							fieldSettings.thumbPosition = colorPickerMousePosition + new Vector2(6, 9);
						}
					}

					Rect colorPickerThumbRect = new Rect(fieldSettings.thumbPosition, new Vector2(8, 8));
					GUI.DrawTexture(colorPickerThumbRect, colorPickerThumb);

					byte r, g, b, a;
					PWColorPalette.ColorToByte(c, out r, out g, out b, out a);
					EditorGUIUtility.labelWidth = 20;
					r = (byte)EditorGUILayout.IntSlider("R", r, 0, 255);
					g = (byte)EditorGUILayout.IntSlider("G", g, 0, 255);
					b = (byte)EditorGUILayout.IntSlider("B", b, 0, 255);
					a = (byte)EditorGUILayout.IntSlider("A", a, 0, 255);
					c = PWColorPalette.ByteToColor(r, g, b, a);
					EditorGUIUtility.labelWidth = 0;

					EditorGUILayout.Space();

					//hex field
					int hex = PWColorPalette.ColorToHex(c, false); //get color without alpha
					EditorGUIUtility.labelWidth = 80;
					EditorGUI.BeginChangeCheck();
					string hexColor = EditorGUILayout.TextField("Hex color", hex.ToString("X6"));
					if (EditorGUI.EndChangeCheck())
						a = 255;
					EditorGUIUtility.labelWidth = 0;
					Regex reg = new Regex(@"[^A-F0-9 -]");
					hexColor = reg.Replace(hexColor, "");
					hexColor = hexColor.Substring(0, Mathf.Min(hexColor.Length, 6));
					if (hexColor == "")
						hexColor = "0";
					hex = int.Parse(a.ToString("X2") + hexColor, System.Globalization.NumberStyles.HexNumber);
					c = PWColorPalette.HexToColor(hex, false);

					if (e.isMouse && localColorPickerRect.Contains(e.mousePosition))
						e.Use();
				}
				GUILayout.EndArea();
			}
			
			//draw the icon
			Rect colorPreviewRect = iconRect;
			if (displayColorPreview)
			{
				int width = (int)rect.width;
				int colorPreviewPadding = 5;
				
				Vector2 prefixSize = Vector2.zero;
				if (!String.IsNullOrEmpty(prefix))
				{
					prefixSize = GUI.skin.label.CalcSize(new GUIContent(prefix));
					prefixSize.x += 5; //padding of 5 pixels
					colorPreviewRect.position += new Vector2(prefixSize.x, 0);
					Rect prefixRect = new Rect(iconRect.position, prefixSize);
					GUI.Label(prefixRect, prefix);
				}
				colorPreviewRect.size = new Vector2(width - icColorSize - prefixSize.x - colorPreviewPadding, 16);
				iconRect.position += new Vector2(colorPreviewRect.width + prefixSize.x + colorPreviewPadding, 0);
				iconRect.size = new Vector2(icColorSize, icColorSize);
				EditorGUIUtility.DrawColorSwatch(colorPreviewRect, c);
			}
			

			//actions if clicked on/outside of the icon
			GUI.DrawTexture(iconRect, ic_color);
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (iconRect.Contains(e.mousePosition) || colorPreviewRect.Contains(e.mousePosition))
				{
					fieldSettings.Active(c);
					e.Use();
				}
				else if (fieldSettings.active)
				{
					fieldSettings.InActive();
					e.Use();
				}
			}
		}
		
		public static void TextField(string prefix, ref string text, bool editable = false, GUIStyle textStyle = null)
		{
			TextField(prefix, EditorGUILayout.GetControlRect().position, ref text, editable, textStyle);
		}

		public static void TextField(ref string text, bool editable = false, GUIStyle textStyle = null)
		{
			TextField(null, EditorGUILayout.GetControlRect().position, ref text, editable, textStyle);
		}

		public static void TextField(Vector2 position, ref string text, bool editable = false, GUIStyle textStyle = null)
		{
			TextField(null, position, ref text, editable, textStyle);
		}

		public static void TextField(string prefix, Vector2 textPosition, ref string text, bool editable = false, GUIStyle textFieldStyle = null)
		{
			Rect	textRect = new Rect(textPosition, Vector2.zero);
			var		e = Event.current;

			string controlName = "textfield-" + text.GetHashCode().ToString();

			var fieldSettings = currentGUISettingsStorage.GetSettingData(() => {
				return new PWTextSettings();
			});
			
			Vector2 nameSize = textFieldStyle.CalcSize(new GUIContent(text));
			textRect.size = nameSize;

			if (!String.IsNullOrEmpty(prefix))
			{
				Vector2 prefixSize = textFieldStyle.CalcSize(new GUIContent(prefix));
				Rect prefixRect = textRect;

				textRect.position += new Vector2(prefixSize.x, 0);
				prefixRect.size = prefixSize;
				GUI.Label(prefixRect, prefix);
			}
			
			Rect iconRect = new Rect(textRect.position + new Vector2(nameSize.x + 10, 0), new Vector2(17, 17));
			bool editClickIn = (editable && e.type == EventType.MouseDown && e.button == 0 && iconRect.Contains(e.mousePosition));

			if (editClickIn)
				fieldSettings.Invert(text);
			
			if (editable)
			{
				GUI.color = (fieldSettings.active) ? PWColorPalette.GetColor("selected") : Color.white;
				GUI.DrawTexture(iconRect, ic_edit);
				GUI.color = Color.white;
			}

			if (fieldSettings.active == true)
			{
				Color oldCursorColor = GUI.skin.settings.cursorColor;
				GUI.skin.settings.cursorColor = Color.white;
				GUI.SetNextControlName(controlName);
				text = GUI.TextField(textRect, text, textFieldStyle);
				GUI.skin.settings.cursorColor = oldCursorColor;
				if (e.isKey && fieldSettings.active)
				{
					if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
					{
						fieldSettings.InActive();
						e.Use();
					}
					else if (e.keyCode == KeyCode.Escape)
					{
						text = (string)fieldSettings.InActive();
						e.Use();
					}
				}
			}
			else
				GUI.Label(textRect, text, textFieldStyle);			
			
			bool editClickOut = (editable && e.type == EventType.MouseDown && e.button == 0 && !iconRect.Contains(e.mousePosition));

			if (editClickOut && fieldSettings.active)
			{
				fieldSettings.InActive();
				e.Use();
			}

			if (editClickIn)
			{
				GUI.FocusControl(controlName);
				var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				te.SelectAll();
				e.Use();
			}
		}
		
		public static void Slider(ref float value, float min, float max, float step = 0.01f, params PWGUIStyle[] styles)
		{
			Slider(null, ref value, ref min, ref max, step, false, false, styles);
		}

		public static void Slider(string name, ref float value, float min, float max, float step = 0.01f, params PWGUIStyle[] styles)
		{
			Slider(name, ref value, ref min, ref max, step, false, false, styles);
		}
	
		public static void Slider(string name, ref float value, ref float min, ref float max, float step = 0.01f, bool editableMin = true, bool editableMax = true, params PWGUIStyle[] styles)
		{
			int		sliderLabelWidth = 30;

			foreach (var style in styles)
				if (style.type == PWGUIStyleType.PrefixLabelWidth)
					sliderLabelWidth = style.data;

			if (name == null)
				name = "";

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUI.BeginDisabledGroup(!editableMin);
					min = EditorGUILayout.FloatField(min, GUILayout.Width(sliderLabelWidth));
				EditorGUI.EndDisabledGroup();
				
				if (step != 0)
				{
					float m = 1 / step;
					value = Mathf.Round(GUILayout.HorizontalSlider(value, min, max) * m) / m;
				}
				else
					value = GUILayout.HorizontalSlider(value, min, max);

				EditorGUI.BeginDisabledGroup(!editableMax);
					max = EditorGUILayout.FloatField(max, GUILayout.Width(sliderLabelWidth));
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndHorizontal();
			
			GUILayout.Space(-4);
			GUILayout.Label(name + value.ToString(), centeredLabel);
		}
		
		public static void IntSlider(ref int value, int min, int max, int step = 1, params PWGUIStyle[] styles)
		{
			IntSlider(null, ref value, ref min, ref max, step, false, false, styles);
		}

		public static void IntSlider(string name, ref int value, int min, int max, int step = 1, params PWGUIStyle[] styles)
		{
			IntSlider(name, ref value, ref min, ref max, step, false, false, styles);
		}
	
		public static void IntSlider(string name, ref int value, ref int min, ref int max, int step = 1, bool editableMin = true, bool editableMax = true, params PWGUIStyle[] styles)
		{
			float		v = value;
			float		m_min = min;
			float		m_max = max;
			Slider(name, ref v, ref m_min, ref m_max, step, editableMin, editableMax, styles);
			value = (int)v;
			min = (int)m_min;
			max = (int)m_max;
		}

		public static void TexturePreview(Texture tex, bool settings = true)
		{
			Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(0));
			previewRect.size = (currentWindowRect.width - 20 - 10) * Vector2.one;
			TexturePreview(previewRect, tex, settings);
			GUILayout.Space(previewRect.width);
		}

		public static void TexturePreview(Rect previewRect, Texture tex, bool settings = true)
		{
			var e = Event.current;

			//create or load texture settings
			var fieldSettings = currentGUISettingsStorage.GetSettingData(() => {
				var state = new PWTextureSettings();
				state.filterMode = FilterMode.Bilinear;
				state.scaleMode = ScaleMode.ScaleToFit;
				state.scaleAspect = 1;
				state.material = null;
				return state;
			});

			EditorGUI.DrawPreviewTexture(previewRect, tex, fieldSettings.material, fieldSettings.scaleMode, fieldSettings.scaleAspect);

			if (!settings)
				return ;

			//render the texture settings window
			if (fieldSettings.active)
			{
				if (e.type == EventType.KeyDown)
					if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
					{
						fieldSettings.InActive();
						e.Use();
					}
				
				Rect settingsRect = EditorGUILayout.BeginVertical(GUILayout.Width(previewRect.width - 20));
				{
					FilterMode	newMode;

					GUI.DrawTexture(settingsRect, settingsBackgroundTexture);

					EditorGUIUtility.labelWidth = 80;
					EditorGUI.BeginChangeCheck();
						newMode = (FilterMode)EditorGUILayout.EnumPopup("filter mode", tex.filterMode);
					if (EditorGUI.EndChangeCheck())
						tex.filterMode = newMode;
					fieldSettings.scaleMode = (ScaleMode)EditorGUILayout.EnumPopup("scale mode", fieldSettings.scaleMode);
					fieldSettings.scaleAspect= EditorGUILayout.FloatField("scale aspect", fieldSettings.scaleAspect);
					fieldSettings.material = (Material)EditorGUILayout.ObjectField("material", fieldSettings.material, typeof(Material), false);
					EditorGUIUtility.labelWidth = 0;
				}
				EditorGUILayout.EndVertical();
				GUILayout.Space(-74); //settingsRect height
			}

			int		icSettingsSize = 16;
			Rect	icSettingsRect = new Rect(previewRect.x + previewRect.width - icSettingsSize, previewRect.y, icSettingsSize, icSettingsSize);
			GUI.DrawTexture(icSettingsRect, ic_settings);
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (icSettingsRect.Contains(e.mousePosition))
				{
					fieldSettings.Invert(0);
					e.Use();
				}
				else if (fieldSettings.active)
				{
					fieldSettings.InActive();
					e.Use();
				}
			}
		}
		
		public static void Sampler2DPreview(Sampler2D samp, bool update, bool settings = true)
		{
			Sampler2DPreview(null, samp, update, settings);
		}
		
		public static void Sampler2DPreview(string prefix, Sampler2D samp, bool update, bool settings = true)
		{
			int previewSize = (int)currentWindowRect.width - 20 - 20; //padding + texture margin
			var e = Event.current;

			if (!String.IsNullOrEmpty(prefix))
				EditorGUILayout.LabelField(prefix);

			var fieldSettings = currentGUISettingsStorage.GetSettingData(() => {
				var state = new PWSampler2DSettings();
				state.filterMode = FilterMode.Bilinear;
				state.gradient = new SerializableGradient(
					PWUtils.CreateGradient(
						new KeyValuePair< float, Color >(0, Color.black),
						new KeyValuePair< float, Color >(1, Color.white)
					)
				);
				state.texture = new Texture2D(previewSize, previewSize, TextureFormat.RGBA32, false);
				return state;
			});

			Texture2D	tex = fieldSettings.texture as Texture2D;
			Gradient	gradient = fieldSettings.gradient;
			

			if (samp.size != tex.width)
				tex.Resize(samp.size, samp.size, TextureFormat.RGBA32, false);

			Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(0));
			
			TexturePreview(tex, false);
			
			//draw the settings window
			if (settings && fieldSettings.active)
			{
				EditorGUILayout.BeginVertical();
				{
					EditorGUI.BeginChangeCheck();
					fieldSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup(fieldSettings.filterMode);
					if (EditorGUI.EndChangeCheck())
						tex.filterMode = fieldSettings.filterMode;
					gradient = (Gradient)gradientField.Invoke(null, new object[] {"", gradient, null});
					if (!gradient.Compare(fieldSettings.serializableGradient))
						update = true;
					fieldSettings.serializableGradient = (SerializableGradient)gradient;
				}
				EditorGUILayout.EndVertical();
				
				if (e.type == EventType.KeyDown && fieldSettings.active)
				{
					if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
					{
						fieldSettings.InActive();
						e.Use();
					}
				}
			}
			
			//draw the setting icon and manage his events
			int icSettingsSize = 16;
			int	icSettingsPadding = 4;
			Rect icSettingsRect = new Rect(previewRect.x + previewRect.width - icSettingsSize - icSettingsPadding, previewRect.y + icSettingsPadding, icSettingsSize, icSettingsSize);

			GUI.DrawTexture(icSettingsRect, ic_settings);
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (icSettingsRect.Contains(e.mousePosition))
				{
					fieldSettings.Invert(null);
					e.Use();
				}
				else if (fieldSettings.active)
				{
					fieldSettings.InActive();
					e.Use();
				}
			}

			//update the texture with the gradient
			if (update)
			{
				Debug.Log("update texture !");
				samp.Foreach((x, y, val) => {
					tex.SetPixel(x, y, gradient.Evaluate(Mathf.Clamp01(val)));
				});
				tex.Apply();
			}
		}
		
		public static void ObjectPreview(object obj, bool update)
		{
			ObjectPreview(null, obj, update);
		}

		public static void ObjectPreview(string name, object obj, bool update)
		{
			Type objType = obj.GetType();

			if (objType == typeof(Sampler2D))
				Sampler2DPreview(name, obj as Sampler2D, update);
			else if (obj.GetType().IsInstanceOfType(typeof(Object)))
			{
				//unity object preview
			}
			else
				Debug.LogWarning("can't preview the object of type: " + obj.GetType());
		}
	}
}