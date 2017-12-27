using System;

namespace FacialRecognitionDoor.FacialRecognition
{
    enum FaceRecognitionExceptionType
    {
        InvalidFilePath = 0,        // The file path is invalid
        InvalidImage,               // The image file is invalid(e.g. Unsupported file type, file size is too small or too large)
        NoFaceDetected,             // No face detected in this image
        MultipleFacesDetected,      // More than one faces detected in this image, not sure which to recognize
        OtherExceptions             // Other exceptions for extensible
    }

    class FaceRecognitionException : Exception
    {
        // Exception type of the exception instance
        public FaceRecognitionExceptionType ExceptionType { get; private set; }

        public FaceRecognitionException(string message) : base(message)
        {
            ExceptionType = FaceRecognitionExceptionType.OtherExceptions;
        }

        public FaceRecognitionException(FaceRecognitionExceptionType exceptionType)
        {
            ExceptionType = exceptionType;
        }
    }
}
