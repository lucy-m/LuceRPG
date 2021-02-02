using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WelcomeScreen : MonoBehaviour
{
    public InputField ServerControl;
    public InputField UsernameControl;
    public InputField PasswordControl;

    public Config Config => Registry.ConfigLoader.Config;

    // Start is called before the first frame update
    private void Start()
    {
        var config = Registry.ConfigLoader.Config;
        ServerControl.text = config.BaseUrl;
        UsernameControl.text = config.Username;
        PasswordControl.text = config.Password;

        var action = new UnityAction<string>(OnEditFinished);

        ServerControl.onEndEdit.AddListener(action);
        UsernameControl.onEndEdit.AddListener(action);
        PasswordControl.onEndEdit.AddListener(action);
    }

    private void OnEditFinished(string s)
    {
        Config.BaseUrl = ServerControl.text;
        Config.Username = UsernameControl.text;
        Config.Password = PasswordControl.text;

        Registry.ConfigLoader.SaveConfig();
    }
}
