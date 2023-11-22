using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChoosePuzzleFromList;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance;
    public GameController gameController;
    public GameObject currentPuzzle;

    public GameObject uiCanvas;
    public GameObject GameRoot;

    public int currentLevelCount = 0;
    public LevelData[] levels;
    public GameObject[] puzzles;
    void Awake()
    {
        //gameController = FindObjectOfType<GameController>();
        Instance = this;
        levels = new LevelData[10]
        {
            new LevelData(1, 60, 1, puzzles[0]),
            new LevelData(2, 120, 2, puzzles[1]),
            new LevelData(3, 121, 3, puzzles[2]),
            new LevelData(4, 151, 4, puzzles[3]),
            new LevelData(5, 181, 5, puzzles[4]),
            new LevelData(6, 361, 6, puzzles[5]),
            new LevelData(7, 601, 7, puzzles[6]),
            new LevelData(8, 721, 8, puzzles[7]),
            new LevelData(9, 841, 9, puzzles[8]),
            new LevelData(10, 1081, 10, puzzles[9]),
        };
    }

    public void ChangeLevel(int levelCount)
    {
        if (gameController == null)
        {
            gameController = FindObjectOfType<GameController>();
            if (gameController == null)
            {
                Debug.LogError("Can not found GameController in the Scene");
            }
        }
        if (currentPuzzle != null)
            Destroy(currentPuzzle);
        uiCanvas.SetActive(false);
        GameRoot.SetActive(true);
        gameController.timer = levels[levelCount].timer;
        gameController.hintLimit = levels[levelCount].hintLimit;
        currentPuzzle = Instantiate(levels[levelCount].puzzle);
        currentLevelCount = levelCount;
        gameController.Init();
    }

    public void NextLevel()
    {
        if (gameController == null)
        {
            gameController = FindObjectOfType<GameController>();
            if (gameController == null)
            {
                Debug.LogError("Can not found GameController in the Scene");
            }
        }
        if (currentPuzzle != null)
            Destroy(currentPuzzle);
        if(currentLevelCount+1 >= levels.Length)
        {
            currentLevelCount = -1;
        }
        gameController.timer = levels[currentLevelCount+1].timer;
        gameController.hintLimit = levels[currentLevelCount+1].hintLimit;
        currentPuzzle = Instantiate(levels[currentLevelCount+1].puzzle);
        Debug.Log("Instantiate");
        currentLevelCount += 1;
        gameController.Init();
        Debug.Log("Init");
    }

    public void Exit()
    {
        uiCanvas.SetActive(true);
        GameRoot.SetActive(false);
    }
}
