namespace NSubstituteTutorial
{
    public class Calculator : ICalculator
    {
        public Calculator()
        {
            
        }

        public Calculator(int @base)
        {
            
        }

        public virtual int Add(int a, int b)
        {
            return a + b;
        }

        public int Divide(int n, int divisor, out float remainder)
        {
            remainder = n - (n / divisor);
            return n / divisor;
        }

        public string Mode { get; set; }

        public void SetMode(string mode)
        {
            Mode = mode;
        }
    }
}