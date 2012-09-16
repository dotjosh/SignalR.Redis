using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookSleeve;
using SignalR.Redis.Infrastructure;

namespace SignalR.Redis
{
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private readonly int _db;
        private readonly string[] _channels;
        private RedisConnection _connection;
        private RedisSubscriberConnection _channel;
        private Task _connectTask;

        private readonly TaskQueue _publishQueue = new TaskQueue();

        public RedisMessageBus(string server, int port, string password, int db, IEnumerable<string> channels, IDependencyResolver resolver)
            : base(resolver)
        {
            _db = db;
            _channels = channels.ToArray();

            _connection = new RedisConnection(host: server, port: port, password: password);

            _connection.Closed += OnConnectionClosed;
            _connection.Error += OnConnectionError;

            // Start the connection
            _connectTask = _connection.Open().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // Do something useful here
                    Debug.WriteLine(task.Exception.GetBaseException());
                }
                else
                {
                    // Create a subscription channel in redis
                    _channel = _connection.GetOpenSubscriberChannel();

                    // Subscribe to the registered connections
                    _channel.Subscribe(_channels, OnMessage);
                }
            });
        }

        protected override Task Send(Message[] messages)
        {
            return Interlocked.Exchange(ref _connectTask, TaskAsyncHelper.Empty).Then(() =>
            {
                var tcs = new TaskCompletionSource<object>();

                SendImpl(messages.GroupBy(m => m.Source).GetEnumerator(), tcs);

                return tcs.Task;
            });
        }

        private void SendImpl(IEnumerator<IGrouping<string, Message>> enumerator, TaskCompletionSource<object> tcs)
        {
            if (!enumerator.MoveNext())
            {
                tcs.TrySetResult(null);
            }
            else
            {
                IGrouping<string, Message> group = enumerator.Current;

                // Get the channel index we're going to use for this message
                int channelIndex = Math.Abs(group.Key.GetHashCode()) % _channels.Length;
                string channel = _channels[channelIndex];

                // Increment the channel number
                _connection.Strings.Increment(_db, channel).Then((id, ch) =>
                {
                    var message = new RedisMessage(id, group.ToArray());

                    return _connection.Publish(ch, message.GetBytes());
                },
                channel).Then(() => SendImpl(enumerator, tcs));
            }
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            // Should we auto reconnect?
        }

        private void OnConnectionError(object sender, BookSleeve.ErrorEventArgs e)
        {
            // How do we bubble errors?
        }

        private void OnMessage(string key, byte[] data)
        {
            // The key is the stream id (channel)
            var message = RedisMessage.Deserialize(data);
 
            _publishQueue.Enqueue(() => OnReceived(key, (ulong)message.Id, message.Messages));
        }

        public override void Dispose()
        {
            if (_channel != null)
            {
                _channel.Unsubscribe(_channels);
                _channel.Close(abort: true);
            }

            if (_connection != null)
            {
                _connection.Close(abort: true);
            }
        }        
    }
}
