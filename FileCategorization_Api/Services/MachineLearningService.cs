using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Models.FileCategorization;
using FileCategorization_Api.Models.MachineLearning;
using Microsoft.ML;

namespace FileCategorization_Api.Services
{
    public class MachineLearningService : IMachineLearningService
    {

        private ITransformer _trainedModel;
        private readonly ILogger<MachineLearningService> _logger;
        private readonly IConfigsService _configsService;

        public MachineLearningService(ILogger<MachineLearningService> logger, IConfigsService configsService)
        {
            _configsService = configsService;
            _logger = logger;
        }


        public string PredictFileCategorization(string fileNameToPredict)
        {
            var _modelPath = Path.Combine(_configsService.GetConfigValue("MODELPATH").Result, _configsService.GetConfigValue("MODELNAME").Result);
            MLContext _mlContext = new MLContext(seed: 0);

            //verifica presenza modello           
            //carica il modello
            CheckModelFolderAndFiles(_modelPath);

            ITransformer loadedModel = _mlContext.Model.Load(_modelPath, out var modelInputSchema);

            MlFileName trx = new MlFileName()
            {
                FileName = fileNameToPredict
            };

            PredictionEngine<MlFileName, MlFileNamePrediction> _predEngine = _mlContext.Model.CreatePredictionEngine<MlFileName, MlFileNamePrediction>(loadedModel);

            var prediction = _predEngine.Predict(trx);
            //_serviceData.WriteLog(LogType.Info, $"{fileNameToPredict} ===> Category Predicted: {prediction.Area} <=== ");

            return prediction.Area;
        }

        public List<FilesDetail> PredictFileCategorization(List<FilesDetail> fileList)
        {
            var _modelPath = Path.Combine(_configsService.GetConfigValue("MODELPATH").Result, _configsService.GetConfigValue("MODELNAME").Result);
            MLContext _mlContext = new MLContext(seed: 0);

            //verifica presenza modello           
            //carica il modello
            CheckModelFolderAndFiles(_modelPath);

            ITransformer loadedModel = _mlContext.Model.Load(_modelPath, out var modelInputSchema);

            // Create prediction engine only once and reuse it
            PredictionEngine<MlFileName, MlFileNamePrediction> _predEngine = _mlContext.Model.CreatePredictionEngine<MlFileName, MlFileNamePrediction>(loadedModel);

            foreach (var item in fileList)
            {
                MlFileName trx = new MlFileName()
                {
                    FileName = item.Name
                };

                var prediction = _predEngine.Predict(trx);
                //_serviceData.WriteLog(LogType.Info, $"{fileNameToPredict} ===> Category Predicted: {prediction.Area} <=== ");
                item.FileCategory = prediction.Area;
            }

            return fileList;

        }

        private void CheckModelFolderAndFiles(string modelPath)
        {

            if (!File.Exists(modelPath))
            {
                TrainAndSaveModel();
            }
        }
        public string TrainAndSaveModel()
        {
            string _trainDataPath = Path.Combine(_configsService.GetConfigValue("TRAINDATAPATH").Result, _configsService.GetConfigValue("TRAINDATANAME").Result);

            _logger.LogInformation("Start training and saving of model");
            MLContext _mlContext = new MLContext(seed: 0);

            _logger.LogInformation("Loading data from file ...");
            IDataView _trainingDataView = _mlContext.Data.LoadFromTextFile<MlFileName>(_trainDataPath, hasHeader: true, separatorChar: ';');

            var pipeline = ProcessData();

            var trainingPipeline = BuildAndTrainModel(_trainingDataView, pipeline);

            _logger.LogInformation("Saving Model");
            SaveModelAsFile(_mlContext, _trainingDataView.Schema, _trainedModel);

            _logger.LogInformation("Training and saving of model completed");

            return "Training and saving of model completed";

        }

        private IEstimator<ITransformer> ProcessData()
        {
            MLContext _mlContext = new MLContext(seed: 0);
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: "Area", outputColumnName: "Label")
                .Append(_mlContext.Transforms.Text.FeaturizeText(inputColumnName: "FileName", outputColumnName: "FileNameFeaturized"))
                .Append(_mlContext.Transforms.Concatenate("Features", "FileNameFeaturized"))
                .AppendCacheCheckpoint(_mlContext);

            return pipeline;
        }

        private IEstimator<ITransformer> BuildAndTrainModel(IDataView trainingDataView, IEstimator<ITransformer> pipeline)
        {
            MLContext _mlContext = new MLContext(seed: 0);

            var trainingPipeline = pipeline.Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _trainedModel = trainingPipeline.Fit(trainingDataView);

            PredictionEngine<MlFileName, MlFileNamePrediction> _predEngine = _mlContext.Model.CreatePredictionEngine<MlFileName, MlFileNamePrediction>(_trainedModel);

            return trainingPipeline;
        }

        private void SaveModelAsFile(MLContext mlContext, DataViewSchema trainingDataViewSchema, ITransformer model)
        {
            string _modelPath = Path.Combine(_configsService.GetConfigValue("MODELPATH").Result, _configsService.GetConfigValue("MODELNAME").Result);

            mlContext.Model.Save(model, trainingDataViewSchema, _modelPath);
        }

    }
}
