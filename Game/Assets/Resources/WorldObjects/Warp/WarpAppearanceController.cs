using LuceRPG.Models;
using UnityEngine;

public class WarpAppearanceController : MonoBehaviour
{
    public GameObject Door;
    public GameObject Mat;

    private WarpModule.Appearance _appearance;

    public WarpModule.Appearance Appearance
    {
        get => _appearance;
        set
        {
            _appearance = value;
            if (_appearance.IsDoor)
            {
                Door.SetActive(true);
                Mat.SetActive(false);
            }
            else if (_appearance.IsMat)
            {
                Door.SetActive(false);
                Mat.SetActive(true);
            }
        }
    }
}
