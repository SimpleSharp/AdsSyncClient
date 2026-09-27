namespace AdsSync.Exceptions
{
    /// <summary>
    /// Contains information about a connection error that has occurred
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="exception"> Thrown exception </param>
    /// <param name="willRetry"> Will try to reconnect (does not affect the function) </param>
    public sealed class ConnectionErrorEventArgs(Exception exception, bool willRetry) : EventArgs
    {
        #region properties
        /// <summary> Thrown exception </summary>
        public Exception Exception { get; } = exception;
        /// <summary> Will try to reconnect (does not affect the function) </summary>
        public bool WillRetry { get; } = willRetry;

        #endregion
    }
}
