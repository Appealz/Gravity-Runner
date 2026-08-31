using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DescriptionView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image characterIcon;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button closeBtn;

    public event Action<CharacterRuntimeData> OnBuyClicked;
    public event Action OnCancel;

    public void Show(CharacterRuntimeData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[DescriptionView] null data!");
            return;
        }

        nameText.text = data.BaseData.displayName;
        descText.text = data.BaseData.description;
        characterIcon.sprite = data.BaseData.icon;
        priceText.text = $"{data.BaseData.price} Coin";
        gameObject.SetActive(true);

        buyBtn.onClick.RemoveAllListeners();
        buyBtn.onClick.AddListener(() => OnBuyClicked?.Invoke(data));

        cancelBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.AddListener(() => OnCancel?.Invoke());
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(() => OnCancel?.Invoke());
    }

    public void Hide() => gameObject.SetActive(false);
}
