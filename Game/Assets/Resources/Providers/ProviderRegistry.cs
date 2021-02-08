namespace LuceRPG.Game.Providers
{
    public class ProviderRegistry
    {
        public IInputProvider Input { get; set; } = new InputProvider();
    }
}
