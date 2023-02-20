using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

/*
GET /recipes -> Get a list of all recipes.
GET /recipes/{id} -> Get a recipe with the given id.
GET /recipes/search/{filter} -> Get a list of recipes with the given filter in the title or description.
GET /recipes/ingredients/{filter} -> Get a list of recipes with the given filter in the ingredients.
POST /recipes -> Create a new recipe.
DELETE /recipes/{id} -> Delete a recipe with the given id.
PUT /recipes/{id} -> Update a recipe with the given id.

STRUCTURE:
-Title(string): The title of the recipe.
-Description(string): A description of the recipe.
-Ingredients(List<string>): A list of ingredients.
-TitleImage(string): The URL of the title image.
*/


var recipeDict = new ConcurrentDictionary<int, Recipe>{};
var newRecipeId = 0;

app.MapGet("/recipes", () => recipeDict.Values);
app.MapGet("/recipes/{id}", (int id) => {
    if (recipeDict.TryGetValue(id, out var recipe)) {
        return Results.Ok(recipe);
    }
    return Results.NotFound($"Recipe with id {id} not found");
});

app.MapGet("/recipes/search/{filter}", (string filter) => {
    var filteredRecipes = new List<Recipe>();
    foreach (var recipe in recipeDict.Values) {
        if (recipe.Title.Contains(filter) || recipe.Description.Contains(filter)) {
            filteredRecipes.Add(recipe);
        }
    }
    return filteredRecipes;
});

app.MapGet("/recipes/ingredients/{filter}", (string filter) => {
    var filteredRecipes = new List<Recipe>();
    foreach (var recipe in recipeDict.Values) {
        if (recipe.Ingredients != null) {
            foreach (var ingredient in recipe.Ingredients) {
                if (ingredient.Contains(filter)) {
                    filteredRecipes.Add(recipe);
                    break;
                }
            }
        }
    }
    return filteredRecipes;
});

app.MapPost("/recipes", (CreateAndUpdateRecipeDto recipeDto) => {
    var newId = Interlocked.Increment(ref newRecipeId);

    var recipe = new Recipe {
        Id = newId,
        Title = recipeDto.Title,
        Description = recipeDto.Description,
        Ingredients = recipeDto.Ingredients,
        TitleImage = recipeDto.TitleImage
    };
    if(!recipeDict.TryAdd(newId, recipe)){
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    };
    return Results.Created($"/recipes/{newId}", recipe);
});

app.MapDelete("/recipes/{id}", (int id) => {

    if (recipeDict.TryRemove(id, out var recipe)) {
        return Results.NoContent();
    }
    return Results.NotFound($"Recipe with id {id} not found");
});

app.MapPut("/recipes/{id}", (int id, CreateAndUpdateRecipeDto recipeDto) => {
    var recipe = new Recipe {
        Id = id,
        Title = recipeDto.Title,
        Description = recipeDto.Description,
        Ingredients = recipeDto.Ingredients,
        TitleImage = recipeDto.TitleImage
    };
    if (recipeDict.TryUpdate(id, recipe, recipeDict[id])) {
        return Results.Created($"/recipes/{id}", recipe);
    }
    return Results.NotFound($"Recipe with id {id} not found");
});

app.Run();

class Recipe {
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string>? Ingredients { get; set; }
    public string TitleImage { get; set; } = "";
}

record CreateAndUpdateRecipeDto(string Title, string Description, List<string> Ingredients, string TitleImage);