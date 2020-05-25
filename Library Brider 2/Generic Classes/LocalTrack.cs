using System.Xml.Serialization;
using System.IO;

namespace Library_Brider_2.Generic_Classes
{
    public enum TagState { MISSING_TITLE, MISSING_TAG, FULL_TAGS }

    [XmlRootAttribute("LocalTrack", Namespace = "Library_Brider_2", IsNullable = false)]
    public class LocalTrack
    {
        public string LocalPath { get; set; }

        public string FileName => Filter.CleanStringForSearch(Path.GetFileNameWithoutExtension(LocalPath));

        public string Author { get; set; }

        public string Title { get; set; }

        public TagState TagState { get; set; }

        public string SpotifyUri { get; set; }

        public string Error { get; set; }
    }
}