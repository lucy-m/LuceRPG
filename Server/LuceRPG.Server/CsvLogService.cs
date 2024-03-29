﻿using LuceRPG.Models;
using LuceRPG.Server.Core;
using LuceRPG.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LuceRPG.Server
{
    public interface ICsvLogService
    {
        void EstablishLog(string clientId, string username);

        void AddProcessResult(IntentionProcessing.ProcessManyResult result);

        void AddClientLogs(
            string clientId,
            IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> logs
        );

        void AddBehaviourUpdateResult(BehaviourMapModule.UpdateResult result);

        void Flush();

        void AddIntentionQueue(IndexedIntentionModule.Model iIntention);
    }

    public class CsvLogService : ICsvLogService
    {
        private readonly string Directory;
        private readonly string ServerLogPath;
        private const string ServerLogName = "server.csv";

        private readonly Dictionary<string, string> _clientFileMap
            = new();

        private readonly ITimestampProvider _timestampProvider;
        private readonly ILogger<CsvLogService> _logger;
        private readonly Queue<string> _toWrite = new();

        public CsvLogService(
            ITimestampProvider timestampProvider,
            ILogger<CsvLogService> logger
        )
        {
            _timestampProvider = timestampProvider;
            _logger = logger;

            var now = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
            Directory = $"logs/{now}/";
            ServerLogPath = Directory + ServerLogName;
            _logger.LogInformation($"Logging to {Directory}");

            System.IO.Directory.CreateDirectory(Directory);
            System.IO.File.WriteAllLines(ServerLogPath, FormatFields.headers);
        }

        public void EstablishLog(string clientId, string username)
        {
            var fileName = $"client-{username}-{clientId}.csv";
            var filePath = Directory + fileName;

            System.IO.File.WriteAllLines(filePath, FormatFields.headers);
            AddServerLogs(ToLogString.clientJoined(_timestampProvider.Now, clientId, username));

            _clientFileMap[clientId] = fileName;
        }

        public void AddProcessResult(IntentionProcessing.ProcessManyResult result)
        {
            var logLines =
                ToLogString
                .processResult(_timestampProvider.Now, result)
                .ToArray();

            AddServerLogs(logLines);
        }

        public void AddBehaviourUpdateResult(BehaviourMapModule.UpdateResult result)
        {
            var logLines =
                ToLogString
                .behaviourUpdateResult(_timestampProvider.Now, result)
                .ToArray();

            AddServerLogs(logLines);
        }

        public void AddIntentionQueue(IndexedIntentionModule.Model iIntention)
        {
            var logLine = ToLogString.intentionQueue(_timestampProvider.Now, iIntention);

            AddServerLogs(logLine);
        }

        private void AddServerLogs(params string[] logs)
        {
            foreach (var log in logs)
            {
                _toWrite.Enqueue(log);
            }
        }

        public void Flush()
        {
            lock (this)
            {
                try
                {
                    var logs = _toWrite.ToArray();
                    System.IO.File.AppendAllLines(ServerLogPath, logs);
                    _toWrite.Clear();
                }
                catch
                {
                    _logger.LogError("Could not write logs to file");
                }
            }
        }

        public void AddClientLogs(
            string clientId,
            IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> logs
        )
        {
            if (_clientFileMap.TryGetValue(clientId, out var fileName))
            {
                var filePath = Directory + fileName;

                var lines =
                    logs
                    .OrderBy(l => l.timestamp)
                    .SelectMany(l => ClientLogEntryFormatter.create(l));

                System.IO.File.AppendAllLines(filePath, lines);
            }
            else
            {
                _logger.LogError($"Unable to add client logs for clientId");
            }
        }
    }

    public class TestCsvLogService : ICsvLogService
    {
        public void AddBehaviourUpdateResult(BehaviourMapModule.UpdateResult result)
        {
        }

        public void AddClientLogs(string clientId, IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> logs)
        {
        }

        public void AddIntentionQueue(IndexedIntentionModule.Model iIntention)
        {
        }

        public void AddProcessResult(IntentionProcessing.ProcessManyResult result)
        {
        }

        public void EstablishLog(string clientId, string username)
        {
        }

        public void Flush()
        {
        }
    }
}
