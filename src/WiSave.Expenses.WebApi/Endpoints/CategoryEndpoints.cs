using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Core.Infrastructure.Identity;
using WiSave.Expenses.Core.Infrastructure.Postgres;
using WiSave.Expenses.Core.Infrastructure.Postgres.Entities;
using WiSave.Expenses.WebApi.Authorization;
using WiSave.Expenses.WebApi.Requests.Categories;

namespace WiSave.Expenses.WebApi.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/expenses/categories").WithTags("Categories");

        group.MapGet("/", GetAll).RequirePermission(Permissions.Expenses.Read);
        group.MapPost("/", Create).RequirePermission(Permissions.Expenses.Write);
        group.MapPut("/{id}", Update).RequirePermission(Permissions.Expenses.Write);
        group.MapDelete("/{id}", Delete).RequirePermission(Permissions.Expenses.Delete);
        group.MapPost("/{id}/subcategories", CreateSubcategory).RequirePermission(Permissions.Expenses.Write);
        group.MapDelete("/{id}/subcategories/{subId}", DeleteSubcategory).RequirePermission(Permissions.Expenses.Delete);
    }

    private static async Task<IResult> GetAll(
        ICurrentUser user, ExpensesDbContext db)
    {
        return Results.Ok(await db.Categories
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
            .ToListAsync());
    }

    private static async Task<IResult> Create(
        ICurrentUser user, ExpensesDbContext db, CreateCategoryRequest request)
    {
        var entity = new CategoryEntity
        {
            Id = Guid.CreateVersion7().ToString(),
            UserId = user.UserId,
            Name = request.Name,
            SortOrder = request.SortOrder ?? 0,
        };

        db.Categories.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/categories/{entity.Id}", new { entity.Id, entity.Name, entity.SortOrder });
    }

    private static async Task<IResult> Update(
        string id, ICurrentUser user, ExpensesDbContext db, UpdateCategoryRequest request)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId);
        if (entity is null) return Results.NotFound();

        entity.Name = request.Name;
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
        await db.SaveChangesAsync();
        return Results.Ok(new { entity.Id, entity.Name, entity.SortOrder });
    }

    private static async Task<IResult> Delete(
        string id, ICurrentUser user, ExpensesDbContext db)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId);
        if (entity is null) return Results.NotFound();

        db.Subcategories.RemoveRange(db.Subcategories.Where(s => s.CategoryId == id));
        db.Categories.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> CreateSubcategory(
        string id, ICurrentUser user, ExpensesDbContext db, CreateSubcategoryRequest request)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId);
        if (category is null) return Results.NotFound();

        var entity = new SubcategoryEntity
        {
            Id = Guid.CreateVersion7().ToString(),
            CategoryId = id,
            Name = request.Name,
            SortOrder = request.SortOrder ?? 0,
        };

        db.Subcategories.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/categories/{id}/subcategories/{entity.Id}", new { entity.Id, entity.Name, entity.SortOrder });
    }

    private static async Task<IResult> DeleteSubcategory(
        string id, string subId, ICurrentUser user, ExpensesDbContext db)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId);
        if (category is null) return Results.NotFound();

        var sub = await db.Subcategories.FirstOrDefaultAsync(s => s.Id == subId && s.CategoryId == id);
        if (sub is null) return Results.NotFound();

        db.Subcategories.Remove(sub);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
