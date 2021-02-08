using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WelcomeScreen : MonoBehaviour
{
    public InputField ServerControl;
    public InputField UsernameControl;
    public InputField PasswordControl;
    public Button JoinGameButton;
    public Text ErrorDisplay;

    public Config Config => Registry.ConfigLoader.Config;

    // Start is called before the first frame update
    private void Start()
    {
        var config = Registry.ConfigLoader.Config;
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
        Config.BaseUrl = ServerControl.text;
        Config.Username = UsernameControl.text;
        Config.Password = PasswordControl.text;

        Registry.ConfigLoader.SaveConfig();
    }

    private void OnJoinGame()
    {
        StartCoroutine(Registry.CommsService.LoadGame(
            (playerId, world) =>
            {
                SceneManager.LoadScene("GameLoader");
            },
            (err) => ErrorDisplay.text = err
        ));
    }
}
