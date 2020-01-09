using System;
using System.IO.Pipes;

namespace Schmellow.DiscordServices.Pinger
{
    public static class InstanceHelper
    {
        public static void Run(ILogger logger, string instanceName)
        {
            bool run = true;
            while (run)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(Constants.PIPE_NAME + instanceName, PipeDirection.InOut))
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
                using (var client = new NamedPipeClientStream(".", Constants.PIPE_NAME + instanceName, PipeDirection.InOut))
                {
                    client.Connect(1000);
                    client.WriteByte(1);
                    if (client.ReadByte() == 1)
                        return true;
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
                using (var client = new NamedPipeClientStream(".", Constants.PIPE_NAME + instanceName, PipeDirection.Out))
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
