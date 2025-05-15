using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audoty
{
    /// <summary>
    /// Plays an AudioPlayer when receives OnPointerClick callback.
    /// It will not play the AudioPlayer if it's attached to a Selectable component which is not interactable or disabled. 
    /// </summary>
    public class PlayAudioOnClick : ScenePlayerBase, IPointerClickHandler, IPointerDownHandler
    {
        private Selectable _selectable;
        private bool _play;

        protected override void Awake()
        {
            base.Awake();
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_play) return;
            
            if (!IsAudioPlayerReady)
            {
                Debug.LogWarning($"AudioPlayer was not ready. Could not play audio on click of {gameObject.name}");
                return;
            }

            if (AudioPlayerToUse == null)
            {
                Debug.LogError("PlayAudioOnClick does not have AudioPlayer assigned.", this);
                return;
            }

            if (AudioPlayerToUse.Clips.Count == 0)
                return;

            int index = UseRandomClip ? Random.Range(0, AudioPlayerToUse.Clips.Count) : ClipIndex;

            if (index == -1)
            {
                Debug.LogError("No clip is selected in PlayAudioOnClick", this);
                return;
            }

            AudioPlayerToUse.Play(index);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _play = _selectable == null || _selectable.interactable && _selectable.enabled;
        }
    }
}