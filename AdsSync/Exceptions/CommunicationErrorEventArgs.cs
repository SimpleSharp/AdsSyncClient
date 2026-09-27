namespace AdsSync.Exceptions
{
    /// <summary>
    /// Contains information about a commmunication error that has occurred
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="exception"> Thrown exception </param>
    /// <param name="name"> Name of the item to be read or writtentyName </param>
    /// <param name="occurredWhileReading"> Exception occured while reading </param>
    /// <param name="occurredWhileWriting"> Exception occured while wiriting </param>
    public sealed class CommunicationErrorEventArgs(Exception exception, string? name, bool occurredWhileReading, bool occurredWhileWriting) : EventArgs
    {
        #region properties
        /// <summary> Thrown exception </summary>
        public Exception Exception { get; } = exception;
        /// <summary> The name of the item to be read or written </summary>
        public string Name { get; } = name;
        /// <summary> Exception occured while reading </summary>
        public bool OccurredWhileReading { get; } = occurredWhileReading;
        /// <summary> Exception occured while wiriting </summary>
        public bool OccurredWhileWriting { get; } = occurredWhileWriting;
        #endregion

    }
}
