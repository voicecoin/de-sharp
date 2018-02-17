using Amazon.Polly;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.AspNet.Core;
using Twilio.Http;
using Twilio.TwiML;
using Voicecoin.AiBot;

namespace Voicecoin.RestApi
{
    [Produces("application/xml")]
    [Route("[controller]/[action]")]
    public class TwilioVoiceController : TwilioController
    {
        private IConfiguration config;
        private IHostingEnvironment env;

        protected Database dc { get; set; }

        public TwilioVoiceController(IConfiguration Configuration, IHostingEnvironment hostingEnvironment)
        {
            config = Configuration;
            env = hostingEnvironment;
            dc = new DefaultDataContextLoader().GetDefaultDc();
        }

        public async Task<IActionResult> Incoming(TwilioCallbackModel callRequestInfoModel)
        {
            ConsoleLogger.WriteLine("System", $"Received call from {callRequestInfoModel.CallSid}");
            string host = $"{Request.Scheme}://{Request.Host}";
            var action = new Uri($"{host}/TwilioVoice/ActionCallback");
            var response = new VoiceResponse();

            var intent = new IntentClassifer(config).TextRequest(callRequestInfoModel.CallSid, $"Hello");

            response.Pause(length: 1);
            response.Play(await VoiceResponsePlay(intent.Result.Fulfillment.Speech, VoiceId.Joanna));

            intent = new IntentClassifer(config).TextRequest(callRequestInfoModel.CallSid, $"I need help");
            response.Play(await VoiceResponsePlay(intent.Result.Fulfillment.Speech, VoiceId.Joanna));

#if TWILIO_RECORD
            response.Record(action: action, trim: "trim-silence", method: HttpMethod.Post);
#else
            response.Gather(input: "speech dtmf", action: action, method: HttpMethod.Post, speechTimeout: "1", timeout: 5);
#endif

            return Content(response.ToString(), "application/xml");
        }

        public async Task<IActionResult> ActionCallback(TwilioCallbackModel record)
        {
            var response = new VoiceResponse();

#if TWILIO_RECORD
            Console.WriteLine($"{DateTime.Now}: {record.RecordingUrl}");
            record.SpeechResult = await TranscriptTwilioRecord(record);
#endif
            ConsoleLogger.WriteLine("System", $"{record.SpeechResult} [Confidence: {record.Confidence}]");

            var aIResponse = new IntentClassifer(config).TextRequest(record.CallSid, record.SpeechResult);
            string text = aIResponse.Result.Fulfillment.Speech;
            VoiceId voiceId = VoiceId.Joanna;
            
            if(aIResponse.Result.Metadata.IntentName == "Transfer2SalesBot - yes")
            {
                response.Play(await VoiceResponsePlay("It's connected to the apple sales chatbot.", voiceId));
            }

            if (aIResponse.Result.Metadata.IntentName == "AlphaGo")
            {
                response.Play(await VoiceResponsePlay("OK, it's connected for you.", voiceId));
            }

            response.Pause(length: 1);

            if (aIResponse.Result.Contexts.Select(x => x.Name).Contains("alphagoaskuserintent"))
            {
                
            }

            if (aIResponse.Result.Parameters.ContainsKey("VoiceId")
                && !String.IsNullOrEmpty(aIResponse.Result.Parameters["VoiceId"].ToString()))
            {
                voiceId = VoiceId.FindValue(aIResponse.Result.Parameters["VoiceId"].ToString());
            }

            response.Play(await VoiceResponsePlay(text, voiceId));

            string host = $"{Request.Scheme}://{Request.Host}";

            response.Gather(input: "speech dtmf", action: new Uri($"{host}/TwilioVoice/ActionCallback"), method: HttpMethod.Post, speechTimeout: "1", timeout: 5);

            return Content(response.ToString(), "application/xml");
        }

        private async Task<string> TranscriptTwilioRecord(TwilioCallbackModel record)
        {
            string url = record.RecordingUrl;
            string recordBasePath = config.GetSection("RecordsPath").Value;

            var client = new RestClient(url);
            var request = new RestRequest("");
            var bytes = client.DownloadData(request);

            string filePath = recordBasePath + "\\" + record.From + "\\" + url.Split('/').Last() + ".wav";
            Directory.CreateDirectory(recordBasePath + "\\" + record.From);

            bytes.SaveAs(filePath);
            string transcript = null;
            try
            {
                transcript = await new GoogleSpeech().Recognize(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }

            return transcript;
        }

        private async Task<Uri> VoiceResponsePlay(string text, VoiceId voice)
        {
            string host = $"{Request.Scheme}://{Request.Host}";
            string url = await new PollyUtter().Utter(text, env.WebRootPath, voice);
            string externalLink = $"{host}{url.Replace(env.WebRootPath, String.Empty).Replace('\\', '/')}";
            return new Uri(externalLink);
        }
    }
}
