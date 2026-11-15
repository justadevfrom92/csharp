using BlazorTodo.Components.Variations;
using Bunit;

namespace BlazorTodo.Tests;

// Component variations (BlazorTodo/Components/Variations/), rendered with bUnit.
public class VariationsTest : BunitContext
{
    [Fact]
    public void Callback_PositivePositive_EventCallback_ParentRedrawsAfterTheChildClicks()
    {
        IRenderedComponent<ParentWithEventCallbackComponent> parent = Render<ParentWithEventCallbackComponent>();

        parent.Find("button.child").Click();
        parent.Find("button.child").Click();

        Assert.Equal("2", parent.Find("p.count").TextContent);
    }

    [Fact]
    public void Callback_PositiveNegative_PlainAction_ValueChangesButParentDoesNotRedraw()
    {
        IRenderedComponent<ParentWithActionComponent> parent = Render<ParentWithActionComponent>();

        parent.Find("button.child").Click();
        parent.Find("button.child").Click();

        Assert.Equal(2, parent.Instance.RealCount);           // the method ran twice...
        Assert.Equal("0", parent.Find("p.count").TextContent); // ...but the screen still shows 0
    }

    [Fact]
    public void Binding_PositivePositive_OnChange_UpdatesWhenTheBoxIsLeft_OnInput_OnEveryKeystroke()
    {
        IRenderedComponent<BindOnChangeComponent> onChange = Render<BindOnChangeComponent>();
        IRenderedComponent<BindOnInputComponent> onInput = Render<BindOnInputComponent>();

        // Typing: plain @bind doesn't even LISTEN to "input" - bUnit reports there's only an "onchange" handler.
        Assert.Throws<MissingEventHandlerException>(() => onChange.Find("input.name").Input("Ann"));
        onInput.Find("input.name").Input("Ann");

        Assert.Equal("", onChange.Find("p.echo").TextContent);   // nothing yet
        Assert.Equal("Ann", onInput.Find("p.echo").TextContent); // oninput updates immediately

        onChange.Find("input.name").Change("Ann");  // ...leaving the box
        Assert.Equal("Ann", onChange.Find("p.echo").TextContent);
    }

    [Fact]
    public void Binding_PositiveNegative_BindAfter_RunsTheMethodOncePerChange()
    {
        IRenderedComponent<BindAfterComponent> component = Render<BindAfterComponent>();

        component.Find("input.name").Change("Ann");
        component.Find("input.name").Change("Bob");

        Assert.Equal("Bob (saved 2 time(s))", component.Find("p.echo").TextContent);
    }

    [Fact]
    public void Callback_NegativeNegative_NoHandlerGiven_ClickingDoesNothingAndDoesNotCrash()
    {
        IRenderedComponent<ChildActionComponent> action = Render<ChildActionComponent>();
        IRenderedComponent<ChildEventCallbackComponent> callback = Render<ChildEventCallbackComponent>();

        action.Find("button.child").Click();   // Action? is null: "?.Invoke" skips it
        callback.Find("button.child").Click(); // an empty EventCallback is safe to invoke

        Assert.NotNull(action.Find("button.child"));
    }
}
