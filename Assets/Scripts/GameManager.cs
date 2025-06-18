using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TankController controller1;
    [SerializeField] private TankController controller2;
    [SerializeField] private string levelSelection;

    void Update()
    {
        if (controller1.score == 10 || controller2.score == 10)
        {
            EndGame();
        }
    }

    public void EndGame()
    {
        SceneManager.LoadScene(levelSelection);
    }
}