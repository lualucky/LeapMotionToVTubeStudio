using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject ShowSettingsButton;
    public GameObject HideSettingsButton;
    public GameObject SettingsPanel;

    // Start is called before the first frame update
    void Start()
    {
        ShowSettingsButton.GetComponent<Button>().onClick.AddListener(ToggleSettingsMenu);
        HideSettingsButton.GetComponent<Button>().onClick.AddListener(ToggleSettingsMenu);
    }

    public void ToggleSettingsMenu()
    {
        ShowSettingsButton.SetActive(!ShowSettingsButton.activeInHierarchy);
        SettingsPanel.SetActive(!SettingsPanel.activeInHierarchy);
    }
}
