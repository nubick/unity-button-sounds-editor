using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Plugins.ButtonSoundsEditor.Editor
{
	public class ButtonSoundsEditor : EditorWindow, IComparer<GameObject>
	{
		private const string AllClickSoundFilter = "All";
		private const string NotAssignedClickSoundFilter = "Not Assigned";

		private Dictionary<CandidatesTypeFilter, List<GameObject>> _candidates;
		private GameObject _selectedCandidate;
		private CandidatesTypeFilter _candidatesTypeFilter;
		private string _selectedClickSoundFilter = AllClickSoundFilter;

		private AudioSource _audioSource;
		private AudioClip _clickSound;
		private Vector2 _scrollPosition;

		private List<GameObject> All { get { return _candidates[CandidatesTypeFilter.All]; } }

		#region Initialization

		public ButtonSoundsEditor()
		{
			_candidates = new Dictionary<CandidatesTypeFilter, List<GameObject>>();
			_candidates.Add(CandidatesTypeFilter.All, new List<GameObject>());
			_candidates.Add(CandidatesTypeFilter.Buttons, new List<GameObject>());
			_candidates.Add(CandidatesTypeFilter.Toggles, new List<GameObject>());
			_candidates.Add(CandidatesTypeFilter.EventTriggers, new List<GameObject>());
		}

		[MenuItem("Window/Utils/Button sounds editor")]
		public static void OpenEditor()
		{
			ButtonSoundsEditor window = GetWindow<ButtonSoundsEditor>();
			window.titleContent = new GUIContent("Button sounds editor");
			window.Initialize();
			window.Show();
		}

		private void Initialize()
		{
			RefreshCandidates();
			ButtonClickSound[] clickSounds = GetAllSoundComponents(All);
			_audioSource = GetFirstAudioSource(clickSounds);
			_clickSound = GetFirstClickSound(clickSounds);
		}

		private ButtonClickSound[] GetAllSoundComponents(List<GameObject> candidates)
		{
			return candidates.Select(_ => _.GetComponent<ButtonClickSound>()).Where(_ => _ != null).ToArray();
		}

		private AudioSource GetFirstAudioSource(ButtonClickSound[] clickSounds)
		{
			ButtonClickSound buttonClickSound = clickSounds.FirstOrDefault(_ => _.AudioSource != null);
			return buttonClickSound == null ? null : buttonClickSound.AudioSource;
		}

		private AudioClip GetFirstClickSound(ButtonClickSound[] clickSounds)
		{
			ButtonClickSound buttonClickSound = clickSounds.FirstOrDefault(_ => _.ClickSound != null);
			return buttonClickSound == null ? null : buttonClickSound.ClickSound;
		}

		#endregion

		private void RefreshCandidates()
		{
			_candidates.Values.ToList().ForEach(_ => _.Clear());

			var buttons = Resources.FindObjectsOfTypeAll<Button>().Where(_ => PrefabUtility.GetPrefabType(_) != PrefabType.Prefab).Select(_ => _.gameObject).ToList();
			_candidates[CandidatesTypeFilter.Buttons].AddRange(buttons);
			_candidates[CandidatesTypeFilter.All].AddRange(buttons);

			var eventTriggers = Resources.FindObjectsOfTypeAll<EventTrigger>().Where(_ => PrefabUtility.GetPrefabType(_) != PrefabType.Prefab &&
							_.triggers.Any(e => e.eventID == EventTriggerType.PointerClick)).Select(_ => _.gameObject).ToList();
			_candidates[CandidatesTypeFilter.EventTriggers].AddRange(eventTriggers);
			_candidates[CandidatesTypeFilter.All].AddRange(eventTriggers);

			var toggles = Resources.FindObjectsOfTypeAll<Toggle>().Where(_ => PrefabUtility.GetPrefabType(_) != PrefabType.Prefab).Select(_ => _.gameObject).ToList();
			_candidates[CandidatesTypeFilter.Toggles].AddRange(toggles);
			_candidates[CandidatesTypeFilter.All].AddRange(toggles);

			_candidates.Values.ToList().ForEach(_ => _.Sort(this));
		}

		private string GetTransformPath(Transform tr)
		{
			string path = tr.root.name;
			if (tr != tr.root)
				path += "/" + AnimationUtility.CalculateTransformPath(tr, tr.root);
			return path;
		}

		public int Compare(GameObject x, GameObject y)
		{
			return GetTransformPath(x.transform).CompareTo(GetTransformPath(y.transform));
		}

		public void OnGUI()
		{
			RefreshCandidates();

			GUILayout.BeginVertical();
			DrawTopPanel();
			DrawMiddlePanel();
			DrawBottomPanel();
			GUILayout.EndVertical();

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}

		#region Filtering

        private void DrawFilterPanels()
		{
            DrawCandidatesTypeFilterPanel();
			DrawSoundsFilterPanel();
		}

        private void DrawCandidatesTypeFilterPanel()
        {
			bool isNeedFilter = _candidates.Values.Count(_ => _.Any()) > 2;
			if (isNeedFilter)
			{
				GUILayout.BeginHorizontal();
				foreach (CandidatesTypeFilter filter in _candidates.Keys)
				{
					if (_candidates[filter].Any())
						if (GUILayout.Toggle(_candidatesTypeFilter == filter, filter.ToString(), EditorStyles.toolbarButton, GUILayout.Width(100f)))
							_candidatesTypeFilter = filter;
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(5f);
			}
			else
			{
				_candidatesTypeFilter = CandidatesTypeFilter.All;
			}
		}

		private void DrawSoundsFilterPanel()
		{
			GUILayout.BeginHorizontal();
			ButtonClickSound[] soundComponents = GetAllSoundComponents(_candidates[_candidatesTypeFilter]);
			List<string> soundFilterNames = soundComponents.Where(_ => _.ClickSound != null).Select(_ => _.ClickSound.name).Distinct().ToList();
			soundFilterNames.Insert(0, NotAssignedClickSoundFilter);
			soundFilterNames.Insert(0, AllClickSoundFilter);
			foreach (string soundNameFilter in soundFilterNames)
			{
				string tabName = soundNameFilter.Substring(0, Mathf.Min(soundNameFilter.Length, 18));
				if (GUILayout.Toggle(_selectedClickSoundFilter == soundNameFilter, tabName, EditorStyles.toolbarButton, GUILayout.Width(100f)))
					_selectedClickSoundFilter = soundNameFilter;
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5f);
		}

		private List<GameObject> GetFilteredCandidates()
		{
			List<GameObject> candidates = _candidates[_candidatesTypeFilter];

			if (_selectedClickSoundFilter == AllClickSoundFilter)
				return candidates;

			List<GameObject> filteredCandidates = new List<GameObject>();
			foreach (GameObject candidate in candidates)
			{
				ButtonClickSound clickSound = candidate.GetComponent<ButtonClickSound>();
				bool isNotAssigned = (clickSound == null || clickSound.ClickSound == null) && _selectedClickSoundFilter == NotAssignedClickSoundFilter;
				bool isSoundNameEqual = clickSound != null && clickSound.ClickSound != null && clickSound.ClickSound.name == _selectedClickSoundFilter;
				if (isNotAssigned || isSoundNameEqual)
					filteredCandidates.Add(candidate);
			}
			return filteredCandidates;
		}

		#endregion

		#region Top panel

		private void DrawTopPanel()
		{
			GUILayout.BeginVertical("Box");
			GUILayout.Space(5);

			DrawAudioSourceSettings();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Click sound", GUILayout.Width(120));
			_clickSound = EditorGUILayout.ObjectField(_clickSound, typeof(AudioClip), false, GUILayout.Width(200)) as AudioClip;

			bool isEnabled = _audioSource != null && _clickSound != null;
			EditorGUI.BeginDisabledGroup(!isEnabled);
			GUILayout.Space(25f);
			if (GUILayout.Button(new GUIContent("Play", "Test assigned AudioClip."), GUILayout.Width(50)))
				_audioSource.PlayOneShot(_clickSound);
			EditorGUI.EndDisabledGroup();

			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.EndVertical();
		}

		private void DrawAudioSourceSettings()
		{
			if (_audioSource == null)
				DrawTip("Tip: All buttons sounds are played using single AudioSource. \nAssign an existing AudioSource from the current scene or create a new AudioSource using 'Create' button!");

			GUILayout.BeginHorizontal();

			GUILayout.Label("Audio source", GUILayout.Width(120));
			_audioSource = EditorGUILayout.ObjectField(_audioSource, typeof(AudioSource), true, GUILayout.Width(200)) as AudioSource;

			GUILayout.Space(25f);
			if (GUILayout.Button(new GUIContent("Create", "Create new AudioSource"), GUILayout.Width(50)))
			{
				GameObject go = new GameObject("ButtonsAudioSource");
				AudioSource audioSource = go.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				_audioSource = audioSource;
				Selection.activeGameObject = go;
			}

			GUILayout.EndHorizontal();
		}

		#endregion

		#region Middle panel

		private void DrawMiddlePanel()
		{
			GUILayout.BeginVertical();
			DrawFilterPanels();
			GUILayout.BeginHorizontal();
			DrawButtonsScrollView();
			DrawSelectedButtonInfoPanel();
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
		}

		private void DrawButtonsScrollView()
		{
			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.MinWidth(440));
			foreach (GameObject candidate in GetFilteredCandidates())
			{
				DrawButtonSettings(candidate);
				DrawButtonSettingsNotificationLine(candidate);
			}
			GUILayout.EndScrollView();
		}

		private void DrawButtonSettings(GameObject candidate)
		{
			GUILayout.BeginHorizontal();

			MarkSelectedCandidate(candidate);

			ButtonClickSound clickSound = candidate.GetComponent<ButtonClickSound>();
			if (clickSound == null)
			{
				GUILayout.Label("No any click sound!", EditorStyles.whiteLabel, GUILayout.Width(225));

				if (GUILayout.Button(new GUIContent("Add", "Add 'ButtonClickSound' component to button."), GUILayout.Width(50)))
				{
					AddButtonClickSound(candidate);
					SelectButton(candidate);
				}
			}
			else
			{
				clickSound.ClickSound = EditorGUILayout.ObjectField(clickSound.ClickSound, typeof(AudioClip), false, GUILayout.Width(200)) as AudioClip;
				if (GUILayout.Button(new GUIContent("X", "Remove 'ButtonClickSound' component from button."), GUILayout.Width(20)))
				{
					DestroyImmediate(clickSound);
					SelectButton(candidate);
				}

				bool hasErrors = clickSound.AudioSource == null || clickSound.ClickSound == null;
				if (hasErrors)
				{
					if (GUILayout.Button("Fix", GUILayout.Width(50)))
					{
						if (clickSound.AudioSource == null)
							clickSound.AudioSource = _audioSource;

						if (clickSound.ClickSound == null)
							clickSound.ClickSound = _clickSound;

						EditorUtility.SetDirty(clickSound);
					}
				}
				else
				{
					if (GUILayout.Button(new GUIContent("Play", "Test assigned AudioClip."), GUILayout.Width(50)))
					{
						clickSound.AudioSource.PlayOneShot(clickSound.ClickSound);
						SelectButton(candidate);
					}
				}
			}

			GUILayout.EndHorizontal();
		}

		private void MarkSelectedCandidate(GameObject candidate)
		{
			GUIStyle labelStyle = EditorStyles.label;
			Color originalColor = labelStyle.normal.textColor;
			if (candidate == _selectedCandidate)
				labelStyle.normal.textColor = new Color(0f, 0.5f, 0.5f);

			if (GUILayout.Button(candidate.name, labelStyle, GUILayout.Width(125)))
				SelectButton(candidate);

			labelStyle.normal.textColor = originalColor;
		}

		private void DrawButtonSettingsNotificationLine(GameObject candidate)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(135f);
			ButtonClickSound clickSound = candidate.GetComponent<ButtonClickSound>();
			if (clickSound != null)
			{
				if (clickSound.AudioSource == null)
				{
					DrawTip("Audio Source is not assigned!");
				}
				else if (clickSound.ClickSound == null)
				{
					DrawTip("Click Sound is not assigned!");
				}
			}
			GUILayout.EndHorizontal();
		}

		private void AddButtonClickSound(GameObject candidate)
		{
			ButtonClickSound buttonClickSound = candidate.AddComponent<ButtonClickSound>();
			AssignClickSound(buttonClickSound);
			EditorUtility.SetDirty(candidate);
		}

		private void AssignClickSound(ButtonClickSound buttonClickSound)
		{
			buttonClickSound.AudioSource = _audioSource;
			buttonClickSound.ClickSound = _clickSound;
			EditorUtility.SetDirty(buttonClickSound);
		}

		private void DrawSelectedButtonInfoPanel()
		{
			if (_selectedCandidate != null)
			{
				GUILayout.BeginVertical();

				Text textComponent = _selectedCandidate.GetComponentInChildren<Text>();
				if (textComponent != null)
					GUILayout.Label(string.Format("Text: '{0}'", textComponent.text));

				GUILayout.Label("Path:" + GetTransformPath(_selectedCandidate.transform), EditorStyles.wordWrappedLabel, GUILayout.Width(300));

				Texture previewTexture = GetPreviewTexture(_selectedCandidate);
				if (previewTexture != null)
					GUILayout.Box(previewTexture,
								  GUILayout.Width(Mathf.Min(previewTexture.width, 300)),
								  GUILayout.Height(Mathf.Min(previewTexture.height, 300)));

				GUILayout.EndVertical();
			}
		}

		private Texture GetPreviewTexture(GameObject selectedCandidate)
		{
			Image image = _selectedCandidate.GetComponentInChildren<Image>();
			if (image != null && image.sprite != null)
				return image.sprite.texture;

			RawImage rawImage = _selectedCandidate.GetComponentInChildren<RawImage>();
			if (rawImage != null)
				return rawImage.texture;

			return null;
		}

		#endregion

		#region Bottom panel

		private void DrawBottomPanel()
		{
			GUILayout.BeginHorizontal("Box");

			DrawAuthorLink();

			GUILayout.Label("Version 1.2");

			GUILayout.FlexibleSpace();

			EditorGUI.BeginDisabledGroup(_clickSound == null);

			if (GUILayout.Button("Add click sound to ALL"))
			{
				foreach (GameObject candidate in All)
				{
					ButtonClickSound buttonClickSound = candidate.GetComponent<ButtonClickSound>();
					if (buttonClickSound == null)
						AddButtonClickSound(candidate);
					else
						AssignClickSound(buttonClickSound);
				}
			}

			EditorGUI.EndDisabledGroup();

			if (GUILayout.Button("Clear ALL"))
			{
				foreach (GameObject candidate in All)
				{
					ButtonClickSound buttonClickSound = candidate.GetComponent<ButtonClickSound>();
					if (buttonClickSound != null)
					{
						DestroyImmediate(buttonClickSound);
						EditorUtility.SetDirty(candidate);
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawTip(string message)
		{
			Color storedColor = GUI.skin.label.normal.textColor;
			GUI.skin.label.normal.textColor = Color.red;
			GUILayout.Label(message);
			GUI.skin.label.normal.textColor = storedColor;
		}

		private void DrawAuthorLink()
		{
			if (GUILayout.Button("How To Use"))
			{
				Application.OpenURL("https://nubick.ru/button-sounds-editor-for-unity/?ref=editor");
			}
		}

		#endregion

		#region Selection

		public void OnEnable()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		public void OnDisable()
		{
			Selection.selectionChanged -= OnSelectionChanged;
		}

		private void SelectButton(GameObject candidate)
		{
			_selectedCandidate = candidate;
			Selection.activeGameObject = candidate;
		}

		private void OnSelectionChanged()
		{
			if (Selection.activeGameObject != _selectedCandidate && All.Contains(Selection.activeGameObject))
				_selectedCandidate = Selection.activeGameObject;
		}

		#endregion

		private enum CandidatesTypeFilter
		{
			All,
			Buttons,
			EventTriggers,
			Toggles
		}
	}
}
