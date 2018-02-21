using Amazon.Polly;
using DotNetToolkit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voicecoin.AiBot;

namespace Voicecoin.RestApi
{
    public class NativeVoiceController : CoreController
    {
        private IConfiguration config;
        private IHostingEnvironment env;
        private string dialogApiKey;
        private string awsAccessKey;
        private string awsSecretKey;

        public NativeVoiceController(IConfiguration Configuration, IHostingEnvironment hostingEnvironment)
        {
            config = Configuration;
            env = hostingEnvironment;
            dialogApiKey = config.GetSection("dialogflow:apiKey").Value;
            awsAccessKey = config.GetSection("Aws:AWSAccessKey").Value;
            awsSecretKey = config.GetSection("Aws:AWSSecretKey").Value;
        }

        public async Task StartVoicehub(VoiceCapturedModel record)
        {
            ConsoleLogger.WriteLine("System", $"{record.SpeechResult} [Confidence: {record.Confidence}]");
            var aIResponse = new IntentClassifer(dialogApiKey).TextRequest(record.CallSid, record.SpeechResult);

            VoiceId voiceId = VoiceId.Joanna;
            var polly = new PollyUtter(awsAccessKey, awsSecretKey);
            string filePath = await polly.Utter(aIResponse.Result.Fulfillment.Speech, env.WebRootPath, voiceId);
            polly.Play(filePath);

            // Start listening
            if(aIResponse.Result.Metadata.IntentName == "TriggerDialog")
            {
                var gs = new GoogleSpeech();
                var transcript = await gs.StreamingRecognize();
                await ActionCallback(new VoiceCapturedModel
                {
                    CallSid = record.CallSid,
                    SpeechResult = transcript
                });
            }
        }

        public async Task ActionCallback(VoiceCapturedModel record)
        {
            ConsoleLogger.WriteLine("System", $"{record.SpeechResult} [Confidence: {record.Confidence}]");

            var aIResponse = new IntentClassifer(dialogApiKey).TextRequest(record.CallSid, record.SpeechResult);

            VoiceId voiceId = VoiceId.Joanna;
            var polly = new PollyUtter(awsAccessKey, awsSecretKey);

            string filePath = String.Empty;

            if (aIResponse.Result.Metadata.IntentName == "Transfer2SalesBot - yes")
            {
                filePath = await polly.Utter("Great, It's connected to for you.", env.WebRootPath, voiceId);
                polly.Play(filePath);
            }

            if (aIResponse.Result.Metadata.IntentName == "AlphaGo")
            {
                filePath = await polly.Utter("OK, it's connected for you.", env.WebRootPath, voiceId);
                polly.Play(filePath);
            }

            if (aIResponse.Result.Parameters.ContainsKey("VoiceId")
                && !String.IsNullOrEmpty(aIResponse.Result.Parameters["VoiceId"].ToString()))
            {
                voiceId = VoiceId.FindValue(aIResponse.Result.Parameters["VoiceId"].ToString());
            }

            filePath = await polly.Utter(aIResponse.Result.Fulfillment.Speech, env.WebRootPath, voiceId);
            polly.Play(filePath);

            var gs = new GoogleSpeech();
            var transcript = await gs.StreamingRecognize();
            await ActionCallback(new VoiceCapturedModel
            {
                CallSid = record.CallSid,
                SpeechResult = transcript
            });

            string host = $"{Request.Scheme}://{Request.Host}";
        }
    }
}
