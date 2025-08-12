using Microsoft.ML.Data;

namespace FileCategorization_Api.Domain.Entities.MachineLearning;

public class MlFileNamePrediction
{
    [ColumnName("PredictedLabel")]
    public string Area;

    [ColumnName("Score")]
    public float[] Score { get; set; }
}