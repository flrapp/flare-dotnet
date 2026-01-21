using System.Collections.Generic;

namespace Flare.Extensions.Configuration.Models;

public class BulkEvaluationResponseModel
{
    public List<FlagEvaluationResponseModel>? Flags { get; set; }
}