using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LuceRPG.Utility;

public class OptimisticIntentionProcessor : MonoBehaviour
{
    public static OptimisticIntentionProcessor Instance = null;
    public float PollPeriod = 0.1f;

    private readonly Dictionary<string, long> _intentions
        = new Dictionary<string, long>();

    private FSharpMap<string, long> _objectBusyMap
        = MapModule.Empty<string, long>();

    private readonly Queue<WithTimestamp.Model<WithId.Model<IntentionModule.Payload>>> _delayed
        = new Queue<WithTimestamp.Model<WithId.Model<IntentionModule.Payload>>>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(GetComponent<OptimisticIntentionProcessor>());
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        StartCoroutine(ProcessDelayed());
    }

    public bool ShouldIgnore(string intentionId)
    {
        return _intentions.TryGetValue(intentionId, out _);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="intention"></param>
    /// <returns>ID used for the intention</returns>
    public string Process(IntentionModule.Type intention)
    {
        var timestamp = TimestampProvider.Now;
        var payload = IntentionModule.makePayload("", intention);
        var withId = WithId.create(payload);
        var withTimestamp = WithTimestamp.create(timestamp, withId);
        _intentions[withId.id] = timestamp;

        DoProcess(new List<WithTimestamp.Model<WithId.Model<IntentionModule.Payload>>> { withTimestamp });

        Debug.Log($"Intention {withId.id} optimistically applied");

        return withId.id;
    }

    private IEnumerator ProcessDelayed()
    {
        while (true)
        {
            var intentions = _delayed.ToArray().OrderBy(i => i.timestamp);
            _delayed.Clear();

            DoProcess(intentions);

            yield return new WaitForSeconds(PollPeriod);
        }
    }

    private void DoProcess(
        IEnumerable<WithTimestamp.Model<WithId.Model<IntentionModule.Payload>>> intentions
    )
    {
        var processResult = IntentionProcessing.processMany(
            DateTime.UtcNow.Ticks,
            FSharpOption<FSharpMap<string, string>>.None,
            _objectBusyMap,
            WorldLoader.Instance.World,
            intentions
        );

        _objectBusyMap = processResult.objectBusyMap;

        WorldLoader.Instance.ApplyUpdate(processResult.events, true);

        foreach (var d in processResult.delayed)
        {
            _delayed.Enqueue(d);
        }
    }
}

