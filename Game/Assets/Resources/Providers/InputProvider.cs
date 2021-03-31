using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LuceRPG.Game.Providers
{
    public interface IInputProvider
    {
        public float GetVertIn();

        public float GetHorzIn();

        public Vector3 GetMousePosition();
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

        public Vector3 GetMousePosition()
        {
            return Input.mousePosition;
        }
    }

    public class TestInputProvider : IInputProvider
    {
        public float HorzIn = 0;
        public float VertIn = 0;
        public Vector3 MousePosition = Vector3.zero;

        public float GetHorzIn()
        {
            return HorzIn;
        }

        public float GetVertIn()
        {
            return VertIn;
        }

        public Vector3 GetMousePosition()
        {
            return MousePosition;
        }
    }
}
