namespace TMG.Domain.Common.Exceptions;

public sealed class AggregateStateException(string message) : Exception(message);
