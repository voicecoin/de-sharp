using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Voicecoin.RestApi
{
    public class GoogleSpeech
    {
        public async Task<string> Recognize(string urlPath)
        {
            StringBuilder sb = new StringBuilder();
            GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

            var speech = SpeechClient.Create();

            var response = speech.Recognize(new RecognitionConfig()
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                //SampleRateHertz = 16000,
                LanguageCode = "en",
            }, RecognitionAudio.FromFile(urlPath));

            foreach (var result in response.Results)
            {
                foreach (var alternative in result.Alternatives)
                {
                    Console.WriteLine("**" + alternative.Transcript + "**");
                    sb.Append(alternative.Transcript);
                }
            }

            return sb.ToString();
        }
    }
}
