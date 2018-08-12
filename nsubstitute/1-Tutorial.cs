using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Windows.Input;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.Extensions;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Exceptions;
using NUnit.Framework;

namespace NSubstituteTutorial
{
    public class Tutorial
    {
        ICalculator nsub = Substitute.For<ICalculator>();

        #region CreateSubstitutes
        [Test]
        public void CreateSubstitute_ForInterface()
        {
            ICalculator isub = Substitute.For<ICalculator>();
            isub.Add(1, 3).Returns(4);
            Assert.That(isub.Add(1, 3), Is.EqualTo(4));
        }

        [Test]
        public void CreateSubstitute_ForClass()
        {
            // For a class with one int ctor parameter
            Calculator csub = Substitute.For<Calculator>(10);
            csub.Add(1, 3).Returns(4);
            Assert.That(csub.Add(1, 3), Is.EqualTo(4));

            // Works only for virtual/abstract members
            try
            {
                csub.Mode.Returns("HEX");
            }
            catch (CouldNotSetReturnDueToTypeMismatchException ex) when (ex.Message.Contains("Return values cannot be configured for non-virtual/non-abstract members"))
            {
                Assert.True(true, "NSubstitute only works with virtual/abstract members when substituting an actual class");
            }
        }

        #region PartialSubstitution
        [Test]
        public void CreateSubstitute_ForClass_PartialSubstitution()
        {
            var reader = Substitute.ForPartsOf<SummingReader>();

            // Without Arg. matchers, the actual ReadFile will be executed
            Assert.Throws<Exception>(() => reader.ReadFile("foo.txt").Returns("1,2,3"));

            // The Arg.Is makes sure that ReadFile is not executed
            reader.ReadFile(Arg.Is("foo.txt")).Returns("1,2,3");
            int result = reader.CalculateSum("foo.txt");
            Assert.That(result, Is.EqualTo(6));

            // Alternatively: Use DoNotCallBase to play it (a bit) safer.
            // Make sure the ReadFile call won't call real implementation
            reader = Substitute.ForPartsOf<SummingReader>();
            reader.When(x => x.ReadFile("foo.txt")).DoNotCallBase(); // <-- Magic here
            reader.ReadFile("foo.txt").Returns("1,2,3");
            result = reader.CalculateSum("foo.txt");
            Assert.That(result, Is.EqualTo(6));
        }

        /// <summary>
        /// Class to demonstrate PartialSubstitution
        /// </summary>
        public class SummingReader
        {
            /// <summary>
            /// We will actually execute this
            /// </summary>
            public virtual int CalculateSum(string path)
            {
                var s = ReadFile(path);
                return s.Split(',').Select(int.Parse).Sum();
            }

            /// <summary>
            /// While substituting this behavior
            /// </summary>
            /// <returns><see cref="Exception"/></returns>
            public virtual string ReadFile(string path)
            {
                throw new Exception($"Actually attempted to access '{path}' on filesystem!");
            }
        }
        #endregion

        [Test]
        public void CreateSubstitute_ForMultipleTypes()
        {
            // Multiple substitutions (at most one class)
            var msub = Substitute.For<ICalculator, IDisposable>();
            Assert.IsInstanceOf<ICalculator>(msub);
            Assert.IsInstanceOf<IDisposable>(msub);


            // For more than three types
            // If one of the types is a class, the second argument are the ctor parameters
            // If all are interfaces, null can be passed as second parameter.
            var gt3Sub = Substitute.For(
                new[] { typeof(IComparable), typeof(IDisposable), typeof(ICloneable), typeof(Calculator) },
                new object[] { 8 } // @base ctor argument for Calculator
            );
            Assert.IsInstanceOf<IDisposable>(gt3Sub);
            Assert.IsInstanceOf<Calculator>(gt3Sub);

            var calcer = (Calculator)gt3Sub;
            calcer.Add(1, 1).Returns(2);
            Assert.That(calcer.Add(1, 1), Is.EqualTo(2));
        }

        [Test]
        public void CreateSubstitute_ForDelegate()
        {
            var func = Substitute.For<Func<string>>();
            func().Returns("hello");
            Assert.That(func(), Is.EqualTo("hello"));
        }
        #endregion

        [Test]
        public void ArgumentMatching()
        {
            // Arg.Any and fixed value
            nsub.Add(Arg.Any<int>(), 5).Returns(10);
            Assert.That(nsub.Add(999, 5), Is.EqualTo(10));

            // Arg.Is: Fixed value and Predicate
            nsub.Add(Arg.Is(1), Arg.Is<int>(x => x < 0)).Returns(5);
            Assert.That(nsub.Add(1, -2), Is.EqualTo(5));

            // First matcher still applies
            Assert.That(nsub.Add(999, 5), Is.EqualTo(10));

            // ReturnsForAnyArgs overwrites everything
            nsub.Add(0, 0).ReturnsForAnyArgs(100);
            Assert.That(nsub.Add(1, -2), Is.EqualTo(100));
        }

        #region Checking Received Calls
        [Test]
        public void Checking_ReceivedCalls()
        {
            var sub = Substitute.For<ICalculator>();

            sub.DidNotReceiveWithAnyArgs().Add(0, 0);

            sub.Add(0, 1);
            sub.Received().Add(0, 1);
            sub.ReceivedWithAnyArgs().Add(0, 0);

            sub.DidNotReceive().Add(Arg.Any<int>(), 9);

            sub.ClearReceivedCalls();

            sub.Add(0, 1);
            sub.Add(0, 2);
            sub.Add(0, 3);
            sub.Received(2).Add(Arg.Is(0), Arg.Is<int>(x => x <= 2));
            // In this case the Arg.Is(0) is required by NSubstitute
            // (providing 0 directly results in an AmbiguousArgumentsException)
        }

        [Test]
        public void Checking_Properties()
        {
            // Property Getter
            string call = nsub.Mode;
            string result = nsub.Received().Mode;

            // Property Setter
            nsub.Mode = "BIN";
            nsub.Received().Mode = "BIN";
        }
        #endregion

        #region Providing Values
        [Test]
        public void ProvideValues_WithoutSetup_ReturnsDefaultOrEmpty()
        {
            var sub = Substitute.For<ICalculator>();

            // No setup provided
            Assert.That(sub.Add(1, 3), Is.EqualTo(default(int)));

            // No matching arguments
            nsub.Add(1, 3).Returns(4);
            Assert.That(nsub.Add(4, 5), Is.Not.EqualTo(4));

            // Default for IEnumerable<T> is Enumerable.Empty<T>()
            var testy = Substitute.For<VirtualsTest>();
            Assert.That(testy.Virtual.Count(), Is.EqualTo(0));
            Assert.Null(testy.NotVirtual);

            // Default for string is ""
            var identity = Substitute.For<IIdentity>();
            Assert.AreEqual("", identity.Name);

            // Members returning an interface will automatically return a sub (recursively)
            var principal = Substitute.For<IPrincipal>();
            IIdentity principalIdentity = principal.Identity;
            principalIdentity.Name.Returns("Neo");
            Assert.That(principalIdentity.Name, Is.EqualTo("Neo"));
        }

        public class VirtualsTest
        {
            public virtual IEnumerable<string> Virtual { get; set; }
            public IEnumerable<string> NotVirtual { get; set; }
        }

        [Test]
        public void ProvideValues_ForMethodsAndProperties()
        {
            // Methods
            nsub.Add(1, 3).Returns(4);
            Assert.That(nsub.Add(1, 3), Is.EqualTo(4));
            Assert.That(nsub.Add(1, 3), Is.EqualTo(4));

            // Properties
            nsub.Mode = "HEX";
            Assert.That(nsub.Mode, Is.EqualTo("HEX"));

            // Can also use the same syntax as for methods:
            nsub.Mode.Returns("DEC");
            Assert.That(nsub.Mode, Is.EqualTo("DEC"));


            // Provide multiple return values
            nsub.Mode.Returns("DEC", "HEX", "BIN");
            Assert.AreEqual("DEC", nsub.Mode);
            Assert.AreEqual("HEX", nsub.Mode);
            Assert.AreEqual("BIN", nsub.Mode);

            // Multiple return values with a function
            nsub.Mode.Returns(
                x => "DEC",
                x => "HEX",
                x => throw new Exception()
            );
            Assert.AreEqual("DEC", nsub.Mode);
            Assert.AreEqual("HEX", nsub.Mode);
            Assert.Throws<Exception>(() => { var result = nsub.Mode; });
        }

        [Test]
        public void ProvideExceptions()
        {
            // using NSubstitute.ExceptionExtensions;
            nsub.Add(1, 1).Throws(new InvalidOperationException());
            Assert.Throws<InvalidOperationException>(() => nsub.Add(1, 1));

            nsub.When(x => x.SetMode("HEX")).Throw<ArgumentException>();
            Assert.Throws<ArgumentException>(() => nsub.SetMode("HEX"));
        }

        [Test]
        public void ProvideValues_WithRefAndOut()
        {
            nsub.Divide(12, 5, out float remainder).Returns((CallInfo callInfo) =>
            {
                callInfo[2] = 0.4F; // [2] = 3th parameter remainder
                return 2;
            });

            Assert.AreEqual(2, nsub.Divide(12, 5, out remainder));
            Assert.AreEqual(0.4F, remainder);
        }

        [Test]
        public void ProvideValues_ForAllOfType()
        {
            // using NSubstitute.Extensions;
            var sub = Substitute.For<ICalculator>();
            sub.Add(1, 3).Returns(3);
            sub.ReturnsForAll<int>(5);

            Assert.That(sub.Add(1, 3), Is.EqualTo(3));
            Assert.That(sub.Add(0, 9), Is.EqualTo(5));
            Assert.That(sub.Divide(0, 9, out float remainder), Is.EqualTo(5));
        }

        [Test]
        public void ProvideValues_FromFunction_WithCallInfo()
        {
            nsub
                .Add(Arg.Any<int>(), Arg.Any<int>())
                .Returns((CallInfo callInfo) =>
                {
                    // Same signature is used for ReturnsForAnyArgs

                    // Get argument value with indexer
                    int firstArg = (int)callInfo[0];

                    // By position
                    int secondArg = callInfo.ArgAt<int>(1);

                    // By type
                    // Throws because there should be a Single parameter of the type
                    Assert.Throws<AmbiguousArgumentsException>(() => callInfo.Arg<int>());

                    // Get parameter type information
                    Assert.That(callInfo.ArgTypes().First(), Is.EqualTo(typeof(int)));

                    // Actual return value
                    return firstArg + secondArg;
                });

            Assert.That(nsub.Add(1, 3), Is.EqualTo(4));
        }
        #endregion

        #region Actions
        [Test]
        public void ProvideValues_AndDoesCallback()
        {
            // Instead of using the "Returns((CallInfo callInfo) => 0" syntax
            // it is also possible to follow up with a callback using AndDoes

            int counter = 0;
            nsub
                .Add(0, 0)
                .ReturnsForAnyArgs(x => 0)
                .AndDoes(x => counter++);

            nsub.Add(7, 3);
            nsub.Add(2, 2);
            Assert.AreEqual(counter, 2);
        }

        [Test]
        public void SubstituteVoids_WithWhenDo()
        {
            var sub = Substitute.For<ICalculator>();

            // Simple void method hijacking
            bool called = false;
            sub
                .When(x => x.SetMode("HEX"))
                .Do(x => called = true);

            sub.SetMode("HEX");
            Assert.True(called);
        }

        #region Performing actions with arguments
        [Test]
        public void PerformingActionsWithArguments()
        {
            int argumentUsed = 0;
            nsub.Add(10, Arg.Do<int>(x => argumentUsed = x));

            nsub.Add(10, 42);
            nsub.Add(11, 0); // does not overwrite argumentUsed because first arg is not 10

            Assert.AreEqual(42, argumentUsed);
        }
        #endregion
        #endregion
    }
}