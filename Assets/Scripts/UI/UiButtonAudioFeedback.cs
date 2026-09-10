using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UiButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    [SerializeField] private UiButtonSound clickSound = UiButtonSound.Auto;
    private Button button;
    private readonly HashSet<int> hoveringPointers = new();

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

    private void OnDisable() => hoveringPointers.Clear();

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Touch has no hover: a finger landing must not play Select before the release click.
        if (eventData is ExtendedPointerEventData pointer
            ? pointer.pointerType == UIPointerType.Touch || pointer.device is Touchscreen
            : eventData.pointerId >= 0) return;
        if (!isActiveAndEnabled || !hoveringPointers.Add(eventData.pointerId)) return;
        PlaySelection();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // A raycast can move between the button background and its text/image children
        // without leaving the button. Only a real exit permits another hover sound.
        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        if (target != null && target.transform.IsChildOf(transform)) return;
        hoveringPointers.Remove(eventData.pointerId);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Mouse clicks also select the button; their feedback is handled by onClick.
        if (eventData is PointerEventData) return;
        PlaySelection();
    }

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
