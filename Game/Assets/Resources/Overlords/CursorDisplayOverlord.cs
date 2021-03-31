using LuceRPG.Game;
using LuceRPG.Game.Providers;
using LuceRPG.Models;
using UnityEngine;

public class CursorDisplayOverlord : MonoBehaviour
{
    public CursorDisplayController CursorDisplay;
    private IInputProvider InputProvider => Registry.Providers.Input;

    // Update is called once per frame
    private void Update()
    {
        if (Camera.current != null && CursorDisplay != null)
        {
            var mousePosition = InputProvider.GetMousePosition();
            var world = Camera.current.ScreenToWorldPoint(mousePosition);

            var point = PointModule.create((int)world.x, (int)world.y);
            CursorDisplay.Position = point;
        }
    }
}
