namespace AURA.Abstractions;

public sealed record CellProgramResult(bool IsSuccess, object? Data = null, string? Error = null)
{
    public static CellProgramResult Ok(object data) => new(true, data);
    public static CellProgramResult Fail(string error) => new(false, null, error);
}
