using System;
using System.IO;
using System.Xml.Serialization;

namespace Library_Brider_2.Generic
{
    public enum LocalSearchType { AUDIO_SEARCH, FILENAME_ONLY, FULL_TAGS }
    public enum FingerprintStatus {NONE, GOT_RESULT, NO_RESULT, WEBSERVICE_ERROR, UNEXPECTED_ERROR}

    [XmlRootAttribute("LocalTrack", Namespace = "Library_Brider_2", IsNullable = false)]
    public class LocalTrack
    {
        public string LocalPath { get; set; }

        public string FileName => Filter.CleanStringForSearch(Path.GetFileNameWithoutExtension(LocalPath));

        public string Author { get; set; }

        public string Title { get; set; }

        public DateTime CreationDate => new FileInfo(LocalPath).CreationTime;

        public DateTime ModificationDate => new FileInfo(LocalPath).LastWriteTime;

        public LocalSearchType SearchType { get; set; }

        public string SpotifyUri { get; set; }

        public FingerprintStatus FingerprintStatus { get; set; }

        public LocalTrack(string filePath)
        {
            LocalPath = filePath;
            SetTagsFromFile(filePath);
            DetermineSearchType();
        }

        public LocalTrack()
        {

        }

        private void SetTagsFromFile(string filePath)
        {
            try
            {
                TagLib.File file_tags = TagLib.File.Create(filePath);
                Author = Filter.CleanStringForSearch(file_tags.Tag.FirstPerformer);
                Title = Filter.CleanStringForSearch(file_tags.Tag.Title);
                file_tags.Dispose();
            }
            catch
            {
                Author = null;
                Title = null;
            }
        }

        private void DetermineSearchType()
        {
            if (Author != null && Title != null)
            {
                SearchType = LocalSearchType.FULL_TAGS;
            }
            else if (Author == null || Title == null)
            {
                SearchType = LocalSearchType.FILENAME_ONLY;
            }
        }

        public string FullTagTitle()
        {
            return Author + " - " + Title;
        }
    }
}