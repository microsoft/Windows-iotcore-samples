using System;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.ProjectOxford.Face.Contract;

namespace FacialRecognitionDoor.FacialRecognition
{
    /// <summary>
    /// Contains utilities used to interact with the Oxford face API
    /// </summary>
    class FaceApiUtils
    {
        /// <summary>
        /// Accepts a StorageFile and returns true if the StorageFile is a valid Image File. 
        /// This is done by testing the file type for .jpg, .png, .gif, and .bmp
        /// </summary>
        public static bool ValidateImageFile(StorageFile imageFile)
        {
            return  imageFile != null       &&
                    imageFile.IsAvailable   &&
                    (imageFile.FileType.ToUpper() == ".JPG" ||
                     imageFile.FileType.ToUpper() == ".PNG" ||
                     imageFile.FileType.ToUpper() == ".GIF" ||
                     imageFile.FileType.ToUpper() == ".BMP");
        }

        /// <summary>
        /// Accepts a StorageFile and returns the name of the folder the file is stored within
        /// </summary>
        public static async Task<string> GetParentFolderNameAsync(StorageFile imageFile)
        {
            var parentFolder = await imageFile.GetParentAsync();
            return parentFolder.Name;
        }

        /// <summary>
        /// Accepts an array Face objects and returns an array of GUIDs assocaited with the Face objects
        /// </summary>
        public static Guid[] FacesToFaceIds(Face[] faces)
        {
            var faceIds = new Guid[faces.Length];

            for(var i = 0; i < faces.Length; i++)
            {
                faceIds[i] = faces[i].FaceId;
            }
            return faceIds;
        }

        /// <summary>
        /// Returns the number of files within the Oxford Whitelist
        /// </summary>
        public static async Task<int> GetFileCountInWhitelist(StorageFolder folder)
        {
            int fileCount = 0;

            fileCount += (await folder.GetFilesAsync()).Count;

            var subFolders = await folder.GetFoldersAsync();

            foreach(var subFolder in subFolders)
            {
                fileCount += (await subFolder.GetFilesAsync()).Count;
            }

            return fileCount;
        }
    }
}
