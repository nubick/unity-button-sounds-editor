using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Plugins.ButtonSoundsEditor
{
    public class ButtonClickSound : MonoBehaviour, IPointerClickHandler
    {
        public AudioSource AudioSource;
        public AudioClip ClickSound;

        public void OnPointerClick(PointerEventData eventData)
        {
            PlayClickSound();
        }

        private void PlayClickSound()
        {
            AudioSource.PlayOneShot(ClickSound);
        }
    }

}
