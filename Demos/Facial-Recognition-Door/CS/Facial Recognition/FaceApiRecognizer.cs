using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using System.Diagnostics;
using Windows.Storage;
using Microsoft.ProjectOxford.Face.Contract;

namespace FacialRecognitionDoor.FacialRecognition
{
    class FaceApiRecognizer : IFaceRecognizer
    {
        #region Private members
        private static readonly Lazy<FaceApiRecognizer> _recognizer = new Lazy<FaceApiRecognizer>( () => new FaceApiRecognizer());

        private FaceApiWhitelist _whitelist = null;
        private IFaceServiceClient _faceApiClient = null;
        private StorageFolder _whitelistFolder = null;
        #endregion

        #region Properties
        /// <summary>
        /// Face API Recognizer instance
        /// </summary>
        public static FaceApiRecognizer Instance
        {
            get
            {
                return _recognizer.Value;
            }
        }

        /// <summary>
        /// Whitelist Id on Cloud Face API
        /// </summary>
        public string WhitelistId
        {
            get;
            private set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// Initial Face Api client
        /// </summary>
        private FaceApiRecognizer() {
            _faceApiClient = new FaceServiceClient(GeneralConstants.OxfordAPIKey, GeneralConstants.FaceAPIEndpoint);
        }
        #endregion

        #region Whitelist

        private void UpdateProgress(IProgress<int> progress, double progressCnt)
        {
            if(progress != null)
            {
                progress.Report((int)Math.Round(progressCnt));
            }
        }

        /// <summary>
        /// Train whitelist until training finished
        /// </summary>
        /// <returns></returns>
        private async Task<bool> TrainingWhitelistAsync()
        {
            bool isSuccess = true;

            // Train whitelist after add all person
            Debug.WriteLine("Start training whitelist: " + WhitelistId);
            await _faceApiClient.TrainPersonGroupAsync(WhitelistId);

            TrainingStatus status;
            

            while(true)
            {
                status = await _faceApiClient.GetPersonGroupTrainingStatusAsync(WhitelistId);

                // wait for training to complete
                if (status.Status != Status.Running)
                {
                    Debug.WriteLine("GetPersonGroupTrainingStatusAsync stopped running:" + status.Message);
                    if (status.Status == Status.Failed)
                    {
                        isSuccess = false;
                    }
                    break;
                }

                await Task.Delay(1000);
                Debug.WriteLine("GetPersonGroupTrainingStatusAsync still running:" + status.Message);
            }

            Debug.WriteLine("GetPersonGroupTrainingStatusAsync end result: " + isSuccess);
            return isSuccess;
        }

        public async Task<bool> CreateWhitelistFromFolderAsync(string whitelistId, StorageFolder whitelistFolder = null, IProgress<int> progress = null)
        {
            bool isSuccess = true;
            double progressCnt = 0;

            WhitelistId = whitelistId;
            _whitelist = new FaceApiWhitelist(WhitelistId);

            try
            {
                // whitelist folder default to picture library
                if (whitelistFolder == null)
                {
                    whitelistFolder = await KnownFolders.PicturesLibrary.GetFolderAsync("WhiteList");
                }

                _whitelistFolder = whitelistFolder;

                // detele person group if already exists
                try
                {
                    // An exception throwed if the person group doesn't exist
                    await _faceApiClient.GetPersonGroupAsync(whitelistId);
                    UpdateProgress(progress, ++progressCnt);

                    await _faceApiClient.DeletePersonGroupAsync(whitelistId);
                    UpdateProgress(progress, ++progressCnt);

                    Debug.WriteLine("Deleted old group");
                }
                catch (FaceAPIException fe)
                {
                    if (fe.ErrorCode == "PersonGroupNotFound")
                    {
                        Debug.WriteLine("The person group doesn't exist");
                    }
                    else
                    {
                        throw fe;
                    }

                }

                await _faceApiClient.CreatePersonGroupAsync(WhitelistId, "White List");
                UpdateProgress(progress, ++progressCnt);

                await BuildWhiteListAsync(progress, progressCnt);
            }
            
            catch(FaceAPIException fe)
            {
                isSuccess = false;
                Debug.WriteLine("FaceAPIException in CreateWhitelistFromFolderAsync : " + fe.ErrorMessage);
            }
            catch(Exception e)
            {
                isSuccess = false;
                Debug.WriteLine("Exception in CreateWhitelistFromFolderAsync : " + e.Message);
            }

            // progress to 100%
            UpdateProgress(progress, 100);

            return isSuccess;
        }

        /// <summary>
        /// Use whitelist folder to build whitelist Database
        /// </summary>
        /// <returns></returns>
        private async Task BuildWhiteListAsync(IProgress<int> progress, double progressCnt)
        {
            Debug.WriteLine("Start building whitelist from " + _whitelistFolder.Path);

            // calc progress step
            var fileCnt = await FaceApiUtils.GetFileCountInWhitelist(_whitelistFolder);
            var progressStep = (100.0 - progressCnt) / fileCnt;

            var subFolders = await _whitelistFolder.GetFoldersAsync();
            // Iterate all subfolders in whitelist
            foreach(var folder in subFolders)
            {
                var personName = folder.Name;

                // create new person
                var personId = await CreatePerson(personName, folder);

                // get all images in the folder
                var files = await folder.GetFilesAsync();

                // iterate all images and add to whitelist
                foreach(var file in files)
                {
                    Debug.WriteLine("BuildWhiteList: Processing " + file.Path);
                    try
                    {
                        
                        var faceId = await DetectFaceFromImage(file);
                        await AddFace(personId, faceId, file.Path);

                        Debug.WriteLine("This image added to whitelist successfully!");
                    }
                    catch(FaceRecognitionException fe)
                    {
                        switch(fe.ExceptionType)
                        {
                            case FaceRecognitionExceptionType.InvalidImage:
                                Debug.WriteLine("WARNING: This file is not a valid image!");
                                break;
                            case FaceRecognitionExceptionType.NoFaceDetected:
                                Debug.WriteLine("WARNING: No face detected in this image");
                                break;
                            case FaceRecognitionExceptionType.MultipleFacesDetected:
                                Debug.WriteLine("WARNING: Multiple faces detected, ignored this image");
                                break;
                        }
                    }

                    // update progress
                    progressCnt += progressStep;
                    UpdateProgress(progress, progressCnt);
                }
            }

            await TrainingWhitelistAsync();

            Debug.WriteLine("Whitelist created successfully!");
        }
        #endregion

        #region Face
        /// <summary>
        /// Add face to both Cloud Face API and local whitelist
        /// </summary>
        /// <param name="personId"></param>
        /// <param name="faceId"></param>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private async Task AddFace(Guid personId, Guid faceId, string imagePath)
        {

            // prevent running synchronous call on UI thread
            await Task.Run(async() =>
            {
                using (Stream imageStream = File.OpenRead(imagePath))
                {
                    AddPersistedFaceResult result = await _faceApiClient.AddPersonFaceAsync(WhitelistId, personId, imageStream);
                }
                _whitelist.AddFace(personId, faceId, imagePath);
            });


        }

        /// <summary>
        /// Remove face from both Cloud Face API and local whitelist
        /// </summary>
        /// <param name="personId"></param>
        /// <param name="faceId"></param>
        /// <returns></returns>
        private async Task RemoveFace(Guid personId, Guid faceId)
        {
            await _faceApiClient.DeletePersonFaceAsync(WhitelistId, personId, faceId);
            _whitelist.RemoveFace(personId, faceId);
        }

        /// <summary>
        /// Detect face and return the face id of a image file
        /// </summary>
        /// <param name="imageFile">
        /// image file to detect face
        /// Note: the image must only contains exactly one face
        /// </param>
        /// <returns>face id</returns>
        private async Task<Guid> DetectFaceFromImage(StorageFile imageFile)
        {
            var stream = await imageFile.OpenStreamForReadAsync();
            var faces = await _faceApiClient.DetectAsync(stream);
            if(faces == null || faces.Length < 1)
            {
                throw new FaceRecognitionException(FaceRecognitionExceptionType.NoFaceDetected);
            }
            else if(faces.Length > 1)
            {
                throw new FaceRecognitionException(FaceRecognitionExceptionType.MultipleFacesDetected);
            }

            return faces[0].FaceId;
        }

        /// <summary>
        /// Detect face and return the face id of a image file
        /// </summary>
        /// <param name="imageFile">
        /// image file to detect face
        /// </param>
        /// <returns>face id</returns>
        private async Task<Guid[]> DetectFacesFromImage(StorageFile imageFile)
        {
            var stream = await imageFile.OpenStreamForReadAsync();
            var faces = await _faceApiClient.DetectAsync(stream);
            if (faces == null || faces.Length < 1)
            {
                throw new FaceRecognitionException(FaceRecognitionExceptionType.NoFaceDetected);
            }

            return FaceApiUtils.FacesToFaceIds(faces) ;
        }

        public async Task<bool> AddImageToWhitelistAsync(StorageFile imageFile, string personName = null)
        {
            bool isSuccess = true;

            // imageFile should be valid image file
            if (!FaceApiUtils.ValidateImageFile(imageFile))
            {
                isSuccess = false;
            }
            else
            {
                var filePath = imageFile.Path;

                // If personName is null/empty, use the folder name as person name
                if(string.IsNullOrEmpty(personName))
                {
                    personName = await FaceApiUtils.GetParentFolderNameAsync(imageFile);
                }

                // If person name doesn't exists, add it
                var personId = _whitelist.GetPersonIdByName(personName);
                if(personId == Guid.Empty)
                {
                    var folder = await imageFile.GetParentAsync();
                    personId = await CreatePerson(personName, folder);
                }

                // detect faces
                var faceId = await DetectFaceFromImage(imageFile);
                await AddFace(personId, faceId, imageFile.Path);

                // train whitelist
                isSuccess = await TrainingWhitelistAsync();
            }

            return isSuccess;
        }

        public async Task<bool> RemoveImageFromWhitelistAsync(StorageFile imageFile, string personName = null)
        {
            bool isSuccess = true;
            if (!FaceApiUtils.ValidateImageFile(imageFile))
            {
                isSuccess = false;
            }
            else
            {
                // If personName is null use the folder name as person name
                if(string.IsNullOrEmpty(personName))
                {
                    personName = await FaceApiUtils.GetParentFolderNameAsync(imageFile);
                }

                var personId = _whitelist.GetPersonIdByName(personName);
                var faceId = _whitelist.GetFaceIdByFilePath(imageFile.Path);
                if(personId == Guid.Empty || faceId == Guid.Empty)
                {
                    isSuccess = false;
                }
                else
                {
                    await RemoveFace(personId, faceId);

                    // train whitelist
                    isSuccess = await TrainingWhitelistAsync();
                }
            }
            return isSuccess;
        }
        #endregion

        #region Person
        /// <summary>
        /// Create a person into Face API and whitelist
        /// </summary>
        /// <param name="personName"></param>
        /// <param name="personFolder"></param>
        /// <returns></returns>
        private async Task<Guid> CreatePerson(string personName, StorageFolder personFolder)
        {
            var ret = await _faceApiClient.CreatePersonAsync(WhitelistId, personName);
            var personId = ret.PersonId;

            _whitelist.AddPerson(personId, personName, personFolder.Path);

            return personId;
        }

        private async Task RemovePerson(Guid personId)
        {
            await _faceApiClient.DeletePersonAsync(WhitelistId, personId);
            _whitelist.RemovePerson(personId);
        }

        public async Task<bool> AddPersonToWhitelistAsync(StorageFolder faceImagesFolder, string personName = null)
        {
            bool isSuccess = true;

            if(faceImagesFolder == null)
            {
                isSuccess = false;
            }
            else
            {
                // use folder name if do not have personName
                if(string.IsNullOrEmpty(personName))
                {
                    personName = faceImagesFolder.Name;
                }

                var personId = await CreatePerson(personName, faceImagesFolder);
                var files = await faceImagesFolder.GetFilesAsync();

                // iterate all files and add to whitelist
                foreach(var file in files)
                {
                    try
                    {
                        // detect faces
                        var faceId = await DetectFaceFromImage(file);
                        await AddFace(personId, faceId, file.Path);
                    }
                    catch(FaceRecognitionException fe)
                    {
                        switch (fe.ExceptionType)
                        {
                            case FaceRecognitionExceptionType.InvalidImage:
                                Debug.WriteLine("WARNING: This file is not a valid image!");
                                break;
                            case FaceRecognitionExceptionType.NoFaceDetected:
                                Debug.WriteLine("WARNING: No face detected in this image");
                                break;
                            case FaceRecognitionExceptionType.MultipleFacesDetected:
                                Debug.WriteLine("WARNING: Multiple faces detected, ignored this image");
                                break;
                        }
                    }
                }

                // train whitelist
                isSuccess = await TrainingWhitelistAsync();
            }

            return isSuccess;                
        }

        public async Task<bool> RemovePersonFromWhitelistAsync(string personName)
        {
            bool isSuccess = true;

            var personId = _whitelist.GetPersonIdByName(personName);
            if(personId == Guid.Empty)
            {
                isSuccess = false;
            }
            else
            {
                // remove all faces belongs to this person
                var faceIds = _whitelist.GetAllFaceIdsByPersonId(personId);
                if(faceIds != null)
                {
                    var faceIdsArr = faceIds.ToArray();
                    for (int i = 0; i < faceIdsArr.Length; i++)
                    {
                        await RemoveFace(personId, faceIdsArr[i]);
                    }
                }

                // remove person
                await RemovePerson(personId);

                // train whitelist
                isSuccess = await TrainingWhitelistAsync();
            }

            return isSuccess;
        }
        #endregion

        #region Face recognition
        public async Task<List<string>> FaceRecognizeAsync(StorageFile imageFile)
        {
            var recogResult = new List<string>();

            if(!FaceApiUtils.ValidateImageFile(imageFile))
            {
                throw new FaceRecognitionException(FaceRecognitionExceptionType.InvalidImage);
            }

            // detect all faces in the image
            var faceIds = await DetectFacesFromImage(imageFile);

            // try to identify all faces to person
            var identificationResults = await _faceApiClient.IdentifyAsync(WhitelistId, faceIds);

            // add identified person name to result list
            foreach(var result in identificationResults)
            {
                if(result.Candidates.Length > 0)
                {
                    var personName = _whitelist.GetPersonNameById(result.Candidates[0].PersonId);
                    Debug.WriteLine("Face ID Confidence: " + Math.Round(result.Candidates[0].Confidence * 100, 1) + "%");
                    recogResult.Add(personName);
                }
            }

            return recogResult;
        }
        #endregion
    }
}
