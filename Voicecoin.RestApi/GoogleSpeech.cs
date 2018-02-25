using DotNetToolkit;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Google.Cloud.Speech.V1.SpeechClient;

namespace Voicecoin.RestApi
{
    public class GoogleSpeech
    {
        private string Transcript { get; set; }
        private SpeechClient SpeechClient { get; set; }
        private StreamingRecognizeStream StreamCall { get; set; }

        public GoogleSpeech()
        {
            GoogleCredential credential = GoogleCredential.GetApplicationDefault();
        }

        public async Task InitRecognitionConfig()
        {
            SpeechClient = Create();
            StreamCall = SpeechClient.StreamingRecognize();

            // Write the initial request with the config.
            await StreamCall.WriteAsync(
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

            // Print responses as they arrive.
            Task printResponses = Task.Run(async () =>
            {
                while (await StreamCall.ResponseStream.MoveNext(
                    default(CancellationToken)))
                {
                    foreach (var result in StreamCall.ResponseStream
                        .Current.Results)
                    {
                        foreach (var alternative in result.Alternatives)
                        {
                            Console.WriteLine(alternative.Transcript);
                        }

                        if (result.IsFinal)
                        {
                            Transcript = String.Join("", result.Alternatives.Select(x => x.Transcript));
                        }
                    }
                }
            });
        }

        public async Task<string> FileRecognize(string urlPath)
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

        public async Task<object> FileStreamingRecognize(string filePath)
        {
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
            // Print responses as they arrive.
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
                    }
                }
            });
            // Stream the file content to the API.  Write 2 32kb chunks per 
            // second.
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                var buffer = new byte[32 * 1024];
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(
                    buffer, 0, buffer.Length)) > 0)
                {
                    await streamingCall.WriteAsync(
                        new StreamingRecognizeRequest()
                        {
                            AudioContent = Google.Protobuf.ByteString
                            .CopyFrom(buffer, 0, bytesRead),
                        });
                    await Task.Delay(500);
                };
            }
            await streamingCall.WriteCompleteAsync();
            await printResponses;
            return 0;
        }

        public async Task<String> MicStreamingRecognize(string lang = "en")
        {
            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                Console.WriteLine("No microphone!");
                return "No microphone!";
            }

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
                        StreamCall.WriteAsync(
                            new StreamingRecognizeRequest()
                            {
                                AudioContent = Google.Protobuf.ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                            })
                            .Wait();
                    }
                };

            waveIn.StartRecording();

            ConsoleLogger.WriteLine("Voice Browser", $"Listening...");
            //Console.WriteLine("Speak now...");

            // Set recording timeout
            int timeoutInSeconds = 10;
            double timeoutInSecondsElapsed = 0;

            do
            {
                // Set speech timeout
                await Task.Delay(TimeSpan.FromSeconds(0.1));
                timeoutInSecondsElapsed += 0.1;
            } while (String.IsNullOrEmpty(Transcript) && timeoutInSecondsElapsed <= timeoutInSeconds);

            // Stop recording and shut down.
            waveIn.StopRecording();
            //Console.WriteLine("Stop capture voice...");

            lock (writeLock) writeMore = false;
            //await StreamingCall.WriteCompleteAsync();
            //await printResponses;

            return Transcript;
        }

        public async Task<string> BytesStreamingRecognize(byte[] buffer)
        {
            if (String.IsNullOrEmpty(Transcript))
            {
                StreamCall.WriteAsync(
                    new StreamingRecognizeRequest()
                    {
                        AudioContent = Google.Protobuf.ByteString
                            .CopyFrom(buffer, 0, buffer.Length)
                    }).Wait();

                return String.Empty;
            }
            else
            {
                // Stop recording and shut down.
                await StreamCall.WriteCompleteAsync();

                Console.WriteLine($"Transcript: {Transcript}");

                return Transcript;
            }
        }
    }
}
