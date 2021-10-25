using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Voting2021.BlockchainWatcher.Services;
using Voting2021.BlockchainWatcher.Settings;
using Voting2021.BlockchainWatcher.Web.Services;

namespace Voting2021.BlockchainWatcher.Web
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddOptions();
			services.Configure<BlockchainConnectionSettings>(Configuration.GetSection("BlockchainConnectionSettings"));
			services.Configure<TransactionStoreSettings>(Configuration.GetSection("TransactionStoreSettings"));

			services.Configure<AppIdSettings>(x =>
			{
				x.AppId = Guid.NewGuid().ToString("N");
			});

			services.AddSingleton<BlockchainWatcherHostedService>();
			services.AddSingleton<StatusService>();
			services.AddSingleton<ITransactionStore, SimpleFileTransactionStore>();
			services.AddSingleton<ITransactionCache, InMemoryTransactionCache>();
			services.AddSingleton<IBlockchainEventProcessor, FastBlockHistoryBlockchainEventProcessor>();
			services.AddSingleton<TransactionFormatter, TransactionFormatter1>();
			services.AddSingleton<TransactionsCopyService>();
			services.AddSingleton<IDataSigningService,DataSigningService>();
			services.AddSingleton<FileOutputService>();
			services.AddHostedService<BlockchainWatcherHostedService>(x => x.GetRequiredService<BlockchainWatcherHostedService>());

			services.AddControllers();
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Voting2021.BlockchainWatcher.Web", Version = "v1" });
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Voting2021.BlockchainWatcher.Web v1"));
			}

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

			//var signingService = app.ApplicationServices.GetRequiredService<IDataSigningService>();

		}
	}
}
