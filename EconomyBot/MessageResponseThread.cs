using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EconomyBot
{
    public class MessageResponseThread
    {
        ulong respondingUserID;
        ulong responseChannelID;
        Regex filterRegex = new Regex(@".", RegexOptions.None); //catch anything

        public event EventHandler ResponseReceived;

        public MessageResponseThread(ulong user, ulong channel) {
            respondingUserID = user;
            responseChannelID = channel;
        }
        public MessageResponseThread(ulong user, ulong channel, Regex r)
        {
            respondingUserID = user;
            responseChannelID = channel;
            filterRegex = r;

            ///Yes or No answer
            ///(yes|nope|yep|no|y|n)/gix
        }

        public void CheckResponse(SocketMessage msg) {
            if (msg.Author.Id == respondingUserID &&
                msg.Channel.Id == responseChannelID &&
                filterRegex.IsMatch(msg.Content)) {
                OnResponseReceived(new MessageResponseEventArgs(msg.Content));
                CoreClass.responseThreads.Remove(this);
            }
        }

        protected virtual void OnResponseReceived(MessageResponseEventArgs e)
        {
            EventHandler handler = ResponseReceived;
            handler?.Invoke(this, e);
        }

        public class MessageResponseEventArgs : EventArgs {
            public string message;
            public MessageResponseEventArgs(string message) {
                this.message = message;
            }
        }
    }
}
