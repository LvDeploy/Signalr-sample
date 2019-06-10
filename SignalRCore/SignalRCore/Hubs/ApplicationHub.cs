using Microsoft.AspNetCore.SignalR;
using SignalRCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SignalRCore.Hubs
{
    public class ApplicationHub : Hub
    {
        public Task Send(ChatMessage chat)
        {
            return Clients.All.SendAsync("Send", chat.Message);
        }

        public ChannelReader<int> CountDown(int from)
        {
            var channel = Channel.CreateUnbounded<int>();

            _ = WriteToChannel(channel.Writer, from); //Descartes

            return channel.Reader;

            //função local
            async Task WriteToChannel(ChannelWriter<int> writer, int thing)
            {
                for (int i = thing; i >= 0; i--)
                {
                    await writer.WriteAsync(i);
                    await Task.Delay(1000);
                }

                writer.Complete();
            }
        }
    }
}
