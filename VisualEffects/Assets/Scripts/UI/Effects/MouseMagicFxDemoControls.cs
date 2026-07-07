// 코드 책임: MouseMagicFx 테스트 장면의 버튼 상태 표시와 FX 토글 동작을 담당한다.
using UnityEngine;
using UnityEngine.UI;

public sealed class MouseMagicFxDemoControls : MonoBehaviour
{
    [SerializeField] private MouseMagicFxController fxController;
    [SerializeField] private Text statusText;
    [SerializeField] private Text clickCountText;
    [SerializeField] private Text toggleButtonText;

    private int clickCount;
    private bool fxEnabled = true;

    private void Awake()
    {
        RefreshLabels("Ready. Move the mouse or left click over this document.");
    }

    public void RegisterClick()
    {
        clickCount++;
        RefreshLabels("Click Test Button received input. FX layer did not block the UI.");
    }

    public void ToggleFx()
    {
        fxEnabled = !fxEnabled;
        if (fxController != null)
        {
            fxController.SetEnabled(fxEnabled);
        }

        RefreshLabels(fxEnabled ? "Mouse Magic FX enabled." : "Mouse Magic FX disabled.");
    }

    private void RefreshLabels(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }

        if (clickCountText != null)
        {
            clickCountText.text = "Button clicks: " + clickCount;
        }

        if (toggleButtonText != null)
        {
            toggleButtonText.text = fxEnabled ? "Disable FX" : "Enable FX";
        }
    }
}
