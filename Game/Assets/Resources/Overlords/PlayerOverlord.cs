using LuceRPG.Game.Services;
using LuceRPG.Game.Stores;
using LuceRPG.Models;
using LuceRPG.Utility;
using System;
using System.Collections;
using UnityEngine;

namespace LuceRPG.Game.Overlords
{
    public class PlayerOverlord : MonoBehaviour
    {
        private ITimestampProvider TimestampProvider => Registry.Providers.Timestamp;
        private WorldStore WorldStore => Registry.Stores.World;
        private CursorStore CursorStore => Registry.Stores.Cursor;
        private IntentionService IntentionService => Registry.Services.Intentions;

        private string Id => Registry.Stores.World.PlayerId;

        // Start is called before the first frame update
        private void Start()
        {
            StartCoroutine(PollInput());
        }

        private IEnumerator PollInput()
        {
            while (true)
            {
                var vertIn = Registry.Providers.Input.GetVertIn();
                var horzIn = Registry.Providers.Input.GetHorzIn();

                if (vertIn > 0)
                {
                    var intention = IntentionModule.Type.NewMove(Id, DirectionModule.Model.North, 1);
                    yield return IntentionService.Dispatch(intention);
                    yield return SpinWhileBusy();
                }

                if (vertIn < 0)
                {
                    var intention = IntentionModule.Type.NewMove(Id, DirectionModule.Model.South, 1);
                    yield return IntentionService.Dispatch(intention);
                    yield return SpinWhileBusy();
                }

                if (horzIn > 0)
                {
                    var intention = IntentionModule.Type.NewMove(Id, DirectionModule.Model.East, 1);
                    yield return IntentionService.Dispatch(intention);
                    yield return SpinWhileBusy();
                }

                if (horzIn < 0)
                {
                    var intention = IntentionModule.Type.NewMove(Id, DirectionModule.Model.West, 1);
                    yield return IntentionService.Dispatch(intention);
                    yield return SpinWhileBusy();
                }

                var playerModel = WorldStore.GetObject(Id);
                var mousePosition = CursorStore.Position;

                if (playerModel.HasValue() && mousePosition != null)
                {
                    var lookDirection = GetLookDirection(mousePosition, playerModel.Value.value);

                    if (lookDirection != playerModel.Value.value.facing)
                    {
                        var intention = IntentionModule.Type.NewTurnTowards(Id, lookDirection);
                        yield return IntentionService.Dispatch(intention);
                    }
                }

                yield return null;
            }
        }

        private static DirectionModule.Model GetLookDirection(
            PointModule.Model mousePosition,
            WorldObjectModule.Payload playerObject
        )
        {
            var playerPos = playerObject.btmLeft;

            var xDiff = mousePosition.x - playerPos.x;
            var yDiff = mousePosition.y - playerPos.y;

            if (xDiff == 0 && yDiff == 0)
            {
                // Do nothing
                return playerObject.facing;
            }
            else
            {
                // Look along the furthest away axis
                var lookAlongX = Math.Abs(xDiff) > Math.Abs(yDiff);

                if (lookAlongX)
                {
                    return xDiff > 0 ? DirectionModule.Model.East : DirectionModule.Model.West;
                }
                else
                {
                    return yDiff > 0 ? DirectionModule.Model.North : DirectionModule.Model.South;
                }
            }
        }

        public IEnumerator SpinWhileBusy()
        {
            var busyUntil = Registry.Processors.Intentions.BusyUntil(Id);

            if (!busyUntil.HasValue)
            {
                yield return null;
            }
            else
            {
                while (TimestampProvider.Now < busyUntil.Value)
                {
                    yield return null;
                }
            }
        }
    }
}
