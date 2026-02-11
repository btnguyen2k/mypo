using MyPo.Shared.Api;
using MyPo.Shared.Bootstrap;
using Microsoft.AspNetCore.Mvc;

namespace MyPo.Api.Bootstrap;

/// <summary>
/// Built-in bootstrapper that adds and maps controllers.
/// </summary>
[Bootstrapper]
public class ControllersBootstrapper
{
	public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		appBuilder.Services.AddControllers()
			.ConfigureApiBehaviorOptions(options =>
			{
				// configure custom response for invalid model state (usually input validation failed)
				options.InvalidModelStateResponseFactory = context =>
				{
					var errors = context.ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
					return new BadRequestObjectResult(new ApiResp
					{
						Status = 400,
						Message = $"Bad request: {string.Join(", ", errors)}",
						Extras = errors
					});
				};
			});

		// appBuilder.Services.AddExceptionHandler<MyGlobalExceptionHandler>(); // Register custom handler
	}

	public static void DecorateApp(WebApplication app)
	{
		// configure global exception handler to return JSON response with error details
		app.UseExceptionHandler(errorApp =>
		{
			errorApp.Run(async context =>
			{
				var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
				var exception = exceptionHandlerPathFeature?.Error;

				var response = new ApiResp
				{
					Status = 500,
					Message = exception?.Message ?? "An unexpected error occurred.",
					Extras = exception?.Message
				};

				context.Response.ContentType = "application/json";
				context.Response.StatusCode = 500;
				await context.Response.WriteAsJsonAsync(response);
			});
		});

		// app.UseExceptionHandler();
		app.MapControllers();
	}
}

// public class MyGlobalExceptionHandler : IExceptionHandler
// {
// 	private readonly ILogger<MyGlobalExceptionHandler> logger;

// 	public MyGlobalExceptionHandler(ILogger<MyGlobalExceptionHandler> logger)
// 	{
// 		this.logger = logger;
// 	}

//     public async ValueTask<bool> TryHandleAsync(
//         HttpContext httpContext,
//         Exception exception,
//         CancellationToken cancellationToken)
//     {
// 		logger.LogError(exception, "An unhandled exception occurred while processing the request {path}.", httpContext.Request.Path);

//         httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
//         httpContext.Response.ContentType = "application/json";
// 		var response = new ApiResp
// 		{
// 			Status = (int)HttpStatusCode.InternalServerError,
// 			Message = exception?.Message ?? $"An unhandled exception occurred while processing the request {httpContext.Request.Path}.",
// 		};

//         await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

//         return true; // Exception handled
//     }
// }
