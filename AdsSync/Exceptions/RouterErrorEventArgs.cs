namespace AdsSync.Exceptions
{
    /// <summary>
    /// Contains information about a router error that has occurred
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="exception"> Thrown exception </param>
    public sealed class RouterErrorEventArgs(Exception exception) : EventArgs
    {
        #region properties
        /// <summary> Thrown exception </summary>
        public Exception Exception { get; } = exception;

        #endregion
    }
}
