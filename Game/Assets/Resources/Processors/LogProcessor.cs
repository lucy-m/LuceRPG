using LuceRPG.Models;
using System.Collections;
using System.Collections.Generic;

namespace LuceRPG.Game.Processors
{
    public class LogProcessor : Processor<WithTimestamp.Model<ClientLogEntryModule.Payload>>
    {
        public override float PollPeriod => 8f;

        protected override IEnumerator Process(IEnumerable<WithTimestamp.Model<ClientLogEntryModule.Payload>> ts)
        {
            yield return Registry.Services.Comms.SendLogs(ts);
        }

        public void AddLog(ClientLogEntryModule.Payload payload)
        {
            var timestamped = WithTimestamp.create(Registry.TimestampProvider.Now, payload);
            Add(timestamped);
        }
    }
}
