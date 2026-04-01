using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Core.Infrastructure.Postgres;
using WiSave.Expenses.Core.Infrastructure.Postgres.Entities;

namespace WiSave.Expenses.WebApi.Endpoints.Categories;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories").WithTags("Categories");

        group.MapGet("/", async (ICurrentUser user, ExpensesDbContext db) =>
            Results.Ok(await db.Categories
                .Where(c => c.UserId == user.UserId)
                .OrderBy(c => c.SortOrder)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.SortOrder,
                    Subcategories = db.Subcategories
                        .Where(s => s.CategoryId == c.Id)
                        .OrderBy(s => s.SortOrder)
                        .Select(s => new { s.Id, s.Name, s.SortOrder })
                        .ToList()
                })
                .AsNoTracking()
                .ToListAsync()));

        group.MapPost("/", async (ICurrentUser user, ExpensesDbContext db, CreateCategoryRequest request) =>
        {
            var entity = new CategoryEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.UserId,
                Name = request.Name,
                SortOrder = request.SortOrder ?? 0,
            };

            db.Categories.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/categories/{entity.Id}", new { entity.Id, entity.Name, entity.SortOrder });
        });

        group.MapPut("/{id}", async (string id, ICurrentUser user, ExpensesDbContext db, UpdateCategoryRequest request) =>
        {
            var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId);
            if (entity is null) return Results.NotFound();

            entity.Name = request.Name;
            if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
            await db.SaveChangesAsync();
            return Results.Ok(new { entity.Id, entity.Name, entity.SortOrder });
        });

        group.MapDelete("/{id}", async (string id, ICurrentUser user, ExpensesDbContext db) =>
        {
            var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId);
            if (entity is null) return Results.NotFound();

            db.Subcategories.RemoveRange(db.Subcategories.Where(s => s.CategoryId == id));
            db.Categories.Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Subcategories
        group.MapPost("/{id}/subcategories", async (
            string id, ICurrentUser user, ExpensesDbContext db, CreateSubcategoryRequest request) =>
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId);
            if (category is null) return Results.NotFound();

            var entity = new SubcategoryEntity
            {
                Id = Guid.NewGuid().ToString(),
                CategoryId = id,
                Name = request.Name,
                SortOrder = request.SortOrder ?? 0,
            };

            db.Subcategories.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/categories/{id}/subcategories/{entity.Id}", new { entity.Id, entity.Name, entity.SortOrder });
        });

        group.MapDelete("/{id}/subcategories/{subId}", async (
            string id, string subId, ICurrentUser user, ExpensesDbContext db) =>
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId);
            if (category is null) return Results.NotFound();

            var sub = await db.Subcategories.FirstOrDefaultAsync(s => s.Id == subId && s.CategoryId == id);
            if (sub is null) return Results.NotFound();

            db.Subcategories.Remove(sub);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public sealed record CreateCategoryRequest(string Name, int? SortOrder = null);
public sealed record UpdateCategoryRequest(string Name, int? SortOrder = null);
public sealed record CreateSubcategoryRequest(string Name, int? SortOrder = null);
