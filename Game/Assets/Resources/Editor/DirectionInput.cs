using LuceRPG.Models;

namespace LuceRPG.Game.Editor
{
    public enum DirectionInput
    {
        North,
        South,
        East,
        West
    }

    public static class DirectionInputToModel
    {
        public static DirectionModule.Model ToModel(this DirectionInput input)
        {
            return input switch
            {
                DirectionInput.North => DirectionModule.Model.North,
                DirectionInput.East => DirectionModule.Model.East,
                DirectionInput.West => DirectionModule.Model.West,
                _ => DirectionModule.Model.South,
            };
        }

        public static DirectionInput ToInput(this DirectionModule.Model model)
        {
            if (model.IsNorth)
            {
                return DirectionInput.North;
            }
            else if (model.IsEast)
            {
                return DirectionInput.East;
            }
            else if (model.IsWest)
            {
                return DirectionInput.West;
            }
            else
            {
                return DirectionInput.South;
            }
        }
    }
}
