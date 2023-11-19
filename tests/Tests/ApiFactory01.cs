namespace tests;

public class ApiFactory01<TProgram> : ApiFactory<TProgram> where TProgram : class 
{
    public override string GetFilename() => $"Tests{Path.DirectorySeparatorChar}appsettings.Test01.json";
}
