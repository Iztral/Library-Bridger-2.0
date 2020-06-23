using AcoustID;
using AcoustID.Web;
using Library_Brider_2.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Library_Brider_2.Fingerprinting
{
    public static class Fingerprint
    {
        private static string Generate(string file)
        {
            NAudioDecoder decoder = new NAudioDecoder(file);
            ChromaContext context = new ChromaContext();

            context.Start(decoder.SampleRate, decoder.Channels);
            decoder.Decode(context, 1000);
            context.Finish();

            return context.GetFingerprint();
        }

        public static LocalTrack LookUp(LocalTrack localTrack)
        {
            if (string.IsNullOrEmpty(Configuration.ClientKey))
            {
                Configuration.ClientKey = Properties.Settings.Default.AcoustID;
            }
            LocalTrack track = new LocalTrack();
            int duration = 0;
            using (TagLib.File file = TagLib.File.Create(localTrack.LocalPath))
            {
                duration = (int)file.Properties.Duration.TotalSeconds;
            }
            TaskScheduler context = TaskScheduler.Default;
            Task<LookupResponse> task = new LookupService().GetAsync(Generate(localTrack.LocalPath), duration,
                new string[] { "recordings", "compress" });
            task.Wait();
            Task successContinuation = task.ContinueWith(t =>
            {
                foreach (var e in t.Exception.InnerExceptions)
                {
                    track.FingerprintStatus = FingerprintStatus.WEBSERVICE_ERROR;
                }
            },
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted |
            TaskContinuationOptions.ExecuteSynchronously, context);

            Task failureContinuation = task.ContinueWith(t =>
            {
                track.FingerprintStatus = FingerprintStatus.UNEXPECTED_ERROR;
                var response = t.Result;

                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    track.FingerprintStatus = FingerprintStatus.WEBSERVICE_ERROR;
                    return;
                }

                if (response.Results.Count == 0)
                {
                    track.FingerprintStatus = FingerprintStatus.NO_RESULT;
                    return;
                }

                foreach (var result in response.Results)
                {
                    if (result.Recordings.Count != 0)
                    {
                        track.FingerprintStatus = FingerprintStatus.GOT_RESULT;
                        track.Author = result.Recordings[0].Artists[0].Name;
                        track.Title = result.Recordings[0].Title;
                        track.SearchType = LocalSearchType.FULL_TAGS;
                    }
                }
            },
            CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion |
            TaskContinuationOptions.ExecuteSynchronously, context);
            Task.WaitAny(successContinuation, failureContinuation);

            return track;
        }

        /*public static void Submit(LocalTrack localTrack)
        {
            using (TagLib.File file = TagLib.File.Create(localTrack.LocalPath))
            {
                using (Task<SubmitResponse> task = new SubmitService(
                    userKey: Properties.Settings.Default.AcoustID).SubmitAsync(
                    request: new SubmitRequest(
                        fingerprint: Generate(localTrack.LocalPath), 
                        duration: (int)file.Properties.Duration.TotalSeconds)
                    {
                        Album = file.Tag.Album,
                        Artist = file.Tag.FirstPerformer,
                        Title = file.Tag.Title,
                        AlbumArtist = file.Tag.FirstAlbumArtist
                    }))
                {
                    task.Start();
                }
            }
        }*/
    }
}
