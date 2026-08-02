using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MyMediaVerse.Web.API.Conventions;

/// <summary>
/// Removes controllers and actions whose <see cref="EnvironmentsAttribute"/> does not
/// include the current host environment. Removal happens while the application model
/// is being built, before routing exists: gated endpoints 404 on non-matching hosts
/// and are absent from Swagger, which is stronger than any authorization filter.
/// </summary>
public sealed class EnvironmentGatingConvention : IApplicationModelConvention
{
    private readonly string _environmentName;

    public EnvironmentGatingConvention(string environmentName)
    {
        _environmentName = environmentName;
    }

    public void Apply(ApplicationModel application)
    {
        for (var i = application.Controllers.Count - 1; i >= 0; i--)
        {
            var controller = application.Controllers[i];

            var controllerGate = controller.Attributes.OfType<EnvironmentsAttribute>().FirstOrDefault();
            if (controllerGate is not null && !controllerGate.Matches(_environmentName))
            {
                application.Controllers.RemoveAt(i);
                continue;
            }

            for (var j = controller.Actions.Count - 1; j >= 0; j--)
            {
                var actionGate = controller.Actions[j].Attributes.OfType<EnvironmentsAttribute>().FirstOrDefault();
                if (actionGate is not null && !actionGate.Matches(_environmentName))
                {
                    controller.Actions.RemoveAt(j);
                }
            }
        }
    }
}
