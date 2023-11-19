using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
    public int level;
    public void Select()
    {
        SceneManager.LoadScene("Puzzle_Classic_Level "+ level);
    }

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Select);
    }
}
