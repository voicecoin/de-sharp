using Amazon.Polly;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Voicecoin.AiBot;
using Voicecoin.RestApi;

namespace Voicecoin.Test
{
    [TestClass]
    public class SalesBotTest : TestEssential
    {
        [TestMethod]
        public async Task TestBuyProductAsync()
        {
            VoiceId voiceId = VoiceId.Joanna;

            var intent = new IntentClassifer(dialogApiKey).TextRequest(sessionId, $"Hello");
            var polly = new PollyUtter(awsAccessKey, awsSecretKey);
            string filePath = await polly.Utter(intent.Result.Fulfillment.Speech, recordsBaseDir, voiceId);
            polly.Play(filePath);

            intent = new IntentClassifer(dialogApiKey).TextRequest(sessionId, $"I need help");
            filePath = await polly.Utter(intent.Result.Fulfillment.Speech, recordsBaseDir, voiceId);
            polly.Play(filePath);

            intent = new IntentClassifer(dialogApiKey).TextRequest(sessionId, $"I want to chat with Alpha Go");

            if (intent.Result.Metadata.IntentName == "Transfer2SalesBot - yes")
            {
                filePath = await polly.Utter("Great, It's connected to for you.", recordsBaseDir, voiceId);
                polly.Play(filePath);
            }

            if (intent.Result.Metadata.IntentName == "AlphaGo")
            {
                filePath = await polly.Utter("OK, it's connected for you.", recordsBaseDir, voiceId);
                polly.Play(filePath);
            }

            if (intent.Result.Parameters.ContainsKey("VoiceId")
                && !String.IsNullOrEmpty(intent.Result.Parameters["VoiceId"].ToString()))
            {
                voiceId = VoiceId.FindValue(intent.Result.Parameters["VoiceId"].ToString());
            }

            filePath = await polly.Utter(intent.Result.Fulfillment.Speech, recordsBaseDir, voiceId);
            polly.Play(filePath);
        }
    }
}
