using Microsoft.ML.Data;

namespace FileCategorization_Api.Models.MachineLearning;

public class MlFileName
{
    [LoadColumn(0)]
    public int Id { get; set; }

    [LoadColumn(1)]
    public string Area { get; set; }

    [LoadColumn(2)]
    public string FileName { get; set; }
}