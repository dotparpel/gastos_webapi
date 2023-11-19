using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using webapi.Models;
using webapi.Repository;

namespace tests;

public abstract class ApiFactory<TProgram> : WebApplicationFactory<TProgram> 
    where TProgram : class 
{
    public abstract string GetFilename();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => {
            config.AddJsonFile(GetFilename());
        });

        return base.CreateHost(builder);
    }

    public object? GetService(Type type) {
        IServiceScopeFactory? service = (IServiceScopeFactory?) 
            this.Services.GetService(typeof(IServiceScopeFactory));
        IServiceScope? scope = service?.CreateScope();
        object? ret = scope?.ServiceProvider.GetRequiredService(type);

        return ret;
    }

    public IGenericSearch<TEntity>? GetSearch<TEntity>(Type? repositoryType = null) 
        where TEntity : class 
    {
        IGenericSearch<TEntity>? ret = null;

        if (repositoryType == null) {
            DbContext? ctx = (DbContext?) GetService(typeof(DbContext));
            if (ctx != null)
                ret = new GenericSearch<TEntity>(ctx);
        } else {
            ret = (IGenericSearch<TEntity>?) GetService(repositoryType);
        }

        return ret;
    }

    public IGenericRepository<TEntity, TKey>? GetRepository<TEntity, TKey> (Type? repositoryType = null)
        where TEntity : Entity<TKey>, new()
    {
        IGenericRepository<TEntity, TKey>? ret = null;

        if (repositoryType == null) {
            DbContext? ctx = (DbContext?) GetService(typeof(DbContext));
            if (ctx != null)
                ret = new GenericRepository<TEntity, TKey>(ctx);
        } else {
            ret = (IGenericRepository<TEntity, TKey>?) GetService(repositoryType);
        }

        return ret;
    }
}