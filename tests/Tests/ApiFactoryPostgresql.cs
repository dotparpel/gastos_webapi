namespace tests;

public class ApiFactoryPostgresql<TProgram> : ApiFactory<TProgram> where TProgram : class 
{
    public override string GetFilename() => $"Tests{Path.DirectorySeparatorChar}appsettings.Postgresql.json";
}