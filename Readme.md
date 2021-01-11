# AmbientTasks [![NuGet badge](https://img.shields.io/nuget/v/AmbientTasks)](https://www.nuget.org/packages/AmbientTasks/ "NuGet (releases)") [![MyGet badge](https://img.shields.io/myget/ambienttasks/vpre/AmbientTasks.svg?label=myget)](https://www.myget.org/feed/ambienttasks/package/nuget/AmbientTasks "MyGet (prereleases)") [![Gitter badge](https://img.shields.io/gitter/room/Techsola/AmbientTasks)](https://gitter.im/Techsola/AmbientTasks "Chat on Gitter") [![Build status badge](https://github.com/Techsola/AmbientTasks/workflows/CI/badge.svg)](https://github.com/Techsola/AmbientTasks/actions?query=workflow%3ACI "Build status") [![codecov badge](https://codecov.io/gh/Techsola/AmbientTasks/branch/master/graph/badge.svg)](https://codecov.io/gh/Techsola/AmbientTasks "Test coverage")

All notable changes are documented in [CHANGELOG.md](CHANGELOG.md).

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

## Debugging into AmbientTasks source

Stepping into AmbientTasks source code, pausing the debugger while execution is inside AmbientTasks code and seeing the source, and setting breakpoints in AmbientTasks all require loading symbols for AmbientTasks. To do this in Visual Studio:

1. Go to Debug > Options, and uncheck ‘Enable Just My Code.’ (It’s a good idea to reenable this as soon as you’re finished with the task that requires debugging into a specific external library.)  
   ℹ *Before* doing this, because Visual Studio can become unresponsive when attempting to load symbols for absolutely everything, I recommend going to Debugging > Symbols within the Options window and selecting ‘Load only specified modules.’

2. If you are using a version that was released to nuget.org, enable the built-in ‘NuGet.org Symbol Server’ symbol location.  
   If you are using a prerelease version of AmbientTasks package, go to Debugging > Symbols within the Options window and add this as a new symbol location: `https://www.myget.org/F/ambienttasks/api/v2/symbolpackage/`

3. If ‘Load only specified modules’ is selected in Options > Debugging > Symbols, you will have to explicitly tell Visual Studio to load symbols for AmbientTasks. One way to do this while debugging is to go to Debug > Windows > Modules and right-click on AmbientTasks. Select ‘Load Symbols’ if you only want to do it for the current debugging session. Select ‘Always Load Automatically’ if you want to load symbols now and also add the file name to a list so that Visual Studio loads AmbientTasks symbols in all future debug sessions when Just My Code is disabled.
