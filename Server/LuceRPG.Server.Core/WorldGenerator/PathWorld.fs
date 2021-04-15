namespace LuceRPG.Server.Core

open LuceRPG.Models

module PathWorld =

    module Tile =

        type Model =
            {
                links: Direction List
            }
