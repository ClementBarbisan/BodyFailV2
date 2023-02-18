using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FramesController : MonoBehaviour
{
    [SerializeField] GridLayoutGroup parentPanel;
    [SerializeField] NuitrackSDK.Frame.DrawSensorFrame sensorFrame;

    void Start()
    {
        if (NuitrackManager.Instance.devices.Count == 0)
            return;

        int size = Mathf.CeilToInt(Mathf.Sqrt(NuitrackManager.Instance.devices.Count));
        parentPanel.cellSize = new Vector2(Screen.width / size, Screen.height / size);

        for (int i = 0; i < NuitrackManager.Instance.devices.Count; i++)
        {
            sensorFrame.sensorId = i;
            sensorFrame.transform.localScale = Vector3.one;
            Instantiate(sensorFrame.gameObject, parentPanel.transform);
        }
    }
}
