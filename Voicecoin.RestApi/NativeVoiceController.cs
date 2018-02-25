using Amazon.Polly;
using DotNetToolkit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public async Task Listening(VoiceCapturedModel record)
        {
            var gs = new GoogleSpeech();
            await gs.InitRecognitionConfig();
            var transcript = await gs.MicStreamingRecognize();
            await ActionCallback(new VoiceCapturedModel
            {
                CallSid = record.CallSid,
                SpeechResult = transcript
            });
        }

        public async Task StartVoicehub(VoiceCapturedModel record)
        {
            ConsoleLogger.WriteLine("Voice Browser", $"Ready to talk.");
            await ActionCallback(new VoiceCapturedModel
            {
                CallSid = record.CallSid,
                SpeechResult = record.SpeechResult
            });
        }

        public async Task ActionCallback(VoiceCapturedModel record)
        {
            var aIResponse = new IntentClassifer(dialogApiKey).TextRequest(record.CallSid, record.SpeechResult);

            VoiceId voiceId = VoiceId.Joanna;
            var polly = new PollyUtter(awsAccessKey, awsSecretKey);

            if (aIResponse.Result.Parameters.ContainsKey("VoiceId")
                && !String.IsNullOrEmpty(aIResponse.Result.Parameters["VoiceId"].ToString()))
            {
                voiceId = VoiceId.FindValue(aIResponse.Result.Parameters["VoiceId"].ToString());
            }

            for(int messageIndex = 0; messageIndex < aIResponse.Result.Fulfillment.Messages.Count; messageIndex++)
            {
                var message = JObject.FromObject(aIResponse.Result.Fulfillment.Messages[messageIndex]);
                string type = message["type"].ToString();

                if (type == "0")
                {
                    string speech = message["speech"].ToString();
                    string filePath = await polly.Utter(speech, env.WebRootPath, voiceId);
                    polly.Play(filePath);
                }
                else if (type == "4")
                {
                    var payload = JsonConvert.DeserializeObject<CustomPayload>(message["payload"].ToString());
                    if(payload.Task == "delay")
                    {
                        await Task.Delay(int.Parse(payload.Parameters.First().ToString()));
                    }
                    else if (payload.Task == "voice")
                    {
                        voiceId = VoiceId.FindValue(payload.Parameters.First().ToString());
                    }
                }
            }

            var gs = new GoogleSpeech();
            await gs.InitRecognitionConfig();
            var transcript = await gs.MicStreamingRecognize();
            await ActionCallback(new VoiceCapturedModel
            {
                CallSid = record.CallSid,
                SpeechResult = transcript
            });

            string host = $"{Request.Scheme}://{Request.Host}";
        }
    }
}
