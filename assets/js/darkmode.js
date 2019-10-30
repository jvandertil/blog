function ToggleThemeStylesheets(theme) {
    var styleSheets = document.styleSheets;

    for (var i = 0; i < styleSheets.length; ++i) {
        var ss = styleSheets[i];

        if (ss.href) {
            var themeTag = ss.ownerNode.getAttribute("data-theme")

            if (themeTag) {
                if (themeTag === theme) {
                    ss.disabled = false;
                } else {
                    ss.disabled = true;
                }
            }
        }
    }
}

function getTheme() {
    var body = document.getElementsByTagName("body")[0];
    var theme = body.getAttribute("data-theme");

    if (!theme) {
        theme = "light";
    }

    return theme;
}

function setTheme(theme) {
    var body = document.getElementsByTagName("body")[0];
    body.setAttribute("data-theme", theme);

    ToggleThemeStylesheets(theme);
}

function isDarkmodeEnabled() {
    var darkModeEnabled = getTheme() === "dark";

    return darkModeEnabled;
}

function saveDarkmodePreference(enabled) {
    localStorage.setItem("darkmode-enabled", enabled.toString());
}

function applyTheme() {
    var enabled = localStorage.getItem("darkmode-enabled");

    if (enabled == null) {
        enabled = isDarkmodeEnabled().toString();
    }

    if (enabled === "true") {
        setTheme("dark");
    } else {
        setTheme("light");
    }
}

function toggleDarkmode() {
    saveDarkmodePreference(!isDarkmodeEnabled());

    applyTheme();
}

function toggleDarkmodeKeyPress(event) {
    var KEY_ENTER = 13;
    var KEY_SPACE = 32;

    switch (event.which) {
        case KEY_ENTER:
        case KEY_SPACE: {
            toggleDarkmode();
        }
    }
}

function initializeTheme() {
    applyTheme();
}
