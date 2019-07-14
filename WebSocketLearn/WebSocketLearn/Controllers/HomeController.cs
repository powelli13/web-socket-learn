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

        private async Task TellJokes(HttpContext context, WebSocket webSocket)
        {
            JokeModel[] jokes = LoadJokes();
            Random rand = new Random();
            int jokeIndex = rand.Next(jokes.Length);

            ArraySegment<byte> bytesToSend = new ArraySegment<byte>();
            byte[] readBuffer = new byte[1024];
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

                // cleanse the buffer
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
                clientResponse = Encoding.ASCII.GetString(readBuffer).Trim('\0');

                jokeState = GetJokeState(clientResponse);

                switch (jokeState)
                {
                    case JokeState.KnockKnock:
                        bytesToSend = StringToBytes(kk);

                        await webSocket.SendAsync(
                            bytesToSend,
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                        break;
                 
                    case JokeState.Who:
                        bytesToSend = StringToBytes(jokes[jokeIndex].Who);

                        await webSocket.SendAsync(
                            bytesToSend,
                            WebSocketMessageType.Text,  
                            true,
                            CancellationToken.None
                        );
                        break;
                 
                    case JokeState.Punchline:
                        bytesToSend = StringToBytes(jokes[jokeIndex].Punchline);

                        jokeIndex = rand.Next(jokes.Length);

                        await webSocket.SendAsync(
                            bytesToSend,
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                        break;
                    default:
                        bytesToSend = StringToBytes("That doesn't make sense.");

                        await webSocket.SendAsync(
                            bytesToSend,
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
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

        public JokeModel[] LoadJokes()
        {
            return new JokeModel[]
            {
                new JokeModel("Cow says.", "No a Cow says MOOO!!"),
                new JokeModel("Kanga.", "Actually, it's kangaroo."),
                new JokeModel("Beats.", "Beats me."),
                new JokeModel("A broken pencil.", "Nevermind, it's pointless."),
                new JokeModel("Little old lady.", "I didn't know you could yodel."),
                new JokeModel("Etch.", "Bless you."),
                new JokeModel("Spell", "W - H - O."),
                new JokeModel("Orange", "Orange you going to answer the door?")
            };
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
