using System.Collections.Generic;
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
    [SerializeField] private Color redLockColor = new Color(1f, 0.08f, 0.04f, 1f);
    [SerializeField] private Color greenLockColor = new Color(0.15f, 1f, 0.3f, 1f);
    [SerializeField] private Vector2 reticleSize = new Vector2(96f, 96f);
    [SerializeField] private float bracketLength = 24f;
    [SerializeField] private float bracketThickness = 6f;
    [SerializeField] private float bracketInset = 4f;

    private Transform currentTarget;
    private readonly List<Image> bracketImages = new List<Image>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureControllerExists()
    {
        GameObject battleUi = GameObject.Find("BattleUI");

        if (battleUi == null
            || battleUi.GetComponentInChildren<LockOnReticleController>(true) != null)
        {
            return;
        }

        battleUi.AddComponent<LockOnReticleController>();
    }

    private void Awake()
    {
        ResolveReferences();

        if (reticle != null)
        {
            reticle.sizeDelta = reticleSize;
        }

        BuildBracketVisual();
    }

    private void OnEnable()
    {
        ResolveReferences();

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
        Color activeColor = state == LockState.Red ? redLockColor : greenLockColor;

        foreach (Image bracket in bracketImages)
        {
            bracket.color = activeColor;
            bracket.enabled = state != LockState.None;
        }
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

    private void ResolveReferences()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (reticle == null)
        {
            Transform reticleTransform = FindChildByName(
                canvas != null ? canvas.transform : transform,
                "LockOnReticle"
            );
            reticle = reticleTransform as RectTransform;
        }

        if (reticleImage == null && reticle != null)
        {
            reticleImage = reticle.GetComponent<Image>();
        }

        if (lockOnController == null)
        {
            lockOnController = FindPlayerLockOnController();
        }
    }

    private void BuildBracketVisual()
    {
        if (reticle == null)
        {
            return;
        }

        if (reticleImage != null)
        {
            // 元の塗りつぶし四角は非表示にし、子要素のブラケットだけを描画する。
            reticleImage.enabled = false;
        }

        RemoveOldGeneratedBrackets();
        bracketImages.Clear();

        CreateCorner("TopLeft", new Vector2(0f, 1f), new Vector2(1f, -1f));
        CreateCorner("TopRight", new Vector2(1f, 1f), new Vector2(-1f, -1f));
        CreateCorner("BottomLeft", new Vector2(0f, 0f), new Vector2(1f, 1f));
        CreateCorner("BottomRight", new Vector2(1f, 0f), new Vector2(-1f, 1f));
    }

    private void CreateCorner(string name, Vector2 anchor, Vector2 inwardDirection)
    {
        float halfLength = bracketLength * 0.5f;
        float halfThickness = bracketThickness * 0.5f;
        Vector2 baseOffset = new Vector2(
            inwardDirection.x * bracketInset,
            inwardDirection.y * bracketInset
        );

        CreateBracketSegment(
            name + "Horizontal",
            anchor,
            new Vector2(bracketLength, bracketThickness),
            baseOffset + new Vector2(inwardDirection.x * halfLength, inwardDirection.y * halfThickness)
        );
        CreateBracketSegment(
            name + "Vertical",
            anchor,
            new Vector2(bracketThickness, bracketLength),
            baseOffset + new Vector2(inwardDirection.x * halfThickness, inwardDirection.y * halfLength)
        );
    }

    private void CreateBracketSegment(
        string name,
        Vector2 anchor,
        Vector2 size,
        Vector2 anchoredPosition)
    {
        GameObject segmentObject = new GameObject(
            "GeneratedReticle_" + name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        RectTransform segment = segmentObject.GetComponent<RectTransform>();
        segment.SetParent(reticle, false);
        segment.anchorMin = anchor;
        segment.anchorMax = anchor;
        segment.pivot = new Vector2(0.5f, 0.5f);
        segment.sizeDelta = size;
        segment.anchoredPosition = anchoredPosition;

        Image image = segmentObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = redLockColor;

        Shadow shadow = segmentObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        shadow.useGraphicAlpha = true;
        bracketImages.Add(image);
    }

    private void RemoveOldGeneratedBrackets()
    {
        for (int i = reticle.childCount - 1; i >= 0; i--)
        {
            Transform child = reticle.GetChild(i);

            if (child.name.StartsWith("GeneratedReticle_"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static LockOnController FindPlayerLockOnController()
    {
        LockOnController[] controllers = FindObjectsByType<LockOnController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (LockOnController controller in controllers)
        {
            BattleParticipant participant = controller.GetComponent<BattleParticipant>();

            if (participant != null && participant.Team == BattleTeam.Player)
            {
                return controller;
            }
        }

        return controllers.Length > 0 ? controllers[0] : null;
    }

    private static Transform FindChildByName(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform match = FindChildByName(parent.GetChild(i), objectName);

            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        reticleSize.x = Mathf.Max(1f, reticleSize.x);
        reticleSize.y = Mathf.Max(1f, reticleSize.y);
        bracketLength = Mathf.Max(1f, bracketLength);
        bracketThickness = Mathf.Max(1f, bracketThickness);
        bracketInset = Mathf.Max(0f, bracketInset);
    }
}
