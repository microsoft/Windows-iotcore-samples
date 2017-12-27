using FacialRecognitionDoor.Helpers;

namespace FacialRecognitionDoor.Objects
{
    /// <summary>
    /// Object specifically to be passed to UserProfilePage that contains an instance of the WebcamHelper and a Visitor object
    /// </summary>
    class UserProfileObject
    {
        /// <summary>
        /// An initialized Visitor object
        /// </summary>
        public Visitor Visitor { get; set; }

        /// <summary>
        /// An initialized WebcamHelper 
        /// </summary>
        public WebcamHelper WebcamHelper { get; set; }

        /// <summary>
        /// Initializes a new UserProfileObject with relevant information
        /// </summary>
        public UserProfileObject(Visitor visitor, WebcamHelper webcamHelper)
        {
            Visitor = visitor;
            WebcamHelper = webcamHelper;
        }
    }
}
