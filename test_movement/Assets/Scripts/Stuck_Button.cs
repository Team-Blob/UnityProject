using UnityEngine;
using UnityEngine.SceneManagement;

public class Stuck_Button : MonoBehaviour
{
    public void Reset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
