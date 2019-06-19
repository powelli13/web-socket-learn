using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebSocketLearn.Models;

namespace WebSocketLearn.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult TellJokes()
        {
            ViewData["Message"] = "Enjoy some knock, knock jokes.";

            return View();
        }

        [Route("kkws")]
        public async Task<IActionResult> JokeWs()
        {

            if (this.HttpContext.WebSockets.IsWebSocketRequest)
            {

                WebSocket webSocket = await this.HttpContext.WebSockets.AcceptWebSocketAsync();
                await TellJokes(this.HttpContext, webSocket);

            }
            else
            {
                this.HttpContext.Response.StatusCode = 400;
            }

            return Ok();
        }

        // have the text to send defined somewhere
        // open the text file with a stream and read amount of bytes based on the state

        // have objects of knock knock jokes stored
            // initial
            // who punchline
        // read jokes from file, JSON?
        // when connection opens send Knock, knock
        // wait for client response, if 'whose there?' send initial
        // wait for response, if '... who?' send punchline
        // poll connection to ensure its open
        // 
        // 

        private async Task TellJokes(HttpContext context, WebSocket webSocket)
        {

            JokeModel joke = new JokeModel("Cow says.", "No, a cow says MOOO!!");
            byte[] readBuffer = new byte[1024];
            ArraySegment<byte> bytesToSend = new ArraySegment<byte>();
            string clientResponse;
            bool listening = true;
            JokeState jokeState = JokeState.None;

            // send knock knock
            string kk = "Knock, knock.";

            // send greeting
            bytesToSend = StringToBytes("Hi there, prepare yourself for hilarious jokes!");

            // send over greeting message, endOfMessage is true will force the client to read this right away
            await webSocket.SendAsync(
                bytesToSend,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            WebSocketReceiveResult result;
            int bytesReceived = 1024;

            // while they don't close
            while (listening)
            {

                // TODO cleanse the buffer find a better way to do this.
                //readBuffer = new byte[1024];
                for (int i = 0; i < bytesReceived; i++)
                {
                    byte.TryParse("\0", out readBuffer[i]);
                }

                // await whose there
                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(readBuffer),
                    CancellationToken.None
                );
                bytesReceived = result.Count < 1024 ? result.Count : 1024;

                if (result.CloseStatus.HasValue)
                {
                    listening = false;
                    break;
                }

                // send setup
                // TODO look for better method of stripping response data than just checking this one character
                clientResponse = string.Empty;
                clientResponse = Encoding.ASCII.GetString(readBuffer).Trim('\0');

                jokeState = GetJokeState(clientResponse);

                switch (jokeState)
                {
                    case JokeState.KnockKnock:
                        bytesToSend = StringToBytes(kk);

                        await webSocket.SendAsync(
                            bytesToSend,
                            result.MessageType,
                            result.EndOfMessage,
                            CancellationToken.None
                        );
                        break;
                 
                    case JokeState.Who:
                        bytesToSend = StringToBytes(joke.Who);

                        await webSocket.SendAsync(
                            bytesToSend,
                            result.MessageType,
                            result.EndOfMessage,
                            CancellationToken.None
                        );
                        break;
                 
                    case JokeState.Punchline:
                        bytesToSend = StringToBytes(joke.Punchline);

                        await webSocket.SendAsync(
                            bytesToSend,
                            result.MessageType,
                            result.EndOfMessage,
                            CancellationToken.None
                        );
                        break;
                    default:
                        // TODO give a message when they are out of the joke state?
                        // TODO this just means unrecognized command should we really break?5
                        //listening = false;
                        break;
                }
            }
        }

        public JokeState GetJokeState(string s)
        {
            if (s.Contains("joke", StringComparison.CurrentCultureIgnoreCase))
            {
                return JokeState.KnockKnock;
            }
            else if (s.Contains("there", StringComparison.CurrentCultureIgnoreCase))
            {
                return JokeState.Who;
            }
            else if (s.Contains("who?", StringComparison.CurrentCultureIgnoreCase))
            {
                return JokeState.Punchline;
            }

            return JokeState.None;
        }
        public ArraySegment<byte> StringToBytes(string s)
        {
            byte[] encodedText = Encoding.ASCII.GetBytes(s);
            return new ArraySegment<byte>(encodedText);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
