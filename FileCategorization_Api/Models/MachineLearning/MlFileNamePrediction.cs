using Microsoft.ML.Data;

namespace FileCategorization_Api.Models.MachineLearning;

public class MlFileNamePrediction
{
    [ColumnName("PredictedLabel")]
    public string Area;

    [ColumnName("Score")]
    public float[] Score { get; set; }
}