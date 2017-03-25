using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Plugins.ButtonSoundsEditor.Editor
{
    public class ButtonSoundsEditor : EditorWindow
    {
        private AudioSource _audioSource;
        private AudioClip _clickSound;
        private Vector2 _scrollPosition;
        private Button _selectedButton;

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
            Button[] buttons = GetButtons();
            ButtonClickSound[] clickSounds = GetButtonClickSounds(buttons);
            _audioSource = GetFirstAudioSource(clickSounds);
            _clickSound = GetFirstClickSound(clickSounds);
        }

        private Button[] GetButtons()
        {
            return Resources.FindObjectsOfTypeAll<Button>().Where(_ => PrefabUtility.GetPrefabType(_) != PrefabType.Prefab).ToArray();
        }

        private ButtonClickSound[] GetButtonClickSounds(Button[] buttons)
        {
            return buttons.Select(_ => _.GetComponent<ButtonClickSound>()).Where(_ => _ != null).ToArray();
        }

        public void OnGUI()
        {
            Button[] buttons = GetButtons();

            GUILayout.BeginVertical();
            DrawTopPanel();
            DrawMiddlePanel(buttons);
            DrawBottomPanel(buttons);
            GUILayout.EndVertical();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        #region Top panel

        private void DrawTopPanel()
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Space(5);

            DrawAudioSourceSettings();

            _clickSound = EditorGUILayout.ObjectField("Click Sound:", _clickSound, typeof(AudioClip), false, GUILayout.Width(400)) as AudioClip;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            //if (GUILayout.Button("Apply to all", GUILayout.Width(100)))
            //{
            //    foreach (ButtonClickSound clickSound in clickSounds)
            //    {
            //        clickSound.AudioSource = _audioSource;
            //        clickSound.ClickSound = _clickSound;
            //        EditorUtility.SetDirty(clickSound);
            //    }
            //}
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.EndVertical();
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

        private void DrawAudioSourceSettings()
        {
            if (_audioSource == null)
                DrawTip("Tip: All buttons sounds are played using single AudioSource. \nAssign an existing AudioSource from the current scene or create a new AudioSource using 'Create' button!");

            GUILayout.BeginHorizontal();

            _audioSource = EditorGUILayout.ObjectField("Audio Source:", _audioSource, typeof(AudioSource), true, GUILayout.Width(400)) as AudioSource;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Create", "Create new AudioSource"), GUILayout.Width(100)))
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

        private void DrawMiddlePanel(Button[] buttons)
        {
            buttons = buttons.OrderBy(_ => GetTransformPath(_.transform)).ToArray();

            GUILayout.BeginHorizontal();
            DrawButtonsScrollView(buttons);
            DrawSelectedButtonInfoPanel();
            GUILayout.EndHorizontal();
        }

        private void DrawButtonsScrollView(Button[] buttons)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.MinWidth(500));
            foreach (Button button in buttons)
                DrawButtonSettings(button);
            GUILayout.EndScrollView();
        }

        private void DrawButtonSettings(Button button)
        {
            GUILayout.BeginHorizontal();


            GUIStyle labelStyle = EditorStyles.label;
            Color originalColor = labelStyle.normal.textColor;
            if (button == _selectedButton)
                labelStyle.normal.textColor = Color.cyan;
            
            if (GUILayout.Button(button.name, labelStyle, GUILayout.Width(125)))
                SelectButton(button);

            labelStyle.normal.textColor = originalColor;


            ButtonClickSound clickSound = button.GetComponent<ButtonClickSound>();
            if (clickSound == null)
            {
                if (GUILayout.Button(new GUIContent("Add", "Add 'ButtonClickSound' component to button."), GUILayout.Width(50)))
                {
                    AddButtonClickSound(button);
                    SelectButton(button);
                } 
            }
            else
            {
                clickSound.ClickSound = EditorGUILayout.ObjectField(clickSound.ClickSound, typeof(AudioClip), false, GUILayout.Width(200)) as AudioClip;
                if (GUILayout.Button(new GUIContent("X", "Remove 'ButtonClickSound' component from button."), GUILayout.Width(20)))
                {
                    DestroyImmediate(clickSound);
                    SelectButton(button);
                }

                if (clickSound.AudioSource == null)
                { 
                    DrawTip("Audio Source is not assigned!");
                }
                else if (clickSound.ClickSound == null)
                {
                    DrawTip("Click Sound is not assigned!");
                }
                else
                {
                    if (GUILayout.Button(new GUIContent("Play", "Test assigned AudioClip."), GUILayout.Width(50)))
                    {
                        clickSound.AudioSource.PlayOneShot(clickSound.ClickSound);
                        SelectButton(button);
                    }
                }
            }
             
            GUILayout.EndHorizontal();
        }

        private void SelectButton(Button button)
        {
            Selection.activeObject = button;
            _selectedButton = button;
        }

        private void AddButtonClickSound(Button button)
        {
            ButtonClickSound buttonClickSound = button.gameObject.AddComponent<ButtonClickSound>();
            AssignClickSound(buttonClickSound);
            EditorUtility.SetDirty(button.gameObject);
        }

        private void AssignClickSound(ButtonClickSound buttonClickSound)
        {
            buttonClickSound.AudioSource = _audioSource;
            buttonClickSound.ClickSound = _clickSound;
            EditorUtility.SetDirty(buttonClickSound);
        }

        private void DrawSelectedButtonInfoPanel()
        {
            if (_selectedButton != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(300));

                Image image = _selectedButton.GetComponent<Image>();
                if (image != null)
                    GUILayout.Box(image.sprite.texture);

                Text textComponent = _selectedButton.GetComponentInChildren<Text>();
                if (textComponent != null)
                    GUILayout.Label("Text:" + textComponent.text);

                GUILayout.Label("Path:" + GetTransformPath(_selectedButton.transform), EditorStyles.wordWrappedLabel, GUILayout.Width(300));

                GUILayout.EndVertical();
            }
        }

        #endregion

        private void DrawBottomPanel(Button[] buttons)
        {
            GUILayout.BeginHorizontal("Box");

            DrawAuthorLink();

            GUILayout.Label("Version 1.1", EditorStyles.whiteLabel);

            GUILayout.FlexibleSpace();

            if(_clickSound == null)
                GUI.enabled = false;

            if (GUILayout.Button("Add click sound to all buttons"))
            {
                foreach (Button button in buttons)
                {
                    ButtonClickSound buttonClickSound = button.GetComponent<ButtonClickSound>();
                    if(buttonClickSound == null)
                        AddButtonClickSound(button);
                    else
                        AssignClickSound(buttonClickSound);
                }
            }

            GUI.enabled = true;

            if (GUILayout.Button("Clear all buttons"))
            {
                foreach (Button button in buttons)
                {
                    ButtonClickSound buttonClickSound = button.GetComponent<ButtonClickSound>();
                    if (buttonClickSound != null)
                    {
                        DestroyImmediate(buttonClickSound);
                        EditorUtility.SetDirty(button);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private string GetTransformPath(Transform tr)
        {
            string path = tr.root.name;
            if (tr != tr.root)
                path += "/" + AnimationUtility.CalculateTransformPath(tr, tr.root);
            return path;
        }

        private void DrawTip(string message)
        {
            GUI.skin.label.normal.textColor = Color.red;
            GUILayout.Label(message);
            GUI.skin.label.normal.textColor = Color.black;
        }

        private void DrawAuthorLink()
        {
            if(GUILayout.Button("How To Use"))
            {
                Application.OpenURL("https://nubick.ru/button-sounds-editor-for-unity/?ref=editor");
            }
        }
    }
}
