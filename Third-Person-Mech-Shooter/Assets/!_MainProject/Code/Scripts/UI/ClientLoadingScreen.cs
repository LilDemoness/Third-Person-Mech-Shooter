using UnityEngine;

namespace UI
{
    public class ClientLoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;


        private void Start() => _canvasGroup.Hide();


        public void StartLoadingScreen(string sceneName)
        {
            _canvasGroup.Show();
        }
        public void UpdateLoadingScreen(string sceneName)
        {

        }
        public void StopLoadingScreen() => _canvasGroup.Hide();
    }
}