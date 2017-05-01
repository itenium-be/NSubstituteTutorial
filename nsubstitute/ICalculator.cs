namespace NSubstituteTutorial
{
    public interface ICalculator
    {
        int Add(int a, int b);
        int Divide(int n, int divisor, out float remainder);
        string Mode { get; set; }
        void SetMode(string mode);
    }
}