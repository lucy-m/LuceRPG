using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Registry
{
    public static ICommsService CommsService { get; set; } = new CommsService();
    public static IConfigLoader ConfigLoader { get; set; } = new ConfigLoader("config.json");
}
