using LuceRPG.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public int Id = 0;

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        var vertIn = Input.GetAxis("Vertical");

        if (vertIn > 0)
        {
            var intention = IntentionModule.Model.NewMove(Id, DirectionModule.Model.North, 1);
            IntentionDispatcher.Instance.Dispatch(intention);
        }
    }
}
