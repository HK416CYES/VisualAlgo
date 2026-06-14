using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject producerPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Btn_SortScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

    public void Btn_GridMapNavScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(2);
    }

    public void Btn_BinaryTreeScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(3);
    }

    /// <summary>
    /// 退出游戏；在编辑器内则停止播放。
    /// </summary>
    public void Btn_QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    public void Btn_OpenProducerPanel()
    {
        if (producerPanel != null) producerPanel.SetActive(true);
    }
}
