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

        public PollyUtter(string awsAccessKey, string awsSecretKey)
        {
            this.awsAccessKey = awsAccessKey;
            this.awsSecretKey = awsSecretKey;
        }

        public async Task<string> Utter(string text, string dir, VoiceId voice)
        {
            string fileName = (text + "-" + voice).GetMd5Hash() + ".mp3";

            string recordBasePath = Database.Configuration == null ? @"Records" : Database.Configuration.GetSection("RecordsPath").Value;
            string filePath = $"{dir}\\{recordBasePath}\\{fileName}";

            if (File.Exists(filePath))
            {
                return filePath;
            }

            string awsAccessKeyId = awsAccessKey;

            string awsSecretAccessKey = awsSecretKey;

            Amazon.RegionEndpoint REGION = Amazon.RegionEndpoint.USEast1;

            AmazonPollyClient pc = new AmazonPollyClient(awsAccessKeyId, awsSecretAccessKey, REGION);

            SynthesizeSpeechRequest sreq = new SynthesizeSpeechRequest();
            sreq.Text = text;
            sreq.OutputFormat = OutputFormat.Mp3;
            sreq.VoiceId = voice;
            SynthesizeSpeechResponse sres = await pc.SynthesizeSpeechAsync(sreq);

            using (FileStream fileStream = File.Create(filePath))
            {
                sres.AudioStream.CopyTo(fileStream);
                fileStream.Flush();
                fileStream.Close();
            }

            return filePath;
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
