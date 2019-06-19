using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSocketLearn.Models
{
    public enum JokeState
    {
        None,
        KnockKnock,
        Who,
        Punchline
    }

    public class JokeModel
    {
        public string Who { get; set; }
        public string Punchline { get; set; }

        public JokeModel(string who, string punchline)
        {
            Who = who;
            Punchline = punchline;
        }
    }
}
