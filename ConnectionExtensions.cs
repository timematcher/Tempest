﻿//
// ConnectionExtensions.cs
//
// Author:
//   Eric Maupin <me@ermau.com>
//
// Copyright (c) 2011 Eric Maupin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Net;
using System.Threading;
using Tempest.InternalProtocol;

namespace Tempest
{
	/// <summary>
	/// Holds extension methods for <see cref="IConnection"/>
	/// </summary>
	public static class ConnectionExtensions
	{
		/// <summary>
		/// Notifys the connection of the reason of disconnection and disconnects.
		/// </summary>
		/// <param name="self">The connection to notify and disconnect.</param>
		/// <param name="reason">The reason to disconnect.</param>
		public static void NotifyAndDisconnect (this IConnection self, ConnectionResult reason)
		{
			if (self == null)
				throw new ArgumentNullException ("self");

			self.Send (new DisconnectMessage { Reason = reason });
			self.Disconnect (false, reason);
		}

		public static void On<TMessage> (this IConnection self, Func<TMessage, bool> predicate, Action<TMessage, MessageEventArgs> callback)
			where TMessage : Message
		{
			if (self == null)
				throw new ArgumentNullException ("self");
			if (predicate == null)
				throw new ArgumentNullException ("predicate");
			if (callback == null)
				throw new ArgumentNullException ("callback");

			EventHandler<MessageEventArgs> evcallback = null;
			evcallback = (s, e) =>
			{
				TMessage msg = e.Message as TMessage;
				if (msg == null || !predicate (msg))
					return;

				self.MessageReceived -= evcallback;
				callback (msg, e);
			};

			self.MessageReceived += evcallback;
		}
	}

	public static class ClientConnectionExtensions
	{
		public static ConnectionResult Connect (this IClientConnection self, EndPoint endPoint, MessageTypes messageTypes)
		{
			ConnectionResult result = ConnectionResult.FailedUnknown;
			AutoResetEvent wait = new AutoResetEvent (false);

			EventHandler<ClientConnectionEventArgs> connected = null;
			EventHandler<DisconnectedEventArgs> disconnected = null;

			connected = (s, e) =>
			{
				self.Connected -= connected;
				self.Disconnected -= disconnected;
				result = ConnectionResult.Success;
				wait.Set();
			};

			disconnected = (s, e) =>
			{
				self.Connected -= connected;
				self.Disconnected -= disconnected;
				result = e.Result;
				wait.Set();
			};

			self.ConnectAsync (endPoint, messageTypes);
			wait.WaitOne();

			return result;
		}
	}
}