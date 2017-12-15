using System;

namespace FacialRecognitionDoor.FacialRecognition
{
    /// <summary>
    /// Face data structure
    /// </summary>
    class HSFace
    {
        /// <summary>
        /// Face id for Face API
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Image file name of the face belongs to
        /// </summary>
        public string ImageFile { get; set; }


        public HSFace() { }

        public HSFace(Guid id, string imageFile)
        {
            Id          = id;
            ImageFile   = imageFile;
        }
    }
}
