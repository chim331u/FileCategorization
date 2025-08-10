using FileCategorization_Api.Models.FileCategorization;


namespace FileCategorization_Api.Interfaces;

public interface IMachineLearningService
{
    string PredictFileCategorization(string fileNameToPredict);
    List<FilesDetail> PredictFileCategorization(List<FilesDetail> fileList);
    string TrainAndSaveModel();
}