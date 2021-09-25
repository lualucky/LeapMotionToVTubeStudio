using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineData : MonoBehaviour
{
    public Toggle EnableToggle;
    public Text NameText;
    public Text ParameterNameText;
    public InputField OffsetInput;
    public Text ValueText;

    public List<GameObject> ToggleObjects;

    private void Start()
    {
        EnableToggle.onValueChanged.AddListener(Toggle);
    }

    public void Toggle(bool enable)
    {
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

    public void SetOffset(float value)
    {
        OffsetInput.text = "" + value;
    }

    public float Offset()
    {
        if(OffsetInput)
            return float.Parse(OffsetInput.text);
        return 0;
    }

    public void RegisterOffsetCallback(UnityEngine.Events.UnityAction<string> offsetEvent)
    {
        if(OffsetInput)
            OffsetInput.onValueChanged.AddListener(offsetEvent);
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
