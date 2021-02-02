using LuceRPG.Models;
using LuceRPG.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LogDispatcher : MonoBehaviour
{
    public static LogDispatcher Instance = null;
    public float PollPeriod = 8f;

    private List<WithTimestamp.Model<ClientLogEntryModule.Payload>> _logs
        = new List<WithTimestamp.Model<ClientLogEntryModule.Payload>>();

    private ICommsService CommsService => Registry.CommsService;
    private ITimestampProvider TimestampProvider => Registry.TimestampProvider;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<LogDispatcher>());
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        StartCoroutine(DoProcess());
    }

    public void AddLog(ClientLogEntryModule.Payload payload)
    {
        var timestamped = WithTimestamp.create(TimestampProvider.Now, payload);
        _logs.Add(timestamped);
    }

    private IEnumerator DoProcess()
    {
        while (true)
        {
            if (_logs.Any())
            {
                Debug.Log("Sending logs to server");

                yield return CommsService.SendLogs(_logs);
                _logs = new List<WithTimestamp.Model<ClientLogEntryModule.Payload>>();
            }

            yield return new WaitForSeconds(PollPeriod);
        }
    }
}
