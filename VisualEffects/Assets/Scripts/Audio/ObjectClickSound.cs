// Code responsibility: play an SFX through SoundManager when a non-UI object receives a click.
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class ObjectClickSound : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string sfxId = "object_click";
    [SerializeField] private bool useOnMouseDownFallback = true;

    // IPointerClickHandler needs an EventSystem plus a GraphicRaycaster for UI objects
    // or a PhysicsRaycaster on the camera for 3D objects. 3D objects need a Collider.
    // The OnMouseDown fallback is kept for simple legacy-input object tests.
    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickSound();
    }

    private void OnMouseDown()
    {
        if (useOnMouseDownFallback)
        {
            PlayClickSound();
        }
    }

    private void PlayClickSound()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("ObjectClickSound cannot play SFX because SoundManager.Instance is null.", this);
            return;
        }

        SoundManager.Instance.PlaySfx(sfxId);
    }
}
