﻿using LuceRPG.Utility;

public static class _Registry
{
    public static ICommsService CommsService { get; set; } = new CommsService();
    public static IConfigLoader ConfigLoader { get; set; } = new ConfigLoader("config.json");
    public static IInputProvider InputProvider { get; set; } = new InputProvider();
    public static ITimestampProvider TimestampProvider { get; set; } = new TimestampProvider();
    public static WorldStore WorldStore { get; set; } = new WorldStore();
}
