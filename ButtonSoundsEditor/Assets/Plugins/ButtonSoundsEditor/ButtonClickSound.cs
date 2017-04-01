using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Plugins.ButtonSoundsEditor
{
    public class ButtonClickSound : MonoBehaviour
    {
        public AudioSource AudioSource;
        public AudioClip ClickSound;

        public void Awake()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(PlayClickSound);
            }

            EventTrigger eventTrigger = GetComponent<EventTrigger>();
            if (eventTrigger != null)
            {
                EventTrigger.Entry clickEntry = eventTrigger.triggers.SingleOrDefault(_ => _.eventID == EventTriggerType.PointerClick);
                if (clickEntry != null)
                    clickEntry.callback.AddListener(_ => PlayClickSound());
            }
        }

        private void PlayClickSound()
        {
            AudioSource.PlayOneShot(ClickSound);
        }
    }

}
