using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UiButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private UiButtonSound clickSound = UiButtonSound.Auto;
    private Button button;

    private void Awake() => Bind(GetComponent<Button>());

    public void Bind(Button target, UiButtonSound sound = UiButtonSound.Auto)
    {
        // Scene rescans must not replace an explicitly assigned Back/Select override.
        if (sound != UiButtonSound.Auto) clickSound = sound;
        if (button == target) return;
        if (button != null) button.onClick.RemoveListener(HandleClick);
        button = target;
        if (button != null) button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(HandleClick);
    }

    public void OnPointerEnter(PointerEventData eventData) => PlaySelection();
    public void OnSelect(BaseEventData eventData) => PlaySelection();

    private void PlaySelection()
    {
        if (!isActiveAndEnabled || button == null || !button.IsActive() || !button.IsInteractable()) return;
        UiAudioRouter.Queue(UiButtonSound.Select);
    }

    private void HandleClick()
    {
        // Button.Press already validated interactability. Its earlier listeners may now
        // have disabled this button/panel or started a scene load; still play the click.
        if (HasManualAudioListener()) return;
        UiButtonSound sound = clickSound;
        if (sound == UiButtonSound.Auto)
        {
            sound = UiAudioRouter.IsBackAction(button.name) ? UiButtonSound.Back : UiButtonSound.Confirm;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (UiAudioRouter.IsBackAction(button.onClick.GetPersistentMethodName(i))) sound = UiButtonSound.Back;
        }
        UiAudioRouter.Queue(sound, true);
    }

    private bool HasManualAudioListener()
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentListenerState(i) == UnityEventCallState.Off) continue;
            Object target = button.onClick.GetPersistentTarget(i);
            if (!(target is FishingAudioFeedback) && !(target is UiAudioRouter)) continue;
            string method = button.onClick.GetPersistentMethodName(i);
            if (method == "PlayUiSelect" || method == "PlayUiConfirm" || method == "PlayUiBack") return true;
        }
        return false;
    }
}
