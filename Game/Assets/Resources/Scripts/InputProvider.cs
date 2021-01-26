using UnityEngine;

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
