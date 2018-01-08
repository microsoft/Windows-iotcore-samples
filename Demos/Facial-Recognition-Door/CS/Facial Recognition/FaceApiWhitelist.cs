using System;
using System.Collections.Generic;

namespace FacialRecognitionDoor.FacialRecognition
{
    /// <summary>
    /// Data structure to store and manage Whitelist
    /// Including mapping between Face API IDs and local storage face images
    /// </summary>
    class FaceApiWhitelist
    {

        #region Private members
        private string _whitelistId;

        // person dictionary
        private Dictionary<Guid, HSPerson> _personIdToPerson;
        private Dictionary<string, HSPerson> _personNameToPerson;

        // face dictionary
        private Dictionary<Guid, HSFace> _faceIdToFace;
        private Dictionary<string, HSFace> _faceNameToFace;

        // person-face dictionary
        private Dictionary<Guid, List<Guid>> _personFacesDict;
        #endregion

        #region Properties
        public string WhitelistId
        {
            get
            {
                return _whitelistId;
            }
        }
        #endregion

        #region Constructors
        public FaceApiWhitelist(string whitelistId)
        {
            _whitelistId = whitelistId;

            _faceIdToFace = new Dictionary<Guid, HSFace>();
            _faceNameToFace = new Dictionary<string, HSFace>();

            _personIdToPerson = new Dictionary<Guid, HSPerson>();
            _personNameToPerson = new Dictionary<string, HSPerson>();

            _personFacesDict = new Dictionary<Guid, List<Guid>>();
        }
        #endregion

        #region Management person methods
        public void AddPerson(Guid personId, string personName, string imageFolder)
        {
            // return if invalid parameters or person exists
            if (personId == Guid.Empty                  ||
                string.IsNullOrEmpty(personName)        ||
                string.IsNullOrEmpty(imageFolder)       ||
                _personIdToPerson.ContainsKey(personId) ||
                _personNameToPerson.ContainsKey(personName))
                return;

            // create person and add to dictionaries
            var person = new HSPerson(personId, personName, imageFolder);

            _personIdToPerson.Add(personId, person);
            _personNameToPerson.Add(personName, person);
            _personFacesDict.Add(personId, new List<Guid>());
        }

        private void RemovePerson(HSPerson person)
        { 
            // remove all person info
            _personIdToPerson.Remove(person.Id);
            _personNameToPerson.Remove(person.Name);
            _personFacesDict.Remove(person.Id);
        }

        public void RemovePerson(Guid personId)
        {
            // return if invalid parameters or person Id not found
            if (personId == Guid.Empty ||
                !_personIdToPerson.ContainsKey(personId))
                return;

            // find person name then remove
            var person = _personIdToPerson[personId];
            RemovePerson(person);
        }

        public void RemovePerson(string personName)
        {
            // return if invalid parameters or person Id not found
            if (string.IsNullOrEmpty(personName) ||
                !_personNameToPerson.ContainsKey(personName))
                return;

            // get person Id then remove
            var person = _personNameToPerson[personName];
            RemovePerson(person);
        }

        #endregion

        #region Query person methods
        public string GetPersonNameById(Guid personId)
        {
            if (personId == null ||
                !_personIdToPerson.ContainsKey(personId))
                return null;

            return _personIdToPerson[personId].Name;
        }

        public Guid GetPersonIdByName(string personName)
        {
            if (string.IsNullOrEmpty(personName) ||
                !_personNameToPerson.ContainsKey(personName))
                return Guid.Empty;

            return _personNameToPerson[personName].Id;
        }

        public List<Guid> GetAllFaceIdsByPersonId(Guid personId)
        {
            if(!_personFacesDict.ContainsKey(personId))
            {
                return null;
            }

            return _personFacesDict[personId];
        }

        #endregion

        #region Management face methods
        private void AddFace(HSPerson person, Guid faceId, string faceImageFile)
        {
            // create new face and add to dictionaries
            var face = new HSFace(faceId, faceImageFile);

            _faceIdToFace.Add(faceId, face);
            _faceNameToFace.Add(faceImageFile, face);

            // add face id to face list of the person
            var faceList = _personFacesDict[person.Id];
            faceList.Add(faceId);
        }

        public void AddFace(Guid personId, Guid faceId, string faceImageFile)
        {
            // return if invalid parameters or face exists
            if (faceId == Guid.Empty                     ||
                personId == Guid.Empty                   ||
                string.IsNullOrEmpty(faceImageFile)      ||
                !_personIdToPerson.ContainsKey(personId) ||
                _faceIdToFace.ContainsKey(faceId)        ||
                _faceNameToFace.ContainsKey(faceImageFile))
                return;

            var person = _personIdToPerson[personId];
            AddFace(person, faceId, faceImageFile);
        }

        public void AddFace(string personName, Guid faceId, string faceImageFile)
        {
            // return if invalid parameters or face exists
            if (faceId == Guid.Empty                         ||
                string.IsNullOrEmpty(faceImageFile)          ||
                string.IsNullOrEmpty(personName)             ||
                !_personNameToPerson.ContainsKey(personName) ||
                _faceIdToFace.ContainsKey(faceId)            ||
                _faceNameToFace.ContainsKey(faceImageFile))
                return;

            var person = _personNameToPerson[personName];
            AddFace(person, faceId, faceImageFile);
        }

        private void RemoveFace(HSPerson person, HSFace face)
        {
            _faceIdToFace.Remove(face.Id);
            _faceNameToFace.Remove(face.ImageFile);

            _personFacesDict[person.Id].Remove(face.Id);
        }

        public void RemoveFace(Guid personId, Guid faceId)
        {
            // return if person/face not exists
            if (personId == Guid.Empty                   ||
                faceId == Guid.Empty                     ||
                !_personIdToPerson.ContainsKey(personId) ||
                !_faceIdToFace.ContainsKey(faceId))
                return;

            var person = _personIdToPerson[personId];
            var face = _faceIdToFace[faceId];
            RemoveFace(person, face);
        }

        public void RemoveFace(string personName, string faceName)
        {
            // return if person/face not exists
            if (string.IsNullOrEmpty(personName)             ||
                string.IsNullOrEmpty(faceName)               ||
                !_personNameToPerson.ContainsKey(personName) ||
                !_faceNameToFace.ContainsKey(faceName))
                return;

            var person = _personNameToPerson[personName];
            var face = _faceNameToFace[faceName];
            RemoveFace(person, face);
        }

        #endregion

        #region Query face methods
        public Guid GetFaceIdByFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) ||
                !_faceNameToFace.ContainsKey(filePath))
                return Guid.Empty;

            return _faceNameToFace[filePath].Id;
        }

        public string GetFaceFilePathById(Guid faceId)
        {
            if (faceId == Guid.Empty ||
                !_faceIdToFace.ContainsKey(faceId))
                return null;

            return _faceIdToFace[faceId].ImageFile;
        }
        #endregion
    }
}
