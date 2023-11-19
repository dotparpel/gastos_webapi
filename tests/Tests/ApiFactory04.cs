namespace tests;

public class ApiFactory04<TProgram> : ApiFactory<TProgram> where TProgram : class 
{
    public override string GetFilename() => $"Tests{Path.DirectorySeparatorChar}appsettings.Test04.json";
}