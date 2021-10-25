using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Voting2021.BlockchainWatcher.Web
{
	public class Program
	{
		public static int Main(string[] args)
		{
			Serilog.Debugging.SelfLog.Enable(Console.Out);

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.Enrich.FromLogContext()
				.WriteTo.Console()
				.CreateBootstrapLogger();

			// перехватываем ошибки инициализации
			try
			{
				Log.Information("Start");
				ConfigureThreadPool();
				using IHost host = CreateHostBuilder(args).Build();
				host.Run();
				return 0;
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Host terminated unexpectedly");
				return 1;
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		static void ConfigureThreadPool()
		{
			ThreadPool.GetMinThreads(out var workerThreads, out var completitionPortThreads);
			ThreadPool.SetMinThreads(Math.Max(workerThreads, 8), Math.Max(completitionPortThreads, 8));
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				}).UseSerilog(delegate (HostBuilderContext context, IServiceProvider services, LoggerConfiguration configuration)
				{
					IEnumerable<ILogEventEnricher> services2 = services.GetServices<ILogEventEnricher>();
					LoggerConfiguration loggerConfiguration = configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services).Enrich.FromLogContext();
					loggerConfiguration.Enrich.With(services2.ToArray());
				});
		}
	}
}
