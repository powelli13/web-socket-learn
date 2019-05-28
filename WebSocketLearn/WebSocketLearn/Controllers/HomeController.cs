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

        public IActionResult ReadDickens()
        {
            ViewData["Message"] = "Enjoy some classic English literature.";

            return View();
        }

        [Route("dickensws")]
        public async Task<IActionResult> DickensWs()
        {

            if (this.HttpContext.WebSockets.IsWebSocketRequest)
            {

                WebSocket webSocket = await this.HttpContext.WebSockets.AcceptWebSocketAsync();
                await StreamBook(this.HttpContext, webSocket);

            }
            else
            {
                this.HttpContext.Response.StatusCode = 400;
            }

            return Ok();
        }

        // have the text to send defined somewhere
        // open the text file with a stream and read amount of bytes based on the state

        // have state consisting of send speed and possibly text defined
        // poll connection to ensure its open
        // send apropriate amount of text data to client, according to state
        // 

        private async Task StreamBook(HttpContext context, WebSocket webSocket)
        {
            var readBuffer = new byte[1024 * 4];
            int count = 0;

            string bookFilePath = @"C:\Users\ipowell\Source\Repos\web-socket-learn\WebSocketLearn\WebSocketLearn\book_text\oliver_twist.txt";
            int countRequested = 10;
            int readCount = 0;
            int index = 0;

            char[] bookBuffer = await GetBookContents();
            char[] sendBuffer = new char[10];

            while (count++ < 100)
            {
                // Prepare bytes to send
                for (int i = 0; i < 10; i++)
                {
                    sendBuffer[i] = bookBuffer[index];
                    index++;
                }

                byte[] encodedText = Encoding.ASCII.GetBytes(sendBuffer);
                ArraySegment<byte> bytesToSend = new ArraySegment<byte>(encodedText);

                await webSocket.SendAsync(
                    bytesToSend,
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }

            await webSocket.SendAsync(
                new ArraySegment<byte>(null),
                WebSocketMessageType.Binary,
                false,
                CancellationToken.None
            );
        }

        private async Task<char[]> GetBookContents()
        {
            string bookFilePath = @"C:\Users\ipowell\Source\Repos\web-socket-learn\WebSocketLearn\WebSocketLearn\book_text\oliver_twist.txt";
            char[] buffer;

            using (StreamReader sr = new StreamReader(bookFilePath))
            {
                buffer = new char[sr.BaseStream.Length];
                await sr.ReadAsync(buffer, 0, (int)sr.BaseStream.Length);
            }

            return buffer;
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
