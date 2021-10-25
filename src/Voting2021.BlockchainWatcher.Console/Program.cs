using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using Voting2021.BlockchainWatcher.Services;
using Voting2021.BlockchainWatcher.Settings;

namespace Voting2021.BlockchainWatcher.Console
{
	class Program
	{
		static int Main(string[] args)
		{
			Serilog.Debugging.SelfLog.Enable(System.Console.Out);

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

		static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigureHostConfiguration(config =>
				{
					config.AddJsonFile("appsettings.json", false);
				})
				.ConfigureServices((hostBuilderContext, services) =>
				{
					services.AddOptions();
					services.Configure<BlockchainConnectionSettings>(hostBuilderContext.Configuration.GetSection("BlockchainConnectionSettings"));
					services.Configure<TransactionStoreSettings>(hostBuilderContext.Configuration.GetSection("TransactionStoreSettings"));
					
					services.AddSingleton<BlockchainWatcherHostedService>();
					services.AddSingleton<ITransactionStore, SimpleFileTransactionStore>();
					services.AddSingleton<ITransactionCache, InMemoryTransactionCache>();
					services.AddSingleton<DataSigningService>();
					services.AddSingleton<IBlockchainEventProcessor, FastBlockHistoryBlockchainEventProcessor>();
					services.AddSingleton<TransactionFormatter, TransactionFormatter1>();
					services.AddHostedService<BlockchainWatcherHostedService>(x => x.GetRequiredService<BlockchainWatcherHostedService>());
				})
				.UseSerilog(delegate (HostBuilderContext context, IServiceProvider services, LoggerConfiguration configuration)
				{
					IEnumerable<ILogEventEnricher> services2 = services.GetServices<ILogEventEnricher>();
					LoggerConfiguration loggerConfiguration = configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services).Enrich.FromLogContext();
					loggerConfiguration.Enrich.With(services2.ToArray());
				});
		}
	}
}
