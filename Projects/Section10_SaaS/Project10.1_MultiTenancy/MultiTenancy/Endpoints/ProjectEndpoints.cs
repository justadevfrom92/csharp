using Microsoft.AspNetCore.Http.HttpResults;
using MultiTenancy.Contexts;
using MultiTenancy.Models;
using MultiTenancy.Services;

namespace MultiTenancy.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", (TenantContext tenantContext) => TypedResults.Ok(tenantContext.Tenant));

        app.MapGet("/projects", async (ProjectService service) => TypedResults.Ok(await service.ListAsync()));

        app.MapGet("/projects/{id:int}", async Task<Results<Ok<ProjectModel>, NotFound>> (int id, ProjectService service) =>
            await service.FindAsync(id) is ProjectModel project ? TypedResults.Ok(project) : TypedResults.NotFound());

        app.MapPost("/projects", async (SaveProjectRequestModel request, ProjectService service) =>
        {
            ProjectModel project = await service.CreateAsync(request.Name);
            return TypedResults.Created($"/projects/{project.Id}", project);
        });

        app.MapPut("/projects/{id:int}", async Task<Results<NoContent, NotFound>> (int id, SaveProjectRequestModel request, ProjectService service) =>
            await service.RenameAsync(id, request.Name) ? TypedResults.NoContent() : TypedResults.NotFound());

        app.MapDelete("/projects/{id:int}", async Task<Results<NoContent, NotFound>> (int id, ProjectService service) =>
            await service.DeleteAsync(id) > 0 ? TypedResults.NoContent() : TypedResults.NotFound());
    }
}
