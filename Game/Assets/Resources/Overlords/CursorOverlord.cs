using LuceRPG.Game;
using LuceRPG.Game.Providers;
using LuceRPG.Game.Stores;
using LuceRPG.Models;
using LuceRPG.Utility;
using Microsoft.FSharp.Collections;
using UnityEngine;

public class CursorOverlord : MonoBehaviour
{
    private IInputProvider InputProvider => Registry.Providers.Input;
    private CursorStore CursorStore => Registry.Stores.Cursor;
    private WorldStore WorldStore => Registry.Stores.World;

    // Update is called once per frame
    private void Update()
    {
        var tWorldPosition = InputProvider.GetMousePosition(Camera.current);

        if (tWorldPosition.HasValue && WorldStore.HasWorld())
        {
            var worldPosition = tWorldPosition.Value;
            var point = PointModule.create((int)worldPosition.x, (int)worldPosition.y);
            CursorStore.Position = point;

            var objectAtPosition = MapModule.TryFind(point, WorldStore.World.blocked);

            if (objectAtPosition.HasValue() && objectAtPosition.Value.IsObject)
            {
                var obj = (objectAtPosition.Value as WorldModule.BlockedType.Object).Item;
                CursorStore.CursorOverObject = obj;
            }
            else
            {
                CursorStore.CursorOverObject = null;
            }
        }

        var mouseX = InputProvider.GetMouseX();
        var mouseY = InputProvider.GetMouseY();
        var mouseMoved = Mathf.Abs(mouseX) + Mathf.Abs(mouseY) > 0.01;

        CursorStore.MouseMoved = mouseMoved;
    }
}
