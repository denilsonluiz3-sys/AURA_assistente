namespace AURA.Core.Runtime;

public enum CellNetworkAccess
{
    Inherit,
    Disabled,
    Enabled
}

public sealed class CellNetworkPolicy
{
    public CellNetworkAccess Access { get; init; } = CellNetworkAccess.Inherit;

    public static CellNetworkPolicy Default { get; } = new();

    public static CellNetworkPolicy Offline { get; } = new()
    {
        Access = CellNetworkAccess.Disabled
    };
}
