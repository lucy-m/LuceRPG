using LuceRPG.Models;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LuceRPG.Utility;
using LuceRPG.Game.Models;

public class OptimisticIntentionProcessor : MonoBehaviour
{
    public static OptimisticIntentionProcessor Instance = null;
    public float PollPeriod = 0.01f;

    private readonly Dictionary<string, long> _intentions
        = new Dictionary<string, long>();

    private FSharpMap<string, long> _objectBusyMap
        = MapModule.Empty<string, long>();

    private readonly Queue<IntentionProcessing.IndexedIntentionModule.Model> _delayed
        = new Queue<IntentionProcessing.IndexedIntentionModule.Model>();

    private readonly Dictionary<string, Dictionary<int, WorldEventModule.Model>> _eventsProduced
        = new Dictionary<string, Dictionary<int, WorldEventModule.Model>>();

    private readonly ITimestampProvider _timestampProvider = new TimestampProvider();

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

    public long? BusyUntil(string objectId)
    {
        var timestamp = MapModule.TryFind(objectId, _objectBusyMap);

        if (timestamp.HasValue())
        {
            return timestamp.Value;
        }
        else
        {
            return null;
        }
    }

    public bool DidProcess(string intentionId)
    {
        return _intentions.TryGetValue(intentionId, out _);
    }

    public void CheckEvent(WorldEventModule.Model e)
    {
        if (_eventsProduced.TryGetValue(e.resultOf, out var indexDict))
        {
            if (indexDict.TryGetValue(e.index, out var optimisticEvent))
            {
                if (!e.t.Equals(optimisticEvent.t))
                {
                    Debug.LogError($"Non-matching events produced from intention {e.resultOf} at index {e.index}");
                }
            }
            else
            {
                Debug.LogError($"Extra event produced from intention {e.resultOf} at index {e.index}");
            }
        }
        else
        {
            Debug.LogWarning($"Event produced from intention {e.resultOf} not applied optimistically");
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="intention"></param>
    /// <returns>ID used for the intention</returns>
    public string Process(IntentionModule.Type intention)
    {
        var timestamp = _timestampProvider.Now;
        var payload = IntentionModule.makePayload("", intention);
        var withId = WithId.create(payload);
        var withTimestamp = WithTimestamp.create(timestamp, withId);
        var indexed = IntentionProcessing.IndexedIntentionModule.create(withTimestamp);

        _intentions[withId.id] = timestamp;

        DoProcess(new List<IntentionProcessing.IndexedIntentionModule.Model> { indexed });

        return withId.id;
    }

    private IEnumerator ProcessDelayed()
    {
        while (true)
        {
            var intentions = _delayed.ToArray().OrderBy(i => i.tsIntention.timestamp);
            _delayed.Clear();

            DoProcess(intentions);

            yield return new WaitForSeconds(PollPeriod);
        }
    }

    private void DoProcess(
        IEnumerable<IntentionProcessing.IndexedIntentionModule.Model> intentions
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

        WorldLoader.Instance.ApplyUpdate(processResult.events, UpdateSource.Game);

        foreach (var e in processResult.events)
        {
            if (!_eventsProduced.TryGetValue(e.resultOf, out var indexDict))
            {
                _eventsProduced[e.resultOf] = new Dictionary<int, WorldEventModule.Model>();
                indexDict = _eventsProduced[e.resultOf];
            }

            indexDict[e.index] = e;
        }

        foreach (var d in processResult.delayed)
        {
            _delayed.Enqueue(d);
        }
    }
}

