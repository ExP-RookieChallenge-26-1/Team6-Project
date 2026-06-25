using Project2048.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPopup : MonoBehaviour
{
    private const int AudioChannelCount = 3;
    private static readonly Color SliderFillColor = new(0.286f, 0.686f, 0.710f, 1f);
    private static readonly Color SliderTrackColor = new(0.18f, 0.18f, 0.18f, 0.85f);

    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button backgroundButton;
    [SerializeField] private Project2048AudioSettings audioSettings;
    [SerializeField] private Transform volumeControlsParent;
    [SerializeField] private bool createMissingVolumeControls = true;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeValueText;
    [SerializeField] private TextMeshProUGUI bgmVolumeValueText;
    [SerializeField] private TextMeshProUGUI sfxVolumeValueText;

    [Header("Mute Toggles")]
    [SerializeField] private Toggle masterMuteToggle;
    [SerializeField] private Toggle bgmMuteToggle;
    [SerializeField] private Toggle sfxMuteToggle;

    private readonly Slider[] volumeSliders = new Slider[AudioChannelCount];
    private readonly Toggle[] muteToggles = new Toggle[AudioChannelCount];
    private readonly TextMeshProUGUI[] volumeValueTexts = new TextMeshProUGUI[AudioChannelCount];
    private bool initialized;
    private bool wantsOpen;

    private void Awake()
    {
        InitializeIfNeeded();
        if (!wantsOpen && popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    public void Open()
    {
        wantsOpen = true;
        InitializeIfNeeded();
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
        }

        Project2048AudioPreferences.ApplySavedVolumes(audioSettings);
        RefreshVolumeSliders();
    }

    public void Close()
    {
        wantsOpen = false;
        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        popupRoot.SetActive(false);
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        if (audioSettings == null)
        {
            audioSettings = Project2048AudioSettings.LoadDefault();
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(Close);
        }

        if (backgroundButton != null)
        {
            backgroundButton.onClick.AddListener(Close);
        }

        ResolveSceneVolumeControls();
        if (createMissingVolumeControls)
        {
            CreateMissingVolumeControls();
        }

        HideLegacyVolumeControls();
        SortVisibleVolumeControls();
        BindVolumeControl(Project2048AudioChannel.Master, masterVolumeSlider, masterMuteToggle, masterVolumeValueText);
        BindVolumeControl(Project2048AudioChannel.BGM, bgmVolumeSlider, bgmMuteToggle, bgmVolumeValueText);
        BindVolumeControl(Project2048AudioChannel.SFX, sfxVolumeSlider, sfxMuteToggle, sfxVolumeValueText);
        Project2048AudioPreferences.ApplySavedVolumes(audioSettings);
    }

    private void ResolveSceneVolumeControls()
    {
        masterVolumeSlider = masterVolumeSlider != null ? masterVolumeSlider : FindSlider("MasterVolumeSlider");
        bgmVolumeSlider = bgmVolumeSlider != null ? bgmVolumeSlider : FindSlider("BGMVolumeSlider");
        sfxVolumeSlider = sfxVolumeSlider != null ? sfxVolumeSlider : FindSlider("SFXVolumeSlider");
        masterMuteToggle = masterMuteToggle != null ? masterMuteToggle : FindToggle("MasterMuteToggle");
        bgmMuteToggle = bgmMuteToggle != null ? bgmMuteToggle : FindToggle("BGMMuteToggle");
        sfxMuteToggle = sfxMuteToggle != null ? sfxMuteToggle : FindToggle("SFXMuteToggle");
    }

    private Slider FindSlider(string objectName)
    {
        if (popupRoot == null)
        {
            return null;
        }

        foreach (var slider in popupRoot.GetComponentsInChildren<Slider>(true))
        {
            if (slider != null && slider.gameObject.name == objectName)
            {
                return slider;
            }
        }

        return null;
    }

    private Toggle FindToggle(string objectName)
    {
        if (popupRoot == null)
        {
            return null;
        }

        foreach (var toggle in popupRoot.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle != null && toggle.gameObject.name == objectName)
            {
                return toggle;
            }
        }

        return null;
    }

    private void BindVolumeControl(
        Project2048AudioChannel channel,
        Slider slider,
        Toggle muteToggle,
        TextMeshProUGUI valueText)
    {
        if (slider == null)
        {
            return;
        }

        var channelIndex = (int)channel;
        volumeSliders[channelIndex] = slider;
        muteToggles[channelIndex] = muteToggle;
        volumeValueTexts[channelIndex] = valueText;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        var savedVolume = Project2048AudioPreferences.GetNormalizedVolume(channel);
        var isMuted = Project2048AudioPreferences.IsMuted(channel);
        slider.SetValueWithoutNotify(savedVolume);
        UpdateVolumeValueText(channel, savedVolume);
        slider.onValueChanged.AddListener(value => HandleVolumeSliderChanged(channel, value));

        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(isMuted);
            muteToggle.onValueChanged.AddListener(value => HandleMuteToggleChanged(channel, value));
        }
    }

    private void HandleVolumeSliderChanged(Project2048AudioChannel channel, float value)
    {
        var normalizedVolume = Mathf.Clamp01(value);
        Project2048AudioPreferences.SetNormalizedVolume(audioSettings, channel, normalizedVolume, false);
        PlayerPrefs.Save();
        UpdateVolumeValueText(channel, normalizedVolume);
    }

    private void HandleMuteToggleChanged(Project2048AudioChannel channel, bool isMuted)
    {
        Project2048AudioPreferences.SetMuted(audioSettings, channel, isMuted, false);
        PlayerPrefs.Save();
    }

    private void RefreshVolumeSliders()
    {
        RefreshVolumeControl(Project2048AudioChannel.Master);
        RefreshVolumeControl(Project2048AudioChannel.BGM);
        RefreshVolumeControl(Project2048AudioChannel.SFX);
    }

    private void RefreshVolumeControl(Project2048AudioChannel channel)
    {
        var channelIndex = (int)channel;
        var slider = volumeSliders[channelIndex];
        if (slider == null)
        {
            return;
        }

        var savedVolume = Project2048AudioPreferences.GetNormalizedVolume(channel);
        slider.SetValueWithoutNotify(savedVolume);
        UpdateVolumeValueText(channel, savedVolume);

        var muteToggle = muteToggles[channelIndex];
        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(Project2048AudioPreferences.IsMuted(channel));
        }
    }

    private void UpdateVolumeValueText(Project2048AudioChannel channel, float normalizedVolume)
    {
        var valueText = volumeValueTexts[(int)channel];
        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(Mathf.Clamp01(normalizedVolume) * 100f).ToString();
        }
    }

    private void CreateMissingVolumeControls()
    {
        var container = GetOrCreateVolumeControlsContainer();
        if (container == null)
        {
            return;
        }

        if (masterVolumeSlider == null)
        {
            masterVolumeSlider = CreateVolumeRow(
                container,
                "MasterVolumeSlider",
                "Master",
                "MasterMuteToggle",
                out masterMuteToggle,
                out masterVolumeValueText);
        }
        else if (masterMuteToggle == null)
        {
            masterMuteToggle = CreateMuteToggleForExistingRow(masterVolumeSlider, "MasterMuteToggle");
        }

        if (bgmVolumeSlider == null)
        {
            bgmVolumeSlider = CreateVolumeRow(
                container,
                "BGMVolumeSlider",
                "BGM",
                "BGMMuteToggle",
                out bgmMuteToggle,
                out bgmVolumeValueText);
        }
        else if (bgmMuteToggle == null)
        {
            bgmMuteToggle = CreateMuteToggleForExistingRow(bgmVolumeSlider, "BGMMuteToggle");
        }

        if (sfxVolumeSlider == null)
        {
            sfxVolumeSlider = CreateVolumeRow(
                container,
                "SFXVolumeSlider",
                "SFX",
                "SFXMuteToggle",
                out sfxMuteToggle,
                out sfxVolumeValueText);
        }
        else if (sfxMuteToggle == null)
        {
            sfxMuteToggle = CreateMuteToggleForExistingRow(sfxVolumeSlider, "SFXMuteToggle");
        }
    }

    private void HideLegacyVolumeControls()
    {
        SetVolumeRowActive(FindSlider("UIVolumeSlider"), false);
        SetVolumeRowActive(FindSlider("AmbienceVolumeSlider"), false);
    }

    private void SortVisibleVolumeControls()
    {
        SetVolumeRowSibling(masterVolumeSlider, 0);
        SetVolumeRowSibling(bgmVolumeSlider, 1);
        SetVolumeRowSibling(sfxVolumeSlider, 2);
    }

    private static void SetVolumeRowActive(Slider slider, bool isActive)
    {
        if (slider == null)
        {
            return;
        }

        var row = slider.transform.parent != null ? slider.transform.parent.gameObject : slider.gameObject;
        row.SetActive(isActive);
    }

    private static void SetVolumeRowSibling(Slider slider, int siblingIndex)
    {
        if (slider == null || slider.transform.parent == null)
        {
            return;
        }

        slider.transform.parent.SetSiblingIndex(siblingIndex);
    }

    private Toggle CreateMuteToggleForExistingRow(Slider slider, string toggleName)
    {
        if (slider == null)
        {
            return null;
        }

        var row = slider.transform.parent != null ? slider.transform.parent : slider.transform;
        return CreateMuteToggle(row, toggleName);
    }

    private Transform GetOrCreateVolumeControlsContainer()
    {
        if (volumeControlsParent != null)
        {
            return volumeControlsParent;
        }

        var parent = ResolveVolumeControlsParent();
        if (parent == null)
        {
            return null;
        }

        var existing = parent.Find("VolumeControls");
        if (existing != null)
        {
            volumeControlsParent = existing;
            return volumeControlsParent;
        }

        var container = CreateRectObject("VolumeControls", parent);
        volumeControlsParent = container.transform;
        var rectTransform = container.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, 35f);
        rectTransform.sizeDelta = new Vector2(560f, 220f);

        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return volumeControlsParent;
    }

    private Transform ResolveVolumeControlsParent()
    {
        if (popupRoot == null)
        {
            return transform;
        }

        var panel = popupRoot.transform.Find("Panel");
        return panel != null ? panel : popupRoot.transform;
    }

    private Slider CreateVolumeRow(
        Transform parent,
        string sliderName,
        string label,
        string toggleName,
        out Toggle muteToggle,
        out TextMeshProUGUI valueText)
    {
        var row = CreateRectObject(sliderName + "Row", parent);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(540f, 42f);

        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 42f;

        CreateLabel(row.transform, label, 110f, TextAlignmentOptions.Left);
        var slider = CreateSlider(row.transform, sliderName);
        valueText = CreateLabel(row.transform, "100", 64f, TextAlignmentOptions.Right);
        muteToggle = CreateMuteToggle(row.transform, toggleName);
        return slider;
    }

    private TextMeshProUGUI CreateLabel(
        Transform parent,
        string text,
        float preferredWidth,
        TextAlignmentOptions alignment)
    {
        var labelObject = CreateRectObject(text + "Label", parent);
        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.preferredHeight = 36f;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 24f;
        label.color = Color.white;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private Slider CreateSlider(Transform parent, string name)
    {
        var sliderObject = CreateRectObject(name, parent);
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(320f, 36f);

        var sliderElement = sliderObject.AddComponent<LayoutElement>();
        sliderElement.flexibleWidth = 1f;
        sliderElement.preferredHeight = 36f;

        var background = CreateSliderImage("Background", sliderObject.transform, SliderTrackColor);
        SetStretch(background.rectTransform, 0f, 0.5f, 1f, 0.5f, 0f, 8f);

        var fillArea = CreateRectObject("Fill Area", sliderObject.transform).GetComponent<RectTransform>();
        SetStretch(fillArea, 0f, 0f, 1f, 1f, 12f, 0f);

        var fill = CreateSliderImage("Fill", fillArea, SliderFillColor);
        SetStretch(fill.rectTransform, 0f, 0.5f, 1f, 0.5f, 0f, 8f);

        var handleArea = CreateRectObject("Handle Slide Area", sliderObject.transform).GetComponent<RectTransform>();
        SetStretch(handleArea, 0f, 0f, 1f, 1f, 12f, 0f);

        var handle = CreateSliderImage("Handle", handleArea, Color.white);
        handle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        handle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        handle.rectTransform.sizeDelta = new Vector2(24f, 24f);

        var slider = sliderObject.AddComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private Toggle CreateMuteToggle(Transform parent, string name)
    {
        var toggleObject = CreateRectObject(name, parent);
        var toggleElement = toggleObject.AddComponent<LayoutElement>();
        toggleElement.preferredWidth = 36f;
        toggleElement.preferredHeight = 36f;

        var background = CreateSliderImage("Background", toggleObject.transform, SliderTrackColor);
        background.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        background.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        background.rectTransform.sizeDelta = new Vector2(26f, 26f);

        var checkmark = CreateSliderImage("Checkmark", background.transform, SliderFillColor);
        checkmark.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        checkmark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        checkmark.rectTransform.sizeDelta = new Vector2(16f, 16f);

        var toggle = toggleObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        toggle.isOn = false;
        return toggle;
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = parent != null ? parent.gameObject.layer : 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Image CreateSliderImage(string name, Transform parent, Color color)
    {
        var imageObject = CreateRectObject(name, parent);
        var image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetStretch(
        RectTransform rectTransform,
        float minX,
        float minY,
        float maxX,
        float maxY,
        float horizontalPadding,
        float height)
    {
        rectTransform.anchorMin = new Vector2(minX, minY);
        rectTransform.anchorMax = new Vector2(maxX, maxY);
        rectTransform.offsetMin = new Vector2(horizontalPadding, -height * 0.5f);
        rectTransform.offsetMax = new Vector2(-horizontalPadding, height * 0.5f);
    }
}
