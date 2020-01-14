using System;
using System.IO.Pipes;

namespace Schmellow.DiscordServices.Pinger
{
    public static class InstanceManager
    {
        public const string PIPE_NAME = "Schmellow.DiscordServices.Pinger.";

        public static void Run(ILogger logger, string instanceName)
        {
            bool run = true;
            while (run)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PIPE_NAME + instanceName, PipeDirection.InOut))
                    {
                        server.WaitForConnection();
                        switch(server.ReadByte())
                        {
                            case 1:
                                server.WriteByte(1);
                                break;
                            case 2:
                                run = false;
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (logger != null)
                        logger.Error(ex, ex.Message);
                    run = false;
                }
            }
        }

        public static bool IsRunning(ILogger logger, string instanceName)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PIPE_NAME + instanceName, PipeDirection.InOut))
                {
                    client.Connect(1000);
                    client.WriteByte(1);
                    return client.ReadByte() == 1;
                }
            }
            catch (TimeoutException)
            {
                // Suppress timeout exception
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.Error(ex, ex.Message);
            }
            return false;
        }

        public static void Stop(ILogger logger, string instanceName)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PIPE_NAME + instanceName, PipeDirection.Out))
                {
                    client.Connect(1000);
                    client.WriteByte(2);
                }
            }
            catch (TimeoutException)
            {
                // Suppress timeout exception
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.Error(ex, ex.Message);
            }
        }

    }
}
