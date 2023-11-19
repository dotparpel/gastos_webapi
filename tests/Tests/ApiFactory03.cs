namespace tests;

public class ApiFactory03<TProgram> : ApiFactory<TProgram> where TProgram : class 
{
    public override string GetFilename() => $"Tests{Path.DirectorySeparatorChar}appsettings.Test03.json";
}