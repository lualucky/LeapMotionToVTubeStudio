using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineData : MonoBehaviour
{
    public Toggle EnableToggle;
    public Text NameText;
    public Text ParameterNameText;
    public InputField SmoothInput;
    public Text ValueText;

    public List<GameObject> ToggleObjects;

    private void Start()
    {
        if (PlayerPrefs.HasKey(ParameterNameText.text + "Enabled"))
        {
            EnableToggle.isOn = PlayerPrefs.GetInt(ParameterNameText.text + "Enabled") > 0;
        }

        EnableToggle.onValueChanged.AddListener(Toggle);
    }

    public void Toggle(bool enable)
    {
        PlayerPrefs.SetInt(ParameterNameText.text + "Enabled", EnableToggle.isOn ? 1 : 0);

        foreach (GameObject obj in ToggleObjects)
        {
            obj.SetActive(enable);
        }
    }

    public void SetName(string name)
    {
        NameText.text = name;
    }

    public string Name()
    {
        return NameText.text;
    }

    public void SetParameterName(string parameterName)
    {
        ParameterNameText.text = parameterName;
    }

    public string ParameterName()
    {
        return ParameterNameText.text;
    }

    public void SetSmooth(int value)
    {
        SmoothInput.text = "" + value;
    }

    public int Smooth()
    {
        if(SmoothInput)
            return int.Parse(SmoothInput.text);
        return 0;
    }

    public void RegisterSmoothCallback(UnityEngine.Events.UnityAction<string> smoothEvent)
    {
        if(SmoothInput)
            SmoothInput.onValueChanged.AddListener(smoothEvent);
    }

    public void SetValue(float value)
    {
        ValueText.text = "" + value;
    }

    public bool Enabled()
    {
        return EnableToggle.isOn;
    }
}
