using System.Net;
using TwinCAT.Ads;
using TwinCAT.Ads.Configuration;
using TwinCAT.Ads.TcpRouter;

namespace AdsSync
{
    /// <summary>
    /// 
    /// </summary>
    internal class AdsRouter
    {
        #region fields & events
        /// <summary> The TCPIP router for replacing the TwinCAT Runtime </summary>
        public readonly AmsTcpIpRouter router;
        #endregion

        #region constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ownNetId"> The target Net ID for this device </param>
        /// <param name="targetNetId"> The  Net ID of the client </param>
        public AdsRouter(AmsNetId ownNetId, AmsNetId targetNetId)
        {
            router = new(ownNetId);
            string ip = string.Join(".", targetNetId.ToString().Split('.').Take(4));
            Route route = new("AdsSyncClient", targetNetId, [IPAddress.Parse(ip)]);

            router.AddRoute(route);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Starts the TCP/IP router asynchronously.
        /// </summary>
        /// <returns> Return TRUE if startet </returns>
        /// <exception cref="TimeoutException"> Thrown when not started in under 5 seconds. </exception>
        public async Task StartAsync()
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            await router.StartAsync(cts.Token);
            if (cts.IsCancellationRequested)
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// Stops the TCP/IP router.
        /// </summary>
        public void Stop()
        {
            router.Stop();
        }
        #endregion
    }
}
