using LuceRPG.Game.Editor;
using LuceRPG.Models;
using UnityEngine;

/// <summary>
/// Propagates directional updates to all child DirectionalSpriteControllers
/// </summary>
[ExecuteAlways]
public class DirectionalSpriteRoot : MonoBehaviour
{
    [SerializeField]
    private DirectionInput _testDirection = DirectionInput.South;

    private DirectionModule.Model _lastDirection;

    public DirectionModule.Model Direction
    {
        get => _lastDirection;
        set
        {
            if (value != _lastDirection && value != null)
            {
                var directionalSprites = GetComponentsInChildren<DirectionalSpriteController>();

                foreach (var s in directionalSprites)
                {
                    s.Direction = value;
                }

                _lastDirection = value;
            }
        }
    }

    private void Update()
    {
        if (Application.isEditor)
        {
            Direction = _testDirection.ToModel();
        }
    }
}
