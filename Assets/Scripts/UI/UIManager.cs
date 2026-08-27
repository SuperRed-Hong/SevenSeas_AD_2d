using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{

    public void StartGame()
    {
        SceneManager.LoadScene("Scene1");
    }
}
