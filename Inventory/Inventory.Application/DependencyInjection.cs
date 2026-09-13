using System.Globalization;
using System.Reflection;
using FluentValidation;
using Inventory.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
namespace Inventory.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(DependencyInjection).Assembly;
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("en");
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: false);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
