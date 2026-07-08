// Code responsibility: automatically play an SFX through SoundManager when a Unity UI Button is clicked.
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIButtonSound : MonoBehaviour
{
    [SerializeField] private string sfxId = "button_click";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning("UIButtonSound requires a Button component.", this);
            return;
        }

        RegisterClickHandler();
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        RegisterClickHandler();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }

    private void RegisterClickHandler()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(PlayClickSound);
        button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("UIButtonSound cannot play SFX because SoundManager.Instance is null.", this);
            return;
        }

        SoundManager.Instance.PlaySfx(sfxId);
    }
}
