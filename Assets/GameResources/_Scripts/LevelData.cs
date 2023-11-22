using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData
{
    public int levelCount;
    public int timer;
    public int hintLimit;
    public GameObject puzzle;

    public LevelData(int levelCount,int time, int hintLimit,GameObject puzzle)
    {
        this.levelCount = levelCount;
        this.timer = time;
        this.hintLimit = hintLimit;
        this.puzzle = puzzle;
    }
}
