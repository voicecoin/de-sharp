using Amazon.Polly;
using Google.Cloud.Translation.V2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Voicecoin.RestApi
{
    public class TranslationController : CoreController
    {
        private IConfiguration config;
        private IHostingEnvironment env;
        private string dialogApiKey;
        private string awsAccessKey;
        private string awsSecretKey;

        public TranslationController(IConfiguration Configuration, IHostingEnvironment hostingEnvironment)
        {
            config = Configuration;
            env = hostingEnvironment;
            dialogApiKey = config.GetSection("dialogflow:apiKey").Value;
            awsAccessKey = config.GetSection("Aws:AWSAccessKey").Value;
            awsSecretKey = config.GetSection("Aws:AWSSecretKey").Value;
        }

        public async Task Listen()
        {
            var gs = new GoogleSpeech();
            var transcript = await gs.MicStreamingRecognize("cmn");
            
            if(transcript.Length > 1)
            {
                TranslationClient client = TranslationClient.Create();
                var response = client.TranslateText(transcript, "en");
                Console.WriteLine(response.TranslatedText);

                var polly = new PollyUtter(awsAccessKey, awsSecretKey);
                string filePath = await polly.Utter(response.TranslatedText, env.WebRootPath, VoiceId.Matthew);
                polly.Play(filePath);
            }

            await Listen();
        }

    }
}
