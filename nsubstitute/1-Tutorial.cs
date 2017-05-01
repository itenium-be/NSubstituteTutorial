using NSubstitute;
using NUnit.Framework;

namespace NSubstituteTutorial
{
    public class Tutorial
    {
        ICalculator nsub = Substitute.For<ICalculator>();

        [Test]
        public void MultipleReturnValues()
        {
            nsub.Add(1, 1).ReturnsForAnyArgs(5);
            nsub.DidNotReceiveWithAnyArgs().SetMode("");
        }
    }
}