function init()
{
    // disable context menu
    document.oncontextmenu = function ()
    {
        return false;
    }
    // keyboard detection
    function post(data)
    {
        $.ajax({
            url: window.location.pathname,
            type: "POST",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        });
    }
    // format modifiers
    function getMod(e)
    {
        return (e.altKey ? "A-" : "") + (e.shiftKey ? "S-" : "") + (e.ctrlKey ? "C-" : "");
    }
    // detect printscreen, mod+Fx keys and F12 (debugger) on keyup
    // printscreen can only be detected on keyup
    window.onkeyup = function (e)
    {
        var key = e.which;
        var mod = getMod(e);
        var data = mod + e.key;
        if (key == 44
            || (mod && (key >= 112 && key <= 135))
            || (!mod && key == 123))
        {
            post(data);
        }
    }
    // Detect ctrl+c/x on keydown because on keyup it is missed sometimes
    window.onkeydown = function (e)
    {
        if (e.repeat)
            return;
        var key = e.which;
        var mod = getMod(e);
        var data = mod + e.key;
        if (e.ctrlKey && (key === 67 || key === 88))
            post(data);
    }
}