namespace Borough.Core.Tables;

/// <summary>
/// A handle was resolved after its row had been freed, or was never allocated at all.
/// </summary>
/// <remarks>
/// <b>This exists so that the failure is loud.</b> Without the generation counter the same handle
/// would resolve to whichever row moved into the slot, and the read would succeed — which in a
/// deterministic simulation is a divergence rather than a crash: two runs disagree, replay reports
/// success, and nothing points at the cause. adr/0004 names this as the reason handles are
/// generational and not bare indices.
/// </remarks>
public sealed class StaleHandleException : InvalidOperationException
{
    internal StaleHandleException(string table, uint index, uint generation)
        : base($"handle {{index {index}, generation {generation}}} into table '{table}' is stale.")
    {
    }

    /// <inheritdoc cref="InvalidOperationException()"/>
    public StaleHandleException()
    {
    }

    /// <inheritdoc cref="InvalidOperationException(string)"/>
    public StaleHandleException(string message)
        : base(message)
    {
    }

    /// <inheritdoc cref="InvalidOperationException(string, Exception)"/>
    public StaleHandleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
