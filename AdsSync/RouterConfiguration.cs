using System.Net;
using TwinCAT.Ads;

namespace AdsSync
{
    /// <summary>
    /// Describes the in-process ADS router hosted by an <see cref="AdsSyncClient"/>.
    /// </summary>
    public sealed class RouterConfiguration
    {
        #region properties
        /// <summary> Gets the NetId under which the local machine announces itself to the target. </summary>
        public AmsNetId LocalNetId { get; }
        /// <summary> Gets the host name or IP address of the target system. </summary>
        public string RemoteHost { get; }
        /// <summary> Gets the AMS address (NetId and port) of the target system. </summary>
        public AmsAddress TargetAddress { get; }
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance with a numeric IP address.
        /// </summary>
        /// <param name="localNetId"> The NetId under which the local machine announces itself to the target. </param>
        /// <param name="remoteIp"> The IPv4 address of the target system (e.g. "192.168.1.10"). </param>
        /// <param name="targetAddress"> The AMS address (NetId and port) of the target system. </param>
        /// <exception cref="ArgumentException"> Thrown when <paramref name="remoteIp"/> is not a valid IPv4 address. </exception>
        public RouterConfiguration(AmsNetId localNetId, IPAddress remoteIp, AmsAddress targetAddress)
        {
            ArgumentNullException.ThrowIfNull(localNetId);
            ArgumentNullException.ThrowIfNull(targetAddress);
            LocalNetId = localNetId;
            RemoteHost = remoteIp.ToString();
            TargetAddress = targetAddress;
        }

        /// <summary>
        /// Initializes a new instance with a host name or IP address.
        /// </summary>
        /// <param name="localNetId"> The NetId under which the local machine announces itself to the target. </param>
        /// <param name="remoteHost"> The host name (e.g. "plc-02.firma.local") or IP address of the target system. </param>
        /// <param name="targetAddress"> The AMS address (NetId and port) of the target system. </param>
        public RouterConfiguration(AmsNetId localNetId, string remoteHost, AmsAddress targetAddress)
        {
            ArgumentNullException.ThrowIfNull(localNetId);
            ArgumentException.ThrowIfNullOrWhiteSpace(remoteHost);
            ArgumentNullException.ThrowIfNull(targetAddress);
            LocalNetId = localNetId;
            RemoteHost = remoteHost;
            TargetAddress = targetAddress;
        }
        #endregion
    }
}
