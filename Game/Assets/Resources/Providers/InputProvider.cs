using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuceRPG.Game.Providers
{
    public interface IInputProvider
    {
        public float GetVertIn();

        public float GetHorzIn();
    }

    public class InputProvider : IInputProvider
    {
        public float GetHorzIn()
        {
            return Input.GetAxis("Horizontal");
        }

        public float GetVertIn()
        {
            return Input.GetAxis("Vertical");
        }
    }

    public class TestInputProvider : IInputProvider
    {
        public float HorzIn = 0;
        public float VertIn = 0;

        public float GetHorzIn()
        {
            return HorzIn;
        }

        public float GetVertIn()
        {
            return VertIn;
        }
    }
}
