# AmbientTasks [![Build Status](https://dev.azure.com/Techsola/AmbientTasks/_apis/build/status/AmbientTasks%20CI?branchName=master)](https://dev.azure.com/Techsola/AmbientTasks/_build/latest?definitionId=1&branchName=master) [![codecov](https://codecov.io/gh/Techsola/AmbientTasks/branch/master/graph/badge.svg)](https://codecov.io/gh/Techsola/AmbientTasks) [![MyGet badge](https://img.shields.io/myget/ambienttasks/vpre/AmbientTasks.svg?label=myget)](https://www.myget.org/feed/ambienttasks/package/nuget/AmbientTasks "MyGet (prereleases)")

Enables scoped completion tracking and error handling of tasks as an alternative to fire-and-forget and `async void`. Easy to produce and consume, and test-friendly.

Benefits:

- Avoids `async void` which, while being semantically correct for top-level event handlers, [is very easy to misuse](https://msdn.microsoft.com/en-us/magazine/jj991977.aspx).

- Avoids fire-and-forget (`async Task` but ignoring the task). This comes with its own pitfalls, leaking the exception to `TaskScheduler.UnobservedTaskException` or never discovering a defect due to suppressing exceptions.

- Test code can use a simple API to know exactly how long to wait for asynchronous processes triggered by non-async APIs before doing a final assert.

- Exceptions are no longer missed in test code due to the test not waiting long enough or the exception being unhandled on a thread pool thread.

- Unhandled task exceptions are sent to a chosen global handler immediately rather than waiting until the next garbage collection (arbitrarily far in the future) finalizes an orphaned task and triggers `TaskScheduler.UnobservedTaskException`.

## Example 1 (view model)

When the UI picker bound to `SelectedFooId` changes the property, the displayed label bound to `SelectedFooName` should update to reflect information about the selection.

(See the [How to use](#how-to-use) section to see what you’d probably want to add to your `Program.Main`.)

```cs
public class ViewModel
{
    private int selectedFooId;

    public int SelectedFooId
    {
        get => selectedFooId;
        set
        {
            if (selectedFooId == value) return;
            selectedFooId = value;
            OnPropertyChanged();

            // Start task without waiting for it
            AmbientTasks.Add(UpdateSelectedFooNameAsync(selectedFooId));
        }
    }

    // Never use async void (or fire-and-forget which is in the same spirit)
    private async Task UpdateSelectedFooNameAsync(int fooId)
    {
        SelectedFooName = null;

        var foo = await LoadFooAsync(fooId);
        if (selectedFooId != fooId) return;

        // Update UI
        SelectedFooName = foo.Name;
    }
}
```

### Test code

```cs
[Test]
public void Changing_selected_ID_loads_and_updates_selected_name()
{
    // Set up a delay
    var vm = new ViewModel(...);

    vm.SelectedFooId = 42;

    await AmbientTasks.WaitAllAsync();
    Assert.That(vm.SelectedFooName, Is.EqualTo("Some name"));
}
```

## Example 2 (form)

(See the [How to use](#how-to-use) section to see what you’d probably want to add to your `Program.Main`.)

```cs
public class MainForm
{
    private void FooComboBox_GotFocus(object sender, EventArgs e)
    {
        // Due to idiosyncrasies of the third-party control, ShowPopup doesn’t work properly when called
        // during the processing of this event. The recommendation is usually to queue ShowPopup to happen
        // right after the event is no longer being handled via Control.BeginInvoke or similar.

        // Use AmbientTasks.Post rather than:
        //  - Control.BeginInvoke
        //  - SynchronizationContext.Post
        //  - await Task.Yield() (requires async void event handler)

        // This way, your tests know how long to wait and exceptions are automatically propagated to them.
        AmbientTasks.Post(() => FooComboBox.ShowPopup());
    }
}
```

### Test code

```cs
[Test]
public void Foo_combo_box_opens_when_it_receives_focus()
{
    var form = new MainForm(...);
    form.Show();

    WindowsFormsUtils.RunWithMessagePump(async () =>
    {
        form.FooComboBox.Focus();

        await AmbientTasks.WaitAllAsync();
        Assert.That(form.FooComboBox.IsPopupOpen, Is.True);
    });
}
```

## How to use

If your application has a top-level exception handler which grabs diagnostics or displays a prompt to send logs or restart, you’ll want to add this to the top of `Program.Main`:

```cs
AmbientTasks.BeginContext(ex => GlobalExceptionHandler(ex));
```

Any failure in a task passed to `AmbientTasks.Add` will be immediately handled there rather than throwing the exception on a background thread or synchronization context.

Use `AmbientTasks.Add` and `Post` any time a non-async call starts off an asynchronous or queued procedure. (See the example section.) This includes replacing fire-and-forget by passing the task to `AmbientTasks.Add` and replacing `async void` by changing it to `void` and moving the awaits into an `async Task` method or lambda. For example:

##### Before

```cs
private async void SomeEventHandler(object sender, EventArgs e)
{
    // Update UI

    var info = await GetInfoAsync(...);

    // Update UI using info
}
```

##### After

```cs
private void SomeEventHandler(object sender, EventArgs e)
{
    // Update UI

    AmbientTasks.Add(async () =>
    {
        var info = await GetInfoAsync(...);

        // Update UI using info
    });
}
```

Finally, await `AmbientTasks.WaitAllAsync()` in your test code whenever `AmbientTasks.Add` is used. This gets the timing right and routes any background exceptions to the responsible test.

It could potentially make sense to delay the application exit until `AmbientTasks.WaitAllAsync()` completes, too, depending on your needs.
