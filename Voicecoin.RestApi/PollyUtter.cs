using Amazon.Polly;
using Amazon.Polly.Model;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Voicecoin.RestApi
{
    public class PollyUtter
    {
        public async Task<string> Utter(string text, string dir, VoiceId voice)
        {
            string fileName = GetMd5Hash(text + "-" + voice) + ".mp3";

            string recordBasePath = Database.Configuration.GetSection("RecordsPath").Value;
            string filePath = $"{dir}\\{recordBasePath}\\{fileName}";

            if (File.Exists(filePath))
            {
                return filePath;
            }

            string awsAccessKeyId = Database.Configuration.GetSection("Aws:AWSAccessKey").Value;

            string awsSecretAccessKey = Database.Configuration.GetSection("Aws:AWSSecretKey").Value;

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

        public string GetMd5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}
