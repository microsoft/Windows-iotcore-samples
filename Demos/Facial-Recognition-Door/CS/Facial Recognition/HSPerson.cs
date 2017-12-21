using System;

namespace FacialRecognitionDoor.FacialRecognition
{
    class HSPerson
    {
        /// <summary>
        /// Person Id for face API
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the person
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The image folder
        /// </summary>
        public string Folder { get; set; }

        public HSPerson() { }

        public HSPerson(Guid id, string name, string folder)
        {
            Id      = id;
            Name    = name;
            Folder  = folder;
        }
    }
}
