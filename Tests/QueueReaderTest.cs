using System;
using Moq;
using NUnit.Framework;
using PRI.Messaging.Patterns;
using Tests.Mocks;
using Newtonsoft.Json;
using System.Messaging;
using System.Threading;
using Microsoft.Win32;
using PRI.Messaging.Patterns.Internal;

namespace Tests
{
	[TestFixture]
	public class QueueReaderTest
	{
		[Test, Explicit]
		public void TestMsmq()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSMQ"))
			{
				if (key == null || key.SubKeyCount == 0) Assert.Inconclusive("MSMQ not installed");
			}

			var message = new Message
			{
				Body = "Sample Message",
				Recoverable = false,
				Formatter = new BinaryMessageFormatter()
			};
			var queueName = @".\private$\Orders";
			MessageQueue msgQ;
			if (MessageQueue.Exists(queueName))
			{
				msgQ = new MessageQueue(queueName);
			}
			else
			{
				msgQ = MessageQueue.Create(queueName);
			}
			try
			{
				using (msgQ)
				{
					msgQ.Formatter = new BinaryMessageFormatter();
					msgQ.Send(message);
					var receivedMessage = msgQ.Receive(TimeSpan.FromSeconds(1));
					Assert.AreEqual(message.Body, receivedMessage.Body);
				}
			}
			finally
			{
				MessageQueue.Delete(queueName);
			}
		}


		[Test]
		public void TestQueueReaderWithMockMsmq()
		{
			var actualMessage = new Message1{CorrelationId = "f3e608d4-96cf-4093-9923-91b13f4b7555"};
			var messageQueueMock = new Mock<IMessageQueue>();
			var messageMock = new MessageQueueMessageWrapper(new Message{Body = actualMessage});

			messageQueueMock.Setup(m => m.EndReceive(It.IsAny<IAsyncResult>())).Returns(messageMock);
			var beginReceiveCalled = false;
			messageQueueMock.Setup(m => m.BeginReceive(It.IsAny<TimeSpan>()))
				.Callback(
					() =>
					{
						if (beginReceiveCalled) throw CreateMessageQueueException();
						beginReceiveCalled = true;
						messageQueueMock.Raise(m => m.ReceiveCompleted += null,
							new MessageQueueReceiveCompletedEventArgs(null));
					});

			Message1 message = null;
			var receiverReader = new MsmqReader<Message1>(messageQueueMock.Object, message1 => (Message1) message1.Message.Body);
			receiverReader.AttachConsumer(new ActionConsumer<Message1>(m => message = m));
			receiverReader.Start();

			Assert.IsNotNull(message);
			Assert.AreEqual("f3e608d4-96cf-4093-9923-91b13f4b7555", message.CorrelationId);
			messageQueueMock.Verify(m => m.BeginReceive(It.IsAny<TimeSpan>()), Times.Exactly(2));
		}

		[Test, Explicit]
		public void TestMsmqReader()
		{
			var queueName = @".\private$\Orders";
			MessageQueue messageQueue;
			if (MessageQueue.Exists(queueName))
			{
				messageQueue = new MessageQueue(queueName);
			}
			else
			{
				messageQueue = MessageQueue.Create(queueName);
			}
			try
			{
				Message1 message;
				EventWaitHandle waitHandle;
				using (var receiverReader = new MsmqReader<Message1>(new MessageQueueWrapper(messageQueue),
					message1 => (Message1) message1.Message.Body)
					)
				{
					message = null;
					waitHandle = new ManualResetEvent(false);
					receiverReader.AttachConsumer(new ActionConsumer<Message1>(m =>
					{
						message = m;
						waitHandle.Set();
					}));
					receiverReader.Start();
					var msmqMessage = new Message
					{
						Body = new Message1 {CorrelationId = "f3e608d4-96cf-4093-9923-91b13f4b7555"},
						Formatter = new BinaryMessageFormatter()
					};
					messageQueue.Formatter = msmqMessage.Formatter;
					messageQueue.Send(msmqMessage);
					var result = waitHandle.WaitOne(TimeSpan.FromSeconds(2));
				}

				Assert.IsNotNull(message);
				Assert.AreEqual("f3e608d4-96cf-4093-9923-91b13f4b7555", message.CorrelationId);
			}
			finally
			{
				MessageQueue.Delete(queueName);
			}
		}

		private static MessageQueueException CreateMessageQueueException()
		{
			return JsonConvert.DeserializeObject<MessageQueueException>("{\"NativeErrorCode\":-1072824293,\"ClassName\":\"System.Messaging.MessageQueueException\",\"Message\":\"External component has thrown an exception.\",\"Data\":null,\"InnerException\":null,\"HelpURL\":null,\"StackTraceString\":null,\"RemoteStackTraceString\":null,\"RemoteStackIndex\":0,\"ExceptionMethod\":null,\"HResult\":-2147467259,\"Source\":null,\"WatsonBuckets\":null}");
		}
	}
}