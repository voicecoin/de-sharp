using Amazon.Polly;
using ApiAiSDK.Model;
using Google.Cloud.Speech.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voicecoin.AiBot;

namespace Voicecoin.RestApi
{
    public static class NexmoExtensions
    {
        public static IApplicationBuilder UseNexmoAudioStream(this IApplicationBuilder app, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 1 * 1024
            };
            app.UseWebSockets(webSocketOptions);
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/socket")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Echo(context, webSocket, configuration, hostingEnvironment);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            return app;
        }

        private static async Task Echo(HttpContext context, WebSocket webSocket, IConfiguration config, IHostingEnvironment env)
        {
            var gv = new GoogleSpeech();
            await gv.InitRecognitionConfig();

            var intentRecognizer = new IntentClassifer(config.GetSection("dialogflow:apiKey").Value);
            var polly = new PollyUtter(config.GetSection("Aws:AWSAccessKey").Value, config.GetSection("Aws:AWSSecretKey").Value);

            var buffer = new byte[1 * 1024];
            WebSocketReceiveResult wsResult;

            // Read from the microphone and stream to API.
            Console.WriteLine("Speak now.");

            do
            {
                wsResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                string transcript = await gv.BytesStreamingRecognize(buffer);

                if (!String.IsNullOrEmpty(transcript))
                {
                    var intent = intentRecognizer.TextRequest(context.Connection.Id, transcript);
                    await Utter(webSocket, polly, intent);

                    gv = new GoogleSpeech();
                    await gv.InitRecognitionConfig();
                }

            } while (!wsResult.CloseStatus.HasValue);

            await webSocket.CloseAsync(wsResult.CloseStatus.Value, wsResult.CloseStatusDescription, CancellationToken.None);
        }

        private static async Task Utter(WebSocket webSocket, PollyUtter polly, AIResponse aIResponse)
        {
            VoiceId voiceId = VoiceId.Joanna;

            if (aIResponse.Result.Parameters.ContainsKey("VoiceId")
                && !String.IsNullOrEmpty(aIResponse.Result.Parameters["VoiceId"].ToString()))
            {
                voiceId = VoiceId.FindValue(aIResponse.Result.Parameters["VoiceId"].ToString());
            }

            await polly.UtterInStream(aIResponse.Result.Fulfillment.Speech, voiceId, async (buffer1, bytesRead) =>
            {
                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(buffer1, 0, bytesRead), WebSocketMessageType.Binary, bytesRead == 0, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            });
        }
    }
}
