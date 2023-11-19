namespace tests;

public class ApiFactory02<TProgram> : ApiFactory<TProgram> where TProgram : class 
{
    public override string GetFilename() => $"Tests{Path.DirectorySeparatorChar}appsettings.Test02.json";
}