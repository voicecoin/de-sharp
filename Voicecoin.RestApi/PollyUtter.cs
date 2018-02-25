using Amazon.Polly;
using Amazon.Polly.Model;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voicecoin.RestApi
{
    public class PollyUtter
    {
        private string awsAccessKey;
        private string awsSecretKey;
        private Amazon.RegionEndpoint REGION = Amazon.RegionEndpoint.USEast1;
        private AmazonPollyClient polly;

        public PollyUtter(string awsAccessKey, string awsSecretKey)
        {
            this.awsAccessKey = awsAccessKey;
            this.awsSecretKey = awsSecretKey;
            polly = new AmazonPollyClient(awsAccessKey, awsSecretKey, REGION);
        }

        public async Task<string> Utter(string text, string dir, VoiceId voice)
        {
            string speaker = "Voice Browser";
            if(voice == VoiceId.Matthew)
            {
                speaker = "Apple's Sales Bot";
            }

            ConsoleLogger.WriteLine(speaker, $"{text}");

            string fileName = (text + "-" + voice).GetMd5Hash() + ".mp3";

            string recordBasePath = Database.Configuration == null ? @"Records" : Database.Configuration.GetSection("RecordsPath").Value;
            string filePath = $"{dir}\\{recordBasePath}\\{fileName}";

            if (File.Exists(filePath))
            {
                return filePath;
            }

            SynthesizeSpeechRequest sreq = new SynthesizeSpeechRequest();
            sreq.Text = text;
            sreq.OutputFormat = OutputFormat.Mp3;
            sreq.VoiceId = voice;
            SynthesizeSpeechResponse sres = await polly.SynthesizeSpeechAsync(sreq);

            using (FileStream fileStream = File.Create(filePath))
            {
                sres.AudioStream.CopyTo(fileStream);
                fileStream.Flush();
                fileStream.Close();
            }

            return filePath;
        }

        public async Task UtterInStream(string text, VoiceId voice, Action<byte[], int> dataAvailable)
        {
            var req = new SynthesizeSpeechRequest();
            req.VoiceId = voice;
            req.TextType = TextType.Text;
            req.Text = text;
            req.OutputFormat = OutputFormat.Pcm;
            req.SampleRate = "16000";
            var response = await polly.SynthesizeSpeechAsync(req);

            using (MemoryStream ms = new MemoryStream())
            {
                response.AudioStream.CopyTo(ms);
                ms.Flush();
                ms.Position = 0;

                var buffer = new byte[1 * 640];
                int bytesRead;
                while ((bytesRead = await ms.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    dataAvailable(buffer, bytesRead);
                };
            }
        }

        public void Play(string filePath)
        {
            using (var audioFile = new AudioFileReader(filePath))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
