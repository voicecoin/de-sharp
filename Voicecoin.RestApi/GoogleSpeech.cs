using DotNetToolkit;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public async Task<String> StreamingRecognize()
        {
            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                Console.WriteLine("No microphone!");
                return "No microphone!";
            }

            //GoogleCredential credential = GoogleCredential.FromFile(env.ContentRootPath + "\\settings.google-credential.json");
            GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();
            var speech = SpeechClient.Create();
            var streamingCall = speech.StreamingRecognize();

            // Write the initial request with the config.
            await streamingCall.WriteAsync(
                new StreamingRecognizeRequest()
                {
                    StreamingConfig = new StreamingRecognitionConfig()
                    {
                        Config = new RecognitionConfig()
                        {
                            Encoding =
                            RecognitionConfig.Types.AudioEncoding.Linear16,
                            SampleRateHertz = 16000,
                            LanguageCode = "en",
                        },
                        InterimResults = true,
                    }
                });


            string transcript = String.Empty;

            // Print responses as they arrive.
            bool isFinal = false;
            Task printResponses = Task.Run(async () =>
            {
                while (await streamingCall.ResponseStream.MoveNext(
                    default(CancellationToken)))
                {
                    foreach (var result in streamingCall.ResponseStream
                        .Current.Results)
                    {
                        foreach (var alternative in result.Alternatives)
                        {
                            Console.WriteLine(alternative.Transcript);
                        }

                        isFinal = result.IsFinal;

                        if (result.IsFinal)
                        {
                            transcript = String.Join("", result.Alternatives.Select(x => x.Transcript));
                            ConsoleLogger.WriteLine("System", $"{transcript} [Stability: {result.Stability}]");
                        }
                    }
                }
            });

            // Read from the microphone and stream to API.
            object writeLock = new object();
            bool writeMore = true;
            var waveIn = new NAudio.Wave.WaveInEvent();
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            waveIn.DataAvailable +=
                (object sender, NAudio.Wave.WaveInEventArgs args) =>
                {
                    lock (writeLock)
                    {
                        if (!writeMore) return;
                        streamingCall.WriteAsync(
                            new StreamingRecognizeRequest()
                            {
                                AudioContent = Google.Protobuf.ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                            })
                            .Wait();
                    }
                };

            waveIn.StartRecording();
            Console.WriteLine("Speak now...");

            // Set recording timeout
            int timeoutInSeconds = 5;
            double timeoutInSecondsElapsed = 0;

            do
            {
                // Set speech timeout
                await Task.Delay(TimeSpan.FromSeconds(0.1));
                timeoutInSecondsElapsed += 0.1;
            } while (!isFinal && timeoutInSecondsElapsed <= timeoutInSeconds);

            // Stop recording and shut down.
            waveIn.StopRecording();
            Console.WriteLine("Stop capture voice...");

            lock (writeLock) writeMore = false;
            await streamingCall.WriteCompleteAsync();
            await printResponses;

            return transcript;
        }
    }
}
