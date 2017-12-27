using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace FacialRecognitionDoor.FacialRecognition
{
    interface IFaceRecognizer
    {
        #region Whitelist

        /// <summary>
        /// Build whitelist from the given path.
        /// The path should be the root path of whitelist, this path can contains
        /// up to 1000 subfolders, each subfolder is the face image library of a 
        /// person. The subfolder name should be the person name.
        /// Each person can have up to 32 face images in the folder.
        /// 
        /// WARNING: use this function will rebuild the whitelist database and all
        ///          data created before will be replaced!
        /// </summary>
        /// <param name="whitelistId">Id of whitelist, unique for each device</param>
        /// <param name="whitelistFolder">
        /// Folder path of the whitelist images
        /// Default to "Whitelist" folder under pictureLibrary
        /// </param>
        /// <param name="progress">Callback to report whitelist building progress</param>
        /// <returns>Is whitelist created successfully</returns>
        Task<bool> CreateWhitelistFromFolderAsync(string whitelistId, StorageFolder whitelistFolder = null, IProgress<int> progress = null);
        #endregion

        #region Add/Remove Face
        /// <summary>
        /// Add an image into whitelist
        /// </summary>
        /// <param name="personName">
        ///     Name of the person which the image belongs to.
        ///     If it's null or empty, the personName will default to the image's
        ///     parent folder's name.
        /// </param>
        /// <param name="imageFile">Image file contains face</param>
        /// <returns>Is successfully added image to whitelist</returns>
        Task<bool> AddImageToWhitelistAsync(StorageFile imageFile, string personName = null);

        /// <summary>
        /// Remove a image from whitelist
        /// </summary>
        /// <param name="personName">
        ///     Name of the person which the image belongs to.
        ///     If it's null or empty, the personName will default to the image's
        ///     parent folder's name.</param>
        /// <param name="imageFile">Image file contains face</param>
        /// <returns>Is successfully removed image from whitelist</returns>
        Task<bool> RemoveImageFromWhitelistAsync(StorageFile imageFile, string personName = null);
        #endregion

        #region Add/Remove Person
        /// <summary>
        /// Add a person into whitelist
        /// </summary>
        /// <param name="personName">The name of the person</param>
        /// <param name="faceImagesFolder">The folder of all images of the person</param>
        /// <returns>True if add person successfully.</returns>
        Task<bool> AddPersonToWhitelistAsync(StorageFolder faceImagesFolder, string personName = null);

        /// <summary>
        /// Remove a person from whitelist
        /// </summary>
        /// <param name="personName">Name of the person to be removed</param>
        /// <returns>True if remove person successfully</returns>
        Task<bool> RemovePersonFromWhitelistAsync(string personName);
        #endregion

        #region Face Recognition
        /// <summary>
        /// Method to perform face recoginition on an face image to identify whether
        /// the faces belongs to person in whitelist
        /// </summary>
        /// <param name="imageFile">
        ///     The image file contains face to be recognized
        /// </param>
        /// <returns>
        ///     The recognition results is the name of person in whitelist.
       ///      Empty if no face belongs to person in whitelist
        /// </returns>
        Task<List<string>> FaceRecognizeAsync(StorageFile imageFile);
        #endregion
    }
}
