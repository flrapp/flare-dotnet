namespace Flare.Extensions.Configuration.Models;

public class FlagEvaluationResponseModel
{
    public string FlagKey { get; set; } = null!;
    public bool Value { get; set; }
}