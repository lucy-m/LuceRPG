using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LuceRPG.Game.Overlords
{
    public class WelcomeOverlord : MonoBehaviour
    {
        public InputField ServerControl;
        public InputField UsernameControl;
        public InputField PasswordControl;
        public Button JoinGameButton;
        public Text ErrorDisplay;

        // Start is called before the first frame update
        private void Start()
        {
            Registry.Services.ConfigLoader.LoadConfig();

            var config = Registry.Stores.Config.Config;
            ServerControl.text = config.BaseUrl;
            UsernameControl.text = config.Username;
            PasswordControl.text = config.Password;
            ErrorDisplay.text = "";

            var configUpdateAction = new UnityAction<string>(OnEditFinished);

            ServerControl.onEndEdit.AddListener(configUpdateAction);
            UsernameControl.onEndEdit.AddListener(configUpdateAction);
            PasswordControl.onEndEdit.AddListener(configUpdateAction);

            var onJoinGame = new UnityAction(OnJoinGame);

            JoinGameButton.onClick.AddListener(onJoinGame);
        }

        private void OnEditFinished(string s)
        {
            Registry.Stores.Config.Config.BaseUrl = ServerControl.text;
            Registry.Stores.Config.Config.Username = UsernameControl.text;
            Registry.Stores.Config.Config.Password = PasswordControl.text;

            Registry.Services.ConfigLoader.SaveConfig();
        }

        private void OnJoinGame()
        {
            StartCoroutine(Registry.Services.Comms.LoadWorld(
                (payload) =>
                {
                    SceneManager.LoadScene("GameLoader");
                },
                (err) => ErrorDisplay.text = err
            ));
        }
    }
}
