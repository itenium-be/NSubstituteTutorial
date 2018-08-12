using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.Exceptions;
using NUnit.Framework;

namespace NSubstituteTutorial
{
    /// <summary>
    /// Not covered by the blog post
    /// </summary>
    public class NotCovered
    {
        [Test]
        public void WhenDo_WithFullControl()
        {
            var sub = Substitute.For<ICalculator>();

            int calls = 0;
            sub
                .When(x => x.Add(0, 0))
                .Do(
                    // Also: Callback.Always, Callback.FirstThrow and Callback.AlwaysThrow
                    Callback.First((CallInfo callInfo) => Debug.WriteLine("first call"))
                        .Then((CallInfo callInfo) => Debug.WriteLine("2nd call"))
                        .Then((CallInfo callInfo) => Debug.WriteLine("3rd call"))
                        .ThenThrow<Exception>((CallInfo callInfo) => throw new Exception())
                        .ThenKeepDoing((CallInfo callInfo) => Debug.WriteLine("subsequent calls"))
                        // Also: ThenKeepThrowing()
                        .AndAlways((CallInfo callInfo) => calls++)
                );

            for (int i = 0; i < 3; i++)
            {
                sub.Add(0, 0);
            }
            Assert.That(calls, Is.EqualTo(3));
        }

        #region InvokeArgumentCallbacks
        public interface IOrderProcessor
        {
            void ProcessOrder(Action<bool> orderProcessed);
        }

        public class OrderPlacedCommand
        {
            private readonly IOrderProcessor _orderProcessor;

            public bool OkCalled { get; set; }

            public OrderPlacedCommand(IOrderProcessor orderProcessor)
            {
                _orderProcessor = orderProcessor;
            }

            public void Execute()
            {
                _orderProcessor.ProcessOrder(
                    wasOk =>
                    {
                        if (wasOk)
                            OkCalled = true;
                    }
                );
            }
        }

        [Test]
        public void InvokeArgumentCallbacks()
        {
            var processor = Substitute.For<IOrderProcessor>();
            processor.ProcessOrder(Arg.Invoke(true)); // <-- Magic here

            var command = new OrderPlacedCommand(processor);
            command.Execute();

            Assert.True(command.OkCalled);
        }
        #endregion

        #region Checking Received Calls 
        [Test]
        public void Checking_CheckingIndexers()
        {
            var dictionary = Substitute.For<IDictionary<string, int>>();
            dictionary["test"] = 1;
            dictionary.Received()["test"] = 1;
            dictionary.Received()["test"] = Arg.Is<int>(x => x < 5);
        }

        // For Events, see: http://nsubstitute.github.io/help/received-calls/#checking_event_subscriptions
        #endregion

        #region Received in order
        // http://nsubstitute.github.io/help/received-in-order/
        //[Test]
        //public void Checking_OrderOfCalls()
        //{
        //    var connection = Substitute.For<IConnection>();
        //    var command = Substitute.For<ICommand>();
        //    var subject = new Controller(connection, command);

        //    subject.DoStuff();

        //    Received.InOrder(() => {
        //        connection.Open();
        //        command.Run(connection);
        //        connection.Close();
        //    });
        //}
        #endregion
    }
}