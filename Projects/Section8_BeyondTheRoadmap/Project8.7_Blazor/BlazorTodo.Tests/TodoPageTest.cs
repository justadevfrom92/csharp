using AngleSharp.Dom;
using BlazorTodo.Components.Pages;

namespace BlazorTodo.Tests;

// bUnit renders components IN MEMORY (no browser, no server) and lets the test click, type and read the HTML.
// BunitContext: a fresh rendering environment for every test (xUnit creates a new instance per test).
public class TodoPageTest : BunitContext
{
    private static void AddItem(IRenderedComponent<TodoPage> page, string title)
    {
        page.Find("#new-title").Change(title);
        page.Find("form").Submit();
    }

    // ---------- Add an item ----------

    [Fact]
    public void AddItem_PositivePositive_ValidTitle_IsListedAndFormIsCleared()
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();

        AddItem(page, "  Buy milk  ");

        Assert.Equal("Buy milk", page.Find("li span").TextContent); // trimmed
        Assert.Equal("1 of 1 left", page.Find("#remaining").TextContent);
        Assert.Equal("", page.Find("#new-title").GetAttribute("value"));
    }

    [Fact]
    public void AddItem_PositiveNegative_TitleOver60Characters_ShowsMessageAndAddsNothing()
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();

        AddItem(page, new string('x', 61));

        Assert.Equal("Keep it under 60 characters.", page.Find(".validation-message").TextContent);
        Assert.NotNull(page.Find("#empty-message"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")] // only spaces: [Required] treats this as empty too
    public void AddItem_NegativeNegative_EmptyTitle_ShowsRequiredMessageAndAddsNothing(string title)
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();

        AddItem(page, title);

        Assert.Equal("Write what needs doing.", page.Find(".validation-message").TextContent);
        Assert.Empty(page.FindAll("li"));
    }

    // ---------- Complete an item (EventCallback from the child component) ----------

    [Fact]
    public void CompleteItem_PositivePositive_TickingCheckbox_MarksDoneAndUpdatesCount()
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();
        AddItem(page, "Buy milk");
        AddItem(page, "Write tests");

        page.Find("li input[type=checkbox]").Change(true);

        Assert.Equal("1 of 2 left", page.Find("#remaining").TextContent);
        Assert.Contains("done", page.FindAll("li")[0].ClassList);
    }

    [Fact]
    public void CompleteItem_PositiveNegative_TickingTwice_PutsItemBackToNotDone()
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();
        AddItem(page, "Buy milk");

        page.Find("li input[type=checkbox]").Change(true);
        page.Find("li input[type=checkbox]").Change(false);

        Assert.Equal("1 of 1 left", page.Find("#remaining").TextContent);
        Assert.DoesNotContain("done", page.Find("li").ClassList);
    }

    [Fact]
    public void CompleteItem_NegativeNegative_NothingCompleted_ClearButtonIsDisabled()
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();
        AddItem(page, "Buy milk");

        IElement clearButton = page.Find("#clear-completed");

        Assert.True(clearButton.HasAttribute("disabled"));
    }

    // ---------- Remove / clear ----------

    [Fact]
    public void RemoveItem_PositivePositive_RemovesOnlyThatItem()
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();
        AddItem(page, "Buy milk");
        AddItem(page, "Write tests");

        page.FindAll("li button.remove")[0].Click();

        IReadOnlyList<IElement> remaining = page.FindAll("li span");
        Assert.Single(remaining);
        Assert.Equal("Write tests", remaining[0].TextContent);
    }

    [Fact]
    public void RemoveItem_PositiveNegative_RemovingTheLastItem_ShowsEmptyMessage()
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();
        AddItem(page, "Buy milk");

        page.Find("li button.remove").Click();

        Assert.Equal("Nothing to do.", page.Find("#empty-message").TextContent);
    }

    [Fact]
    public void RemoveItem_NegativeNegative_ClearCompleted_KeepsItemsThatAreNotDone()
    {
        IRenderedComponent<TodoPage> page = Render<TodoPage>();
        AddItem(page, "Buy milk");
        AddItem(page, "Write tests");
        page.Find("li input[type=checkbox]").Change(true);

        page.Find("#clear-completed").Click();

        Assert.Equal("Write tests", page.Find("li span").TextContent);
        Assert.Equal("1 of 1 left", page.Find("#remaining").TextContent);
    }
}
