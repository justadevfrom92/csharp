using BlazorTodo.Components;
using BlazorTodo.Models;

namespace BlazorTodo.Tests;

// The child component on its own: parameters in, EventCallbacks out.
public class TodoItemComponentTest : BunitContext
{
    [Fact]
    public void TodoItem_PositivePositive_RendersTitleAndRaisesCallbacks()
    {
        var item = new TodoItemModel { Title = "Buy milk" };
        TodoItemModel? toggled = null;
        TodoItemModel? removed = null;

        IRenderedComponent<TodoItemComponent> component = Render<TodoItemComponent>(parameters => parameters
            .Add(child => child.Item, item)
            .Add(child => child.OnToggled, (TodoItemModel changed) => toggled = changed)
            .Add(child => child.OnRemoved, (TodoItemModel gone) => removed = gone));
        component.Find("input[type=checkbox]").Change(true);
        component.Find("button.remove").Click();

        Assert.Equal("Buy milk", component.Find("span").TextContent);
        Assert.Same(item, toggled);
        Assert.Same(item, removed);
    }

    [Fact]
    public void TodoItem_PositiveNegative_DoneItem_IsShownAsDoneAndChecked()
    {
        var item = new TodoItemModel { Title = "Buy milk", IsDone = true };

        IRenderedComponent<TodoItemComponent> component = Render<TodoItemComponent>(parameters => parameters.Add(child => child.Item, item));

        Assert.Contains("done", component.Find("li").ClassList);
        Assert.True(component.Find("input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public void TodoItem_NegativeNegative_NoCallbacksGiven_ClickingDoesNothingAndDoesNotCrash()
    {
        var item = new TodoItemModel { Title = "Buy milk" };

        IRenderedComponent<TodoItemComponent> component = Render<TodoItemComponent>(parameters => parameters.Add(child => child.Item, item));
        component.Find("input[type=checkbox]").Change(true);
        component.Find("button.remove").Click();

        Assert.False(item.IsDone); // the child never changes the item itself; only the parent does
    }
}
