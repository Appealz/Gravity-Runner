using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    [SerializeField] Image charIcon;
    [SerializeField] GameObject lockObj;
    [SerializeField] Button myBtn;

    private CharacterRuntimeData runtimeData;

    public event Action<CharacterRuntimeData> OnClicked;
    public void Init(CharacterRuntimeData data)
    {
        if (data == null)
        {
            Debug.LogError("[CharacterButton] Init data is NULL!");
            return;
        }

        runtimeData = data; 
        charIcon.sprite = data.BaseData.icon;

        lockObj.SetActive(!data.IsUnlocked);

        myBtn.onClick.RemoveAllListeners();
        myBtn.onClick.AddListener(() => OnClicked?.Invoke(runtimeData));
    }

    public CharacterRuntimeData RuntimeData => runtimeData;

    public void Refresh(CharacterRuntimeData data)
    {
        runtimeData = data;
        lockObj.SetActive(!data.IsUnlocked);
    }
}
