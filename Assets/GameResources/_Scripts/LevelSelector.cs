using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
    private int levelCount;
    public void Start()
    {
        levelCount = int.Parse(GetComponentInChildren<Text>().text) - 1;
        GetComponent<Button>().onClick.AddListener(Select);
    }
    public void Select()
    {
        LevelController.Instance.ChangeLevel(levelCount);

    }
}
