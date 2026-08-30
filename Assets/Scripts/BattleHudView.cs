using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattleHudView : MonoBehaviour
{
    private const string GeneratedRootName = "GeneratedBattleHud";
    private const int CurrentLayoutVersion = 2;

    private static readonly Color PanelColor = new Color(0.025f, 0.04f, 0.055f, 0.88f);
    private static readonly Color PanelLightColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);
    private static readonly Color CyanColor = new Color(0.05f, 0.85f, 1f, 1f);
    private static readonly Color YellowColor = new Color(1f, 0.78f, 0.08f, 1f);
    private static readonly Color MagentaColor = new Color(1f, 0.18f, 0.55f, 1f);
    private static readonly Color UnavailableColor = new Color(1f, 0.08f, 0.04f, 1f);
    private static readonly Color WhiteColor = new Color(0.95f, 0.98f, 1f, 1f);
    private static readonly Color MutedColor = new Color(0.58f, 0.68f, 0.73f, 1f);

    [SerializeField] private int layoutVersion;

    public TMP_Text HealthText { get; private set; }
    public TMP_Text MachineNameText { get; private set; }
    public Slider HealthGauge { get; private set; }
    public Slider BoostGauge { get; private set; }
    public TMP_Text MainAmmoText { get; private set; }
    public TMP_Text MainWeaponNameText { get; private set; }
    public TMP_Text SubAmmoText { get; private set; }
    public TMP_Text SubWeaponNameText { get; private set; }
    public TMP_Text SpecialAmmoText { get; private set; }
    public TMP_Text SpecialWeaponNameText { get; private set; }
    public Slider ChargeGauge { get; private set; }
    public Slider AwakeningGauge { get; private set; }
    public TMP_Text TimerText { get; private set; }
    public TMP_Text PlayerCostText { get; private set; }
    public TMP_Text EnemyCostText { get; private set; }
    public TMP_Text PartnerNameText { get; private set; }
    public TMP_Text PartnerHealthText { get; private set; }
    public Slider PartnerHealthGauge { get; private set; }
    public TMP_Text TargetNameText { get; private set; }
    public TMP_Text TargetHealthText { get; private set; }
    public Slider TargetHealthGauge { get; private set; }
    public RectTransform RadarPlot { get; private set; }

    private RectTransform mainWeaponPanel;
    private RectTransform subWeaponPanel;
    private RectTransform specialWeaponPanel;
    private RectTransform partnerPanel;
    private RectTransform targetPanel;
    private Image mainWeaponStatus;
    private Image subWeaponStatus;
    private Image specialWeaponStatus;
    private RectTransform radarPlayerMarker;
    private readonly List<RectTransform> radarParticipantMarkers = new List<RectTransform>();

    public static BattleHudView Ensure(Transform canvasTransform)
    {
        if (canvasTransform == null)
        {
            return null;
        }

        Transform existingTransform = canvasTransform.Find(GeneratedRootName);
        BattleHudView existingView = existingTransform != null
            ? existingTransform.GetComponent<BattleHudView>()
            : null;
        bool wrongLifetime = existingView != null
            && Application.isPlaying
            && existingView.gameObject.hideFlags != HideFlags.None;
        bool missingReferences = existingView != null && existingView.HealthText == null;

        if (existingView != null
            && existingView.layoutVersion == CurrentLayoutVersion
            && !wrongLifetime
            && !missingReferences)
        {
            return existingView;
        }

        if (existingTransform != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existingTransform.gameObject);
            }
            else
            {
                DestroyImmediate(existingTransform.gameObject);
            }
        }

        GameObject root = new GameObject(
            GeneratedRootName,
            typeof(RectTransform),
            typeof(BattleHudView)
        );
        root.layer = canvasTransform.gameObject.layer;
        root.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.HideAndDontSave;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvasTransform, false);
        StretchToParent(rootRect);

        BattleHudView view = root.GetComponent<BattleHudView>();
        view.layoutVersion = CurrentLayoutVersion;
        view.BuildLayout();
        return view;
    }

    public void SetPreviewValues()
    {
        MachineNameText.text = "GUNDAM";
        HealthText.text = "660";
        SetGauge(HealthGauge, 660f, 660f);
        SetGauge(BoostGauge, 100f, 100f);
        MainWeaponNameText.text = "BEAM RIFLE";
        MainAmmoText.text = "8";
        SubWeaponNameText.text = "HYPER BAZOOKA";
        SubAmmoText.text = "2";
        SpecialWeaponNameText.text = "SUPPORT";
        SpecialAmmoText.text = "2";
        SetGauge(ChargeGauge, 0.45f, 1f);
        SetGauge(AwakeningGauge, 35f, 100f);
        TimerText.text = "180";
        PlayerCostText.text = "6000";
        EnemyCostText.text = "6000";
        PartnerNameText.text = "ALLY";
        PartnerHealthText.text = "600";
        SetGauge(PartnerHealthGauge, 600f, 600f);
        SetTarget("ENEMY MECH", 600f, 600f, true);
    }

    public void SetMainWeapon(string weaponName, int ammo, bool hasAmmo, bool visible)
    {
        SetWeapon(mainWeaponPanel, MainWeaponNameText, MainAmmoText, mainWeaponStatus,
            weaponName, ammo, hasAmmo, visible);
        RelayoutWeapons();
    }

    public void SetSubWeapon(string weaponName, int ammo, bool hasAmmo, bool visible)
    {
        SetWeapon(subWeaponPanel, SubWeaponNameText, SubAmmoText, subWeaponStatus,
            weaponName, ammo, hasAmmo, visible);
        RelayoutWeapons();
    }

    public void SetSpecialWeapon(string weaponName, int ammo, bool hasAmmo, bool visible)
    {
        SetWeapon(specialWeaponPanel, SpecialWeaponNameText, SpecialAmmoText,
            specialWeaponStatus, weaponName, ammo, hasAmmo, visible);
        RelayoutWeapons();
    }

    public void SetCharge(float chargeRate, bool visible)
    {
        if (ChargeGauge == null)
        {
            return;
        }

        ChargeGauge.gameObject.SetActive(visible);

        if (visible)
        {
            SetGauge(ChargeGauge, Mathf.Clamp01(chargeRate), 1f);
        }
    }

    public void SetPartner(string partnerName, float currentHealth, float maximumHealth, bool visible)
    {
        if (partnerPanel == null)
        {
            return;
        }

        partnerPanel.gameObject.SetActive(visible);

        if (!visible)
        {
            return;
        }

        PartnerNameText.text = partnerName;
        PartnerHealthText.text = Mathf.CeilToInt(currentHealth).ToString();
        SetGauge(PartnerHealthGauge, currentHealth, maximumHealth);
    }

    public void SetTarget(string targetName, float currentHealth, float maximumHealth, bool visible)
    {
        if (targetPanel == null)
        {
            return;
        }

        targetPanel.gameObject.SetActive(visible);

        if (!visible)
        {
            return;
        }

        TargetNameText.text = targetName;
        TargetHealthText.text = Mathf.CeilToInt(currentHealth).ToString();
        SetGauge(TargetHealthGauge, currentHealth, maximumHealth);
    }

    public void UpdateRadar(
        BattleParticipant player,
        IReadOnlyList<BattleParticipant> participants,
        Vector2 worldCenter,
        Vector2 worldSize)
    {
        if (RadarPlot == null)
        {
            return;
        }

        if (player == null)
        {
            radarPlayerMarker?.gameObject.SetActive(false);
            return;
        }

        SetRadarMarkerPosition(radarPlayerMarker, player.transform.position, worldCenter, worldSize);
        radarPlayerMarker.gameObject.SetActive(true);
        int markerIndex = 0;

        foreach (BattleParticipant participant in participants)
        {
            if (participant == null || participant == player || !participant.IsAvailable)
            {
                continue;
            }

            RectTransform marker = GetRadarParticipantMarker(markerIndex++);
            SetRadarMarkerPosition(
                marker,
                participant.transform.position,
                worldCenter,
                worldSize
            );
            marker.GetComponent<Image>().color = participant.Team == player.Team
                ? YellowColor
                : MagentaColor;
            marker.gameObject.SetActive(true);
        }

        for (int i = markerIndex; i < radarParticipantMarkers.Count; i++)
        {
            radarParticipantMarkers[i].gameObject.SetActive(false);
        }
    }

    private void SetRadarMarkerPosition(
        RectTransform marker,
        Vector3 worldPosition,
        Vector2 worldCenter,
        Vector2 worldSize)
    {
        Vector2 safeSize = new Vector2(
            Mathf.Max(1f, worldSize.x),
            Mathf.Max(1f, worldSize.y)
        );
        Vector2 normalized = new Vector2(
            (worldPosition.x - worldCenter.x) / (safeSize.x * 0.5f),
            (worldPosition.z - worldCenter.y) / (safeSize.y * 0.5f)
        );
        normalized.x = Mathf.Clamp(normalized.x, -1f, 1f);
        normalized.y = Mathf.Clamp(normalized.y, -1f, 1f);
        Vector2 plotHalfSize = RadarPlot.rect.size * 0.44f;
        marker.anchoredPosition = Vector2.Scale(normalized, plotHalfSize);
    }

    private void BuildLayout()
    {
        BuildTeamStatus();
        BuildTimer();
        BuildTargetStatus();
        BuildPlayerStatus();
        BuildBoostStatus();
        BuildPartnerStatus();
        BuildWeaponRack();
        BuildRadar();
        SetPreviewValues();
    }

    private void BuildTeamStatus()
    {
        RectTransform panel = CreatePanel(
            "TeamStatus",
            transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(22f, -20f),
            new Vector2(430f, 92f),
            PanelColor
        );
        CreateAccent(panel, CyanColor, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -3f), new Vector2(430f, 6f));
        CreateText("PlayerLabel", panel, "TEAM", 17f, FontStyles.Bold,
            TextAlignmentOptions.Left, CyanColor, new Vector2(18f, -13f), new Vector2(100f, 24f));
        PlayerCostText = CreateText("PlayerCost", panel, "6000", 34f, FontStyles.Bold,
            TextAlignmentOptions.Left, WhiteColor, new Vector2(16f, -36f), new Vector2(170f, 48f));
        CreateText("EnemyLabel", panel, "ENEMY", 17f, FontStyles.Bold,
            TextAlignmentOptions.Right, MagentaColor, new Vector2(314f, -13f), new Vector2(96f, 24f));
        EnemyCostText = CreateText("EnemyCost", panel, "6000", 34f, FontStyles.Bold,
            TextAlignmentOptions.Right, WhiteColor, new Vector2(240f, -36f), new Vector2(170f, 48f));
        CreateAccent(panel, CyanColor, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(18f, 9f), new Vector2(176f, 5f));
        CreateAccent(panel, MagentaColor, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-194f, 9f), new Vector2(176f, 5f));
    }

    private void BuildTimer()
    {
        RectTransform panel = CreatePanel(
            "TimerStatus",
            transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -18f),
            new Vector2(180f, 92f),
            PanelColor
        );
        CreateText("TimerLabel", panel, "TIME", 15f, FontStyles.Bold,
            TextAlignmentOptions.Center, CyanColor, new Vector2(20f, -10f), new Vector2(140f, 22f));
        TimerText = CreateText("Timer", panel, "180", 48f, FontStyles.Bold,
            TextAlignmentOptions.Center, WhiteColor, new Vector2(10f, -30f), new Vector2(160f, 54f));
        CreateAccent(panel, YellowColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-55f, 5f), new Vector2(110f, 5f));
    }

    private void BuildPlayerStatus()
    {
        RectTransform panel = CreatePanel(
            "PlayerStatus",
            transform,
            Vector2.zero,
            Vector2.zero,
            new Vector2(24f, 22f),
            new Vector2(500f, 190f),
            PanelColor
        );
        CreateAccent(panel, CyanColor, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -4f), new Vector2(500f, 7f));
        CreateText("ArmorLabel", panel, "ARMOR", 16f, FontStyles.Bold,
            TextAlignmentOptions.Left, CyanColor, new Vector2(20f, -23f), new Vector2(120f, 24f));
        MachineNameText = CreateText("MachineName", panel, "GUNDAM", 22f, FontStyles.Bold,
            TextAlignmentOptions.Right, MutedColor, new Vector2(210f, -20f), new Vector2(266f, 30f));
        HealthText = CreateText("Health", panel, "660", 66f, FontStyles.Bold,
            TextAlignmentOptions.Left, WhiteColor, new Vector2(18f, -50f), new Vector2(230f, 78f));
        HealthGauge = CreateGauge("HealthGauge", panel, new Vector2(20f, 22f),
            new Vector2(456f, 22f), CyanColor);
        CreateText("BurstLabel", panel, "EX BURST", 14f, FontStyles.Bold,
            TextAlignmentOptions.Left, MagentaColor, new Vector2(266f, -74f), new Vector2(120f, 22f));
        AwakeningGauge = CreateGauge("AwakeningGauge", panel, new Vector2(266f, 49f),
            new Vector2(210f, 16f), MagentaColor);
    }

    private void BuildTargetStatus()
    {
        targetPanel = CreatePanel(
            "TargetStatus",
            transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -118f),
            new Vector2(420f, 72f),
            PanelColor
        );
        CreateAccent(targetPanel, MagentaColor, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -3f), new Vector2(420f, 5f));
        TargetNameText = CreateText("TargetName", targetPanel, "ENEMY MECH", 18f,
            FontStyles.Bold, TextAlignmentOptions.Left, WhiteColor,
            new Vector2(16f, -12f), new Vector2(280f, 26f));
        TargetHealthText = CreateText("TargetHealth", targetPanel, "600", 25f,
            FontStyles.Bold, TextAlignmentOptions.Right, WhiteColor,
            new Vector2(310f, -8f), new Vector2(92f, 34f));
        TargetHealthGauge = CreateGauge("TargetHealthGauge", targetPanel,
            new Vector2(16f, 12f), new Vector2(386f, 12f), MagentaColor);
    }

    private void BuildBoostStatus()
    {
        RectTransform panel = CreatePanel(
            "BoostStatus",
            transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 24f),
            new Vector2(500f, 70f),
            PanelColor
        );
        CreateText("BoostLabel", panel, "BOOST", 16f, FontStyles.Bold,
            TextAlignmentOptions.Left, CyanColor, new Vector2(18f, -11f), new Vector2(110f, 24f));
        BoostGauge = CreateGauge("BoostGauge", panel, new Vector2(18f, 14f),
            new Vector2(464f, 20f), CyanColor);
    }

    private void BuildPartnerStatus()
    {
        RectTransform panel = CreatePanel(
            "PartnerStatus",
            transform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(22f, 20f),
            new Vector2(330f, 112f),
            PanelColor
        );
        partnerPanel = panel;
        CreateAccent(panel, YellowColor, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -3f), new Vector2(330f, 6f));
        CreateText("PartnerLabel", panel, "PARTNER", 14f, FontStyles.Bold,
            TextAlignmentOptions.Left, YellowColor, new Vector2(16f, -14f), new Vector2(100f, 22f));
        PartnerNameText = CreateText("PartnerName", panel, "ALLY", 18f, FontStyles.Bold,
            TextAlignmentOptions.Left, WhiteColor, new Vector2(16f, -39f), new Vector2(190f, 28f));
        PartnerHealthText = CreateText("PartnerHealth", panel, "600", 30f, FontStyles.Bold,
            TextAlignmentOptions.Right, WhiteColor, new Vector2(220f, -32f), new Vector2(92f, 38f));
        PartnerHealthGauge = CreateGauge("PartnerHealthGauge", panel, new Vector2(16f, 14f),
            new Vector2(296f, 13f), YellowColor);
    }

    private void BuildWeaponRack()
    {
        RectTransform rack = CreateRect(
            "WeaponRack",
            transform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-24f, -74f),
            new Vector2(380f, 326f)
        );

        BuildWeaponSlot(rack, "MainWeapon", 0f, "MAIN", "BEAM RIFLE", "8",
            out TMP_Text mainName, out TMP_Text mainAmmo, out RectTransform mainPanel,
            out mainWeaponStatus);
        mainWeaponPanel = mainPanel;
        MainWeaponNameText = mainName;
        MainAmmoText = mainAmmo;
        ChargeGauge = CreateGauge("ChargeGauge", mainPanel, new Vector2(20f, 11f),
            new Vector2(244f, 8f), YellowColor);

        BuildWeaponSlot(rack, "SubWeapon", -108f, "SUB", "HYPER BAZOOKA", "2",
            out TMP_Text subName, out TMP_Text subAmmo, out subWeaponPanel,
            out subWeaponStatus);
        SubWeaponNameText = subName;
        SubAmmoText = subAmmo;

        BuildWeaponSlot(rack, "SpecialWeapon", -216f, "SPECIAL", "SUPPORT", "2",
            out TMP_Text specialName, out TMP_Text specialAmmo, out specialWeaponPanel,
            out specialWeaponStatus);
        SpecialWeaponNameText = specialName;
        SpecialAmmoText = specialAmmo;
    }

    private void BuildWeaponSlot(
        Transform parent,
        string objectName,
        float y,
        string category,
        string weaponName,
        string ammo,
        out TMP_Text nameText,
        out TMP_Text ammoText,
        out RectTransform panel,
        out Image statusImage)
    {
        panel = CreatePanel(
            objectName,
            parent,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, y),
            new Vector2(360f, 96f),
            PanelColor
        );
        statusImage = CreateAccent(panel, CyanColor, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 4f), new Vector2(270f, 6f));
        CreateText("Category", panel, category, 13f, FontStyles.Bold,
            TextAlignmentOptions.Left, CyanColor, new Vector2(18f, -12f), new Vector2(90f, 20f));
        nameText = CreateText("WeaponName", panel, weaponName, 17f, FontStyles.Bold,
            TextAlignmentOptions.Left, WhiteColor, new Vector2(18f, -39f), new Vector2(242f, 34f));
        ammoText = CreateText("Ammo", panel, ammo, 48f, FontStyles.Bold,
            TextAlignmentOptions.Right, WhiteColor, new Vector2(270f, -17f), new Vector2(72f, 58f));
    }

    private void BuildRadar()
    {
        RectTransform panel = CreatePanel(
            "Radar",
            transform,
            Vector2.one,
            Vector2.one,
            new Vector2(-22f, -20f),
            new Vector2(224f, 224f),
            new Color(0.015f, 0.025f, 0.03f, 0.82f)
        );
        CreateText("RadarLabel", panel, "RADAR", 14f, FontStyles.Bold,
            TextAlignmentOptions.Center, CyanColor, new Vector2(62f, -8f), new Vector2(100f, 22f));
        RadarPlot = CreateRect("RadarPlot", panel, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-88f, -96f), new Vector2(176f, 176f));
        CreateAccent(RadarPlot, new Color(0.1f, 0.75f, 0.85f, 0.35f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-1f, 0f), new Vector2(2f, 176f));
        CreateAccent(RadarPlot, new Color(0.1f, 0.75f, 0.85f, 0.35f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -1f), new Vector2(176f, 2f));
        radarPlayerMarker = CreateRadarMarker(
            "PlayerMarker",
            RadarPlot,
            Vector2.zero,
            CyanColor,
            new Vector2(12f, 12f)
        );
        radarParticipantMarkers.Add(CreateRadarMarker(
            "PartnerMarker",
            RadarPlot,
            new Vector2(-34f, -28f),
            YellowColor,
            new Vector2(10f, 10f)
        ));
        radarParticipantMarkers.Add(CreateRadarMarker(
            "EnemyMarker",
            RadarPlot,
            new Vector2(42f, 48f),
            MagentaColor,
            new Vector2(12f, 12f)
        ));
    }

    private static RectTransform CreatePanel(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        RectTransform rect = CreateRect(objectName, parent, anchor, pivot, anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static RectTransform CreateRect(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.hideFlags = parent.gameObject.hideFlags;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        RectTransform rect = CreateRect(objectName, parent, new Vector2(0f, 1f),
            new Vector2(0f, 1f), anchoredPosition, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.characterSpacing = 0f;
        return text;
    }

    private static Slider CreateGauge(
        string objectName,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        Color fillColor)
    {
        RectTransform gaugeRect = CreateRect(objectName, parent, Vector2.zero,
            Vector2.zero, anchoredPosition, size);
        Image background = gaugeRect.gameObject.AddComponent<Image>();
        background.color = PanelLightColor;
        background.raycastTarget = false;

        RectTransform fillRect = CreateRect("Fill", gaugeRect, Vector2.zero,
            Vector2.zero, new Vector2(3f, 3f), new Vector2(-6f, -6f));
        fillRect.anchorMax = Vector2.one;
        Image fill = fillRect.gameObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.raycastTarget = false;

        Slider slider = gaugeRect.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.interactable = false;
        slider.fillRect = fillRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static Image CreateAccent(
        Transform parent,
        Color color,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = CreateRect("Accent", parent, anchor, pivot, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetWeapon(
        RectTransform panel,
        TMP_Text nameText,
        TMP_Text ammoText,
        Image statusImage,
        string weaponName,
        int ammo,
        bool hasAmmo,
        bool visible)
    {
        if (panel == null)
        {
            return;
        }

        panel.gameObject.SetActive(visible);

        if (!visible)
        {
            return;
        }

        Color stateColor = hasAmmo ? WhiteColor : UnavailableColor;
        nameText.text = weaponName;
        nameText.color = stateColor;
        ammoText.text = Mathf.Max(0, ammo).ToString();
        ammoText.color = stateColor;
        statusImage.color = hasAmmo ? CyanColor : UnavailableColor;
    }

    private void RelayoutWeapons()
    {
        float y = 0f;
        RectTransform[] panels = { mainWeaponPanel, subWeaponPanel, specialWeaponPanel };

        foreach (RectTransform panel in panels)
        {
            if (panel == null || !panel.gameObject.activeSelf)
            {
                continue;
            }

            panel.anchoredPosition = new Vector2(0f, y);
            y -= 108f;
        }
    }

    private RectTransform GetRadarParticipantMarker(int index)
    {
        while (radarParticipantMarkers.Count <= index)
        {
            radarParticipantMarkers.Add(CreateRadarMarker(
                "ParticipantMarker" + radarParticipantMarkers.Count,
                RadarPlot,
                Vector2.zero,
                MagentaColor,
                new Vector2(10f, 10f)
            ));
        }

        return radarParticipantMarkers[index];
    }

    private static RectTransform CreateRadarMarker(
        string objectName,
        Transform parent,
        Vector2 position,
        Color color,
        Vector2 size)
    {
        RectTransform marker = CreateRect(objectName, parent, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, size);
        marker.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image image = marker.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return marker;
    }

    private static void SetGauge(Slider gauge, float current, float maximum)
    {
        gauge.minValue = 0f;
        gauge.maxValue = Mathf.Max(1f, maximum);
        gauge.value = Mathf.Clamp(current, 0f, gauge.maxValue);
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
