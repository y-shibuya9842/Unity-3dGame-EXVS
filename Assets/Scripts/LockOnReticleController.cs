using UnityEngine;
using UnityEngine.UI;

public class LockOnReticleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform reticle;
    [SerializeField] private Image reticleImage;

    [Header("Appearance")]
    [SerializeField] private Color redLockColor = Color.red;
    [SerializeField] private Color greenLockColor = Color.green;
    [SerializeField] private Vector2 reticleSize = new Vector2(60f, 60f);

    private Transform currentTarget;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (reticle != null)
        {
            reticle.sizeDelta = reticleSize;
        }
    }

    private void OnEnable()
    {
        if (lockOnController == null)
        {
            return;
        }

        lockOnController.OnTargetChanged += HandleTargetChanged;
        lockOnController.OnLockStateChanged += HandleLockStateChanged;
        HandleTargetChanged(lockOnController.CurrentTarget);
        HandleLockStateChanged(lockOnController.CurrentLockState);
    }

    private void LateUpdate()
    {
        UpdateReticlePosition();
    }

    private void OnDisable()
    {
        if (lockOnController == null)
        {
            return;
        }

        lockOnController.OnTargetChanged -= HandleTargetChanged;
        lockOnController.OnLockStateChanged -= HandleLockStateChanged;
    }

    private void HandleTargetChanged(Transform target)
    {
        currentTarget = target;

        if (reticle != null)
        {
            reticle.gameObject.SetActive(target != null);
        }
    }

    private void HandleLockStateChanged(LockState state)
    {
        if (reticleImage == null)
        {
            return;
        }

        reticleImage.color = state == LockState.Red ? redLockColor : greenLockColor;
        reticleImage.enabled = state != LockState.None;
    }

    private void UpdateReticlePosition()
    {
        if (currentTarget == null || reticle == null || worldCamera == null || canvas == null)
        {
            return;
        }

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(currentTarget.position);
        bool isVisible = screenPosition.z > 0f;
        reticle.gameObject.SetActive(isVisible);

        if (!isVisible)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvasCamera,
            out Vector2 localPosition))
        {
            reticle.anchoredPosition = localPosition;
        }
    }
}
