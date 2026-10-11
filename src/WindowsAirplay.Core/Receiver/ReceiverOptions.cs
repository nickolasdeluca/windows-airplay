namespace WindowsAirplay.Core.Receiver;
public sealed record ReceiverOptions(string Name, string RuntimeDirectory, int PortBase = 35000, int Width = 1920, int Height = 1080, int RefreshRate = 60);
