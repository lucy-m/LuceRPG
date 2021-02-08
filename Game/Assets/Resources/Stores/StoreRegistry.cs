using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuceRPG.Game.Stores
{
    public class StoreRegistry
    {
        public WorldStore World { get; private set; } = new WorldStore();
        public ConfigStore Config { get; private set; } = new ConfigStore();
    }
}
