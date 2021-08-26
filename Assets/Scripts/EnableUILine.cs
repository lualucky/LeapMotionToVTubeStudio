using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnableUILine : MonoBehaviour
{
    public List<GameObject> ToggleObjects;

    private void Start()
    {
        GetComponent<Toggle>().onValueChanged.AddListener(Toggle);
    }

    public void Toggle(bool enable)
    {
        foreach(GameObject obj in ToggleObjects)
        {
            obj.SetActive(enable);
        }
    }
}
